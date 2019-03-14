using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Model
{
    using System.Text.RegularExpressions;
    using PerNodeThreadRootList = Dictionary<Tuple<int, int>, List<BuildTimelineEntry>>;

    public class CalculatedThreadAffinity
    {
        public int ThreadId
        {
            get;
            set;
        }

        public HashSet<int> InvalidThreadIds
        {
            get;
            set;
        }

        public bool Calculated
        {
            get;
            set;
        }

        public CalculatedThreadAffinity()
        {
            ThreadId = 0;
            InvalidThreadIds = new HashSet<int>();
            Calculated = false;
        }
    }

    public class BuildTimelineEntry
    {
        public BuildEventArgs StartBuildEvent
        {
            get;
            set;
        }

        public BuildEventArgs EndBuildEvent
        {
            get;
            set;
        }

        public TimeSpan ElapsedTime
        {
            get
            {
                return EndBuildEvent.Timestamp - StartBuildEvent.Timestamp;
            }
        }

        public CalculatedThreadAffinity ThreadAffinity
        {
            get;
            set;
        }

        public BuildTimelineEntry Parent
        {
            get;
            private set;
        }

        public List<BuildTimelineEntry> Children
        {
            get;
            private set;
        }

        public List<BuildMessageEventArgs> Messages
        {
            get;
            private set;
        }

        public BuildTimelineEntry()
        {
            ThreadAffinity = new CalculatedThreadAffinity();
            Children = new List<BuildTimelineEntry>();
            Messages = new List<BuildMessageEventArgs>();
        }

        public void AddChild(BuildTimelineEntry entry)
        {
            Children.Add(entry);
            entry.Parent = this;
        }

        public bool OverlapsWith(BuildTimelineEntry entry)
        {
            return StartBuildEvent.Timestamp < entry.EndBuildEvent.Timestamp &&
                   entry.StartBuildEvent.Timestamp < EndBuildEvent.Timestamp;
        }
    }

    /////////////////////////////////////////////////

    public class FileCompilation
    {
        public string FileName;
        public DateTime StartTimestamp;
        public DateTime EndTimestamp;
        public int ThreadId;

        public DateTime FrontEndFinishTime;
        public string FrontEndFinishMessage;
        public DateTime BackEndFinishTime;
        public string BackEndFinishMessage;

        public TimeSpan ElapsedTime
        {
            get
            {
                return EndTimestamp - StartTimestamp;
            }
        }
    }

    public class ParallelFileCompilation
    {
        public BuildTimelineEntry Parent
        {
            get;
            private set;
        }

        public bool IsCompleted
        {
            get
            {
                return m_unfinishedCompilations.Count == 0;
            }
        }

        public List<FileCompilation> Compilations
        {
            get;
            private set;
        }

        private List<FileCompilation> m_unfinishedCompilations;

        public ParallelFileCompilation(BuildTimelineEntry entry)
        {
            Parent = entry;
            m_unfinishedCompilations = new List<FileCompilation>();
            Compilations = new List<FileCompilation>();
        }

        public void StartFileCompilation(string fileName, DateTime timestamp)
        {
            FileCompilation fileCompilation = new FileCompilation()
            {
                FileName = fileName,
                StartTimestamp = timestamp,
                ThreadId = 0,
            };

            // find out which thread it was assigned to
            while(m_unfinishedCompilations.Exists(cl => cl.ThreadId == fileCompilation.ThreadId))
            {
                ++fileCompilation.ThreadId;
            }

            m_unfinishedCompilations.Add(fileCompilation);
        }

        public void EndFrontEndCompilation(string fileName, DateTime timestamp, string message)
        {
            FileCompilation fileCompilation = m_unfinishedCompilations.Find(cl => cl.FileName == fileName);
            if(fileCompilation != null)
            {
                fileCompilation.FrontEndFinishTime = timestamp;
                fileCompilation.FrontEndFinishMessage = message;
            }
        }

        public void EndFileCompilation(string fileName, DateTime timestamp, string message)
        {
            int index = m_unfinishedCompilations.FindIndex(cl => cl.FileName == fileName);
            if(index != -1)
            {
                FileCompilation fileCompilation = m_unfinishedCompilations[index];
                fileCompilation.BackEndFinishTime = timestamp;
                fileCompilation.EndTimestamp = timestamp;
                fileCompilation.BackEndFinishMessage = message;

                Debug.Assert(fileCompilation.ElapsedTime >= TimeSpan.Zero);

                m_unfinishedCompilations.RemoveAt(index);
                Compilations.Add(fileCompilation);
            }
        }
    }

    /////////////////////////////////////////////////

    public class BuildTimeline
    {
        private static Regex s_CompileFileStart = new Regex(@"^[^\s]+\.(cpp|cc|c)$");
        private static Regex s_CompileFrontEndFinish = new Regex(@"^time\(.+c1xx.dll\).+\[(.+)\]$");
        private static Regex s_CompileBackEndFinish = new Regex(@"^time\(.+c2.dll\).+\[(.+)\]$");

        public List<List<BuildTimelineEntry>> PerNodeRootEntries
        {
            get;
            private set;
        }

        public List<ParallelFileCompilation> ParallelFileCompilations
        {
            get;
            private set;
        }

        public bool IsCompleted
        {
            get
            {
                return m_unfinishedProjects.Count == 0 &&
                       m_unfinishedTargets.Count == 0 &&
                       m_unfinishedTasks.Count == 0 &&
                       m_unfinishedParallelFileCompilations.Count == 0;
            }
        }

        private List<BuildTimelineEntry> m_unfinishedProjects;
        private List<BuildTimelineEntry> m_unfinishedTargets;
        private List<BuildTimelineEntry> m_unfinishedTasks;

        private List<ParallelFileCompilation> m_unfinishedParallelFileCompilations;

        public BuildTimeline(int totalNodeCount)
        {
            PerNodeRootEntries = new List<List<BuildTimelineEntry>>();
            
            // node 0 to be used by top-level build element only
            for(int i = 0; i < totalNodeCount + 1; ++i)
            {
                PerNodeRootEntries.Add(new List<BuildTimelineEntry>());
            }

            ParallelFileCompilations = new List<ParallelFileCompilation>();

            m_unfinishedProjects = new List<BuildTimelineEntry>();
            m_unfinishedTargets = new List<BuildTimelineEntry>();
            m_unfinishedTasks = new List<BuildTimelineEntry>();

            m_unfinishedParallelFileCompilations = new List<ParallelFileCompilation>();
        }

        // Build

        public void ProcessBuildStartEvent(BuildStartedEventArgs e)
        {
            BuildTimelineEntry entry = new BuildTimelineEntry()
            {
                StartBuildEvent = e
            };

            Debug.Assert(PerNodeRootEntries[0].Count == 0);
            PerNodeRootEntries[0].Add(entry);
        }

        public void ProcessBuildEndEvent(BuildFinishedEventArgs e)
        {
            Debug.Assert(PerNodeRootEntries[0].Count == 1);

            BuildTimelineEntry entry = PerNodeRootEntries[0][0];
            entry.EndBuildEvent = e;
        }

        // Project

        public void ProcessProjectStartEvent(ProjectStartedEventArgs e)
        {
            BuildTimelineEntry projectEntry = new BuildTimelineEntry()
            {
                StartBuildEvent = e
            };

            if(e.ParentProjectBuildEventContext.ProjectInstanceId == BuildEventContext.InvalidProjectInstanceId ||
               e.ParentProjectBuildEventContext.NodeId != e.BuildEventContext.NodeId)
            {
                PerNodeRootEntries[e.BuildEventContext.NodeId].Add(projectEntry);
            }
            else
            {
                Debug.Assert(e.ParentProjectBuildEventContext.TaskId == BuildEventContext.InvalidTaskId);
                Debug.Assert(e.ParentProjectBuildEventContext.TargetId == BuildEventContext.InvalidTargetId);

                BuildTimelineEntry parent = m_unfinishedTasks.Find(entry =>
                {
                    return entry.StartBuildEvent.BuildEventContext.ProjectContextId == e.ParentProjectBuildEventContext.ProjectContextId &&
                           entry.StartBuildEvent.BuildEventContext.ProjectInstanceId == e.ParentProjectBuildEventContext.ProjectInstanceId;
                });
                Debug.Assert(parent != null);

                parent.AddChild(projectEntry);
            }

            m_unfinishedProjects.Add(projectEntry);
        }

        public void ProcessProjectEndEvent(ProjectFinishedEventArgs e)
        {
            int index = m_unfinishedProjects.FindIndex(entry => {
                return entry.StartBuildEvent.BuildEventContext == e.BuildEventContext;
            });
            Debug.Assert(index != -1);

            m_unfinishedProjects[index].EndBuildEvent = e;
            m_unfinishedProjects.RemoveAt(index);
        }

        // Target

        public void ProcessTargetStartEvent(TargetStartedEventArgs e)
        {
            BuildTimelineEntry targetEntry = new BuildTimelineEntry()
            {
                StartBuildEvent = e
            };

            BuildTimelineEntry projectEntry = m_unfinishedProjects.Find(p => {
                return p.StartBuildEvent.BuildEventContext.ProjectContextId == e.BuildEventContext.ProjectContextId &&
                       p.StartBuildEvent.BuildEventContext.ProjectInstanceId == e.BuildEventContext.ProjectInstanceId;
            });
            Debug.Assert(projectEntry != null);

            projectEntry.AddChild(targetEntry);
            m_unfinishedTargets.Add(targetEntry);
        }
        
        public void ProcessTargetEndEvent(TargetFinishedEventArgs e)
        {
            int index = m_unfinishedTargets.FindIndex(entry => {
                return entry.StartBuildEvent.BuildEventContext == e.BuildEventContext;
            });
            Debug.Assert(index != -1);

            m_unfinishedTargets[index].EndBuildEvent = e;
            m_unfinishedTargets.RemoveAt(index);
        }

        // Task

        public void ProcessTaskStartEvent(TaskStartedEventArgs e)
        {
            BuildTimelineEntry taskEntry = new BuildTimelineEntry()
            {
                StartBuildEvent = e
            };

            BuildTimelineEntry targetEntry = m_unfinishedTargets.Find(t => {
                return t.StartBuildEvent.BuildEventContext.ProjectContextId == e.BuildEventContext.ProjectContextId &&
                       t.StartBuildEvent.BuildEventContext.ProjectInstanceId == e.BuildEventContext.ProjectInstanceId &&
                       t.StartBuildEvent.BuildEventContext.TargetId == e.BuildEventContext.TargetId;
            });
            Debug.Assert(targetEntry != null);

            targetEntry.AddChild(taskEntry);
            m_unfinishedTasks.Add(taskEntry);

            // special case: CL task
            if(e.TaskName == "CL")
            {
                ParallelFileCompilation cl = new ParallelFileCompilation(taskEntry);
                m_unfinishedParallelFileCompilations.Add(cl);
            }
        }

        public void ProcessTaskEndEvent(TaskFinishedEventArgs e)
        {
            int index = m_unfinishedTasks.FindIndex(entry => {
                return entry.StartBuildEvent.BuildEventContext == e.BuildEventContext;
            });
            Debug.Assert(index != -1);

            BuildTimelineEntry taskEntry = m_unfinishedTasks[index];
            taskEntry.EndBuildEvent = e;
            m_unfinishedTasks.RemoveAt(index);

            // special case: CL task
            if(e.TaskName == "CL")
            {
                
                index = m_unfinishedParallelFileCompilations.FindIndex(cl =>
                {
                    return cl.Parent == taskEntry;
                });
                Debug.Assert(index != -1);
                Debug.Assert(m_unfinishedParallelFileCompilations[index].IsCompleted);
                
                ParallelFileCompilations.Add(m_unfinishedParallelFileCompilations[index]);
                m_unfinishedParallelFileCompilations.RemoveAt(index);
            }
        }

        public void ProcessMessageEvent(BuildMessageEventArgs e)
        {
            BuildTimelineEntry parent = null;

            if(e.BuildEventContext.NodeId == BuildEventContext.InvalidNodeId)
            {
                parent = PerNodeRootEntries[0][0];
            }
            else
            {
                parent = m_unfinishedTasks.Find(entry => entry.StartBuildEvent.BuildEventContext == e.BuildEventContext);

                if(parent == null)
                {
                    parent = m_unfinishedTargets.Find(entry => entry.StartBuildEvent.BuildEventContext == e.BuildEventContext);

                    if(parent == null)
                    {
                        parent = m_unfinishedProjects.Find(entry => entry.StartBuildEvent.BuildEventContext == e.BuildEventContext);
                    }
                }
            }
            Debug.Assert(parent != null);

            parent.Messages.Add(e);

            // special case: CL task
            if(parent.StartBuildEvent is TaskStartedEventArgs)
            {
                TaskStartedEventArgs taskStarted = parent.StartBuildEvent as TaskStartedEventArgs;
                if(taskStarted.TaskName == "CL")
                {
                    ProcessFileCompilationMessage(e, taskStarted);
                }
            }
        }

        private void ProcessFileCompilationMessage(BuildMessageEventArgs e, TaskStartedEventArgs parent)
        {
            ParallelFileCompilation compilation = m_unfinishedParallelFileCompilations.Find(cl => cl.Parent.StartBuildEvent == parent);
            if(compilation != null)
            {
                Match compileFileStartMatch = s_CompileFileStart.Match(e.Message);
                if(compileFileStartMatch.Success)
                {
                    compilation.StartFileCompilation(compileFileStartMatch.Value, e.Timestamp);

                    return;
                }

                Match frontendFinishMatch = s_CompileFrontEndFinish.Match(e.Message);
                if(frontendFinishMatch.Success)
                {
                    string fileName = frontendFinishMatch.Groups[1].Value.Split('\\').Last();
                    compilation.EndFrontEndCompilation(fileName, e.Timestamp, e.Message);

                    return;
                }

                Match backendFinishMatch = s_CompileBackEndFinish.Match(e.Message);
                if(backendFinishMatch.Success)
                {
                    string fileName = backendFinishMatch.Groups[1].Value.Split('\\').Last();
                    compilation.EndFileCompilation(fileName, e.Timestamp, e.Message);

                    return;
                }
            }
        }

        public void CalculateParallelExecutions()
        {
            Debug.Assert(IsCompleted);

            PerNodeThreadRootList threadRootEntries = new PerNodeThreadRootList();

            foreach(var list in PerNodeRootEntries)
            {
                foreach(var rootEntry in list)
                {
                    CalculateParallelExecutionsForHierarchy(rootEntry, threadRootEntries);
                }
            }
        }

        private void CalculateParallelExecutionsForHierarchy(BuildTimelineEntry entry, PerNodeThreadRootList threadRootEntries)
        {
            if(entry.Parent != null)
            {
                List<BuildTimelineEntry> overlappingSiblings = new List<BuildTimelineEntry>();

                // take all overlapping siblings
                foreach(var sibling in entry.Parent.Children)
                {
                    if(sibling != entry && entry.OverlapsWith(sibling))
                    {
                        overlappingSiblings.Add(sibling);
                    }
                }

                // calculate thread affinity for this entry
                foreach(var sibling in overlappingSiblings)
                {
                    if(sibling.ThreadAffinity.Calculated)
                    {
                        entry.ThreadAffinity.InvalidThreadIds.Add(sibling.ThreadAffinity.ThreadId);
                    }
                }
                while(entry.ThreadAffinity.InvalidThreadIds.Contains(entry.ThreadAffinity.ThreadId))
                {
                    ++entry.ThreadAffinity.ThreadId;
                }

                // let other siblings know the calculated ThreadId is taken
                foreach(var sibling in overlappingSiblings)
                {
                    if(!sibling.ThreadAffinity.Calculated)
                    {
                        sibling.ThreadAffinity.InvalidThreadIds.Add(entry.ThreadAffinity.ThreadId);
                    }
                }
            }

            // check for overlaps with other root entries within the calculated ThreadId
            if (entry.StartBuildEvent.BuildEventContext != null && (entry.Parent == null || entry.ThreadAffinity.ThreadId != entry.Parent.ThreadAffinity.ThreadId))
            {
                Tuple<int, int> key = new Tuple<int, int>(entry.StartBuildEvent.BuildEventContext.NodeId, entry.ThreadAffinity.ThreadId);

                List<BuildTimelineEntry> rootEntriesInThreadId = null;
                threadRootEntries.TryGetValue(key, out rootEntriesInThreadId);
                if (rootEntriesInThreadId == null)
                {
                    rootEntriesInThreadId = new List<BuildTimelineEntry>();
                }

                // look for overlaps
                foreach (var rootEntry in rootEntriesInThreadId)
                {
                    Debug.Assert(rootEntry.ThreadAffinity.Calculated);

                    if (entry.OverlapsWith(rootEntry))
                    {
                        entry.ThreadAffinity.InvalidThreadIds.Add(rootEntry.ThreadAffinity.ThreadId);
                    }
                }

                // add it to the list
                rootEntriesInThreadId.Add(entry);
                threadRootEntries[key] = rootEntriesInThreadId;

                // recalculate thread affinity
                entry.ThreadAffinity.ThreadId = 0;
                while (entry.ThreadAffinity.InvalidThreadIds.Contains(entry.ThreadAffinity.ThreadId))
                {
                    ++entry.ThreadAffinity.ThreadId;
                }
            }

            // pass calculated thread affinity data to children
            foreach (var child in entry.Children)
            {
                child.ThreadAffinity.ThreadId = entry.ThreadAffinity.ThreadId;
                entry.ThreadAffinity.InvalidThreadIds = new HashSet<int>(entry.ThreadAffinity.InvalidThreadIds);
            }

            entry.ThreadAffinity.Calculated = true;

            foreach (var child in entry.Children)
            {
                CalculateParallelExecutionsForHierarchy(child, threadRootEntries);
            }
        }
    }
}
