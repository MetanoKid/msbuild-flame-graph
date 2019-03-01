using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Model
{
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

        public BuildTimelineEntry()
        {
            ThreadAffinity = new CalculatedThreadAffinity();
            Children = new List<BuildTimelineEntry>();
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

    public class BuildTimeline
    {
        public List<List<BuildTimelineEntry>> PerNodeRootEntries
        {
            get;
            private set;
        }

        private List<BuildTimelineEntry> m_unfinishedProjects;
        private List<BuildTimelineEntry> m_unfinishedTargets;
        private List<BuildTimelineEntry> m_unfinishedTasks;

        public BuildTimeline(int totalNodeCount)
        {
            PerNodeRootEntries = new List<List<BuildTimelineEntry>>();
            
            // node 0 to be used by top-level build element only
            for(int i = 0; i < totalNodeCount + 1; ++i)
            {
                PerNodeRootEntries.Add(new List<BuildTimelineEntry>());
            }

            m_unfinishedProjects = new List<BuildTimelineEntry>();
            m_unfinishedTargets = new List<BuildTimelineEntry>();
            m_unfinishedTasks = new List<BuildTimelineEntry>();
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
        }

        public void ProcessTaskEndEvent(TaskFinishedEventArgs e)
        {
            int index = m_unfinishedTasks.FindIndex(entry => {
                return entry.StartBuildEvent.BuildEventContext == e.BuildEventContext;
            });
            Debug.Assert(index != -1);

            m_unfinishedTasks[index].EndBuildEvent = e;
            m_unfinishedTasks.RemoveAt(index);
        }

        public bool IsCompleted()
        {
            return m_unfinishedProjects.Count == 0 &&
                   m_unfinishedTargets.Count == 0 &&
                   m_unfinishedTasks.Count == 0;
        }

        public void CalculateParallelExecutions()
        {
            Debug.Assert(IsCompleted());

            foreach(var list in PerNodeRootEntries)
            {
                foreach(var rootEntry in list)
                {
                    CalculateParallelExecutionsForHierarchy(rootEntry);
                }
            }
        }

        private void CalculateParallelExecutionsForHierarchy(BuildTimelineEntry entry)
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

            // pass calculated thread affinity data to children
            foreach (var child in entry.Children)
            {
                child.ThreadAffinity.ThreadId = entry.ThreadAffinity.ThreadId;
                entry.ThreadAffinity.InvalidThreadIds = new HashSet<int>(entry.ThreadAffinity.InvalidThreadIds);
            }

            entry.ThreadAffinity.Calculated = true;

            foreach (var child in entry.Children)
            {
                CalculateParallelExecutionsForHierarchy(child);
            }
        }
    }
}
