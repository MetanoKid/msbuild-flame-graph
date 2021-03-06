﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BuildTimeline
{
    // alias for the group of root timeline entries per <node id, calculated thread id>
    using PerNodeThreadRootEntries = Dictionary<Tuple<int, int>, List<TimelineEntry>>;

    class TimelineBuilderContext
    {
        public List<BuildEntry> OpenBuildEntries;
        public List<BuildEntry> OpenProjectEntries;
        public List<BuildEntry> OpenTargetEntries;
        public List<BuildEntry> OpenTaskEntries;

        public bool HasOpenBuilds
        {
            get
            {
                return OpenBuildEntries.Count > 0;
            }
        }

        public bool HasOpenProjects
        {
            get
            {
                return OpenProjectEntries.Count > 0;
            }
        }

        public bool HasOpenTargets
        {
            get
            {
                return OpenTargetEntries.Count > 0;
            }
        }

        public bool HasOpenTasks
        {
            get
            {
                return OpenTaskEntries.Count > 0;
            }
        }

        public BuildEntry RootEntry { get; set; }

        public TimelineBuilderContext()
        {
            OpenBuildEntries = new List<BuildEntry>();
            OpenProjectEntries = new List<BuildEntry>();
            OpenTargetEntries = new List<BuildEntry>();
            OpenTaskEntries = new List<BuildEntry>();
        }
    }

    public class TimelineBuilder
    {
        private BuildData m_buildData;

        public TimelineBuilder(BuildData buildData)
        {
            m_buildData = buildData;
        }

        public Timeline Build(TimelineEntryPostProcessor.Processor perEntryPostProcessors)
        {
            TimelineBuilderContext context = ProcessEvents(m_buildData.Events);
            Debug.Assert(!context.HasOpenBuilds);
            Debug.Assert(!context.HasOpenProjects);
            Debug.Assert(!context.HasOpenTargets);
            Debug.Assert(!context.HasOpenTasks);
            Debug.Assert(context.RootEntry != null);

            Timeline timeline = BuildTimelineFrom(m_buildData, context);
            if(perEntryPostProcessors != null)
            {
                PostProcess(timeline, perEntryPostProcessors);
            }

            CalculateParallelEntries(timeline);

            EnsureNoEntryOverflowsParent(timeline);

            return timeline;
        }

        private TimelineBuilderContext ProcessEvents(List<Event> events)
        {
            TimelineBuilderContext context = new TimelineBuilderContext();

            foreach (Event e in events)
            {
                if (e is BuildStartedEvent)
                {
                    ProcessBuildStartEvent(e as BuildStartedEvent, context);
                }
                else if (e is BuildFinishedEvent)
                {
                    ProcessBuildEndEvent(e as BuildFinishedEvent, context);
                }
                else if (e is ProjectStartedEvent)
                {
                    ProcessProjectStartEvent(e as ProjectStartedEvent, context);
                }
                else if (e is ProjectFinishedEvent)
                {
                    ProcessProjectEndEvent(e as ProjectFinishedEvent, context);
                }
                else if (e is TargetStartedEvent)
                {
                    ProcessTargetStartEvent(e as TargetStartedEvent, context);
                }
                else if (e is TargetFinishedEvent)
                {
                    ProcessTargetEndEvent(e as TargetFinishedEvent, context);
                }
                else if (e is TaskStartedEvent)
                {
                    ProcessTaskStartEvent(e as TaskStartedEvent, context);
                }
                else if (e is TaskFinishedEvent)
                {
                    ProcessTaskEndEvent(e as TaskFinishedEvent, context);
                }
                else if (e is MessageEvent)
                {
                    ProcessMessageEvent(e as MessageEvent, context);
                }
                else if (e is WarningEvent)
                {
                    ProcessMessageEvent(e as WarningEvent, context);
                }
                else if (e is ErrorEvent)
                {
                    ProcessMessageEvent(e as ErrorEvent, context);
                }
            }

            return context;
        }

        private void ProcessBuildStartEvent(BuildStartedEvent e, TimelineBuilderContext context)
        {
            Debug.Assert(!context.HasOpenBuilds);
            Debug.Assert(!context.HasOpenProjects);
            Debug.Assert(!context.HasOpenTargets);
            Debug.Assert(!context.HasOpenTasks);
            Debug.Assert(context.RootEntry == null);
            
            BuildEntry buildEntry = new BuildEntry(e);
            context.OpenBuildEntries.Add(buildEntry);
            context.RootEntry = buildEntry;
        }

        private void ProcessBuildEndEvent(BuildFinishedEvent e, TimelineBuilderContext context)
        {
            Debug.Assert(context.OpenBuildEntries.Count == 1);
            Debug.Assert(!context.HasOpenProjects);
            Debug.Assert(!context.HasOpenTargets);
            Debug.Assert(!context.HasOpenTasks);

            BuildEntry buildEntry = context.OpenBuildEntries[0];
            Debug.Assert(buildEntry != null);

            buildEntry.CloseWith(e);
            context.OpenBuildEntries.Remove(buildEntry);
        }

        private void ProcessProjectStartEvent(ProjectStartedEvent e, TimelineBuilderContext context)
        {
            Debug.Assert(context.HasOpenBuilds);

            BuildEntry projectEntry = new BuildEntry(e);
            context.OpenProjectEntries.Add(projectEntry);

            // projects always have a parent, we know which via parent event context
            BuildEntry parentEntry = null;

            // no parent event context mean we've been spawned from the build itself
            if(e.ParentEventContext == null)
            {
                parentEntry = context.RootEntry;
            }
            else
            {
                // a project's parent is a task, although the parent context refers to the task project's
                parentEntry = context.OpenTaskEntries.Find(taskEntry =>
                {
                    TaskEventContext taskContext = taskEntry.Context as TaskEventContext;
                    Debug.Assert(taskContext != null);

                    return taskContext.ContextId == e.ParentEventContext.ContextId &&
                           taskContext.ProjectId == e.ParentEventContext.ProjectId;
                });
                
                // finding no task with matching data means our build was requested by a task that's already completed
                // we may have some luck finding the project that spawned that task
                if(parentEntry == null)
                {
                    // our build couldn't be started before within the same node, hence it was scheduled
                    Debug.Assert(e.Context.NodeId != e.ParentEventContext.NodeId);

                    parentEntry = context.OpenProjectEntries.Find(otherProjectEntry =>
                    {
                        ProjectEventContext projectContext = otherProjectEntry.Context as ProjectEventContext;
                        Debug.Assert(projectContext != null);

                        return projectContext.ContextId == e.ParentEventContext.ContextId &&
                               projectContext.ProjectId == e.ParentEventContext.ProjectId;
                    });

                    // the project may have also finished
                    if(parentEntry == null)
                    {
                        // just consider the build itself our parent
                        parentEntry = context.RootEntry;
                    }
                }
            }

            Debug.Assert(parentEntry != null);
            parentEntry.AddChild(e);
            parentEntry.AddChild(projectEntry);
        }

        private void ProcessProjectEndEvent(ProjectFinishedEvent e, TimelineBuilderContext context)
        {
            Debug.Assert(context.HasOpenBuilds);

            BuildEntry projectEntry = context.OpenProjectEntries.Find(_ => _.Context.Equals(e.Context));
            Debug.Assert(projectEntry != null);

            projectEntry.Parent.AddChild(e);
            projectEntry.CloseWith(e);
            context.OpenProjectEntries.Remove(projectEntry);
        }

        private void ProcessTargetStartEvent(TargetStartedEvent e, TimelineBuilderContext context)
        {
            Debug.Assert(context.HasOpenBuilds);
            Debug.Assert(context.HasOpenProjects);

            BuildEntry targetEntry = new BuildEntry(e);
            context.OpenTargetEntries.Add(targetEntry);

            // a target's parent is a Project
            TargetEventContext targetContext = targetEntry.Context as TargetEventContext;
            Debug.Assert(targetContext != null);
            BuildEntry parentEntry = context.OpenProjectEntries.Find(projectEntry =>
            {
                ProjectEventContext projectContext = projectEntry.Context as ProjectEventContext;
                Debug.Assert(projectContext != null);

                return projectContext.ContextId == targetContext.ContextId &&
                       projectContext.ProjectId == targetContext.ProjectId;
            });

            Debug.Assert(parentEntry != null);
            parentEntry.AddChild(e);
            parentEntry.AddChild(targetEntry);
        }

        private void ProcessTargetEndEvent(TargetFinishedEvent e, TimelineBuilderContext context)
        {
            Debug.Assert(context.HasOpenBuilds);
            Debug.Assert(context.HasOpenProjects);

            BuildEntry targetEntry = context.OpenTargetEntries.Find(_ => _.Context.Equals(e.Context));
            Debug.Assert(targetEntry != null);

            targetEntry.Parent.AddChild(e);
            targetEntry.CloseWith(e);
            context.OpenTargetEntries.Remove(targetEntry);
        }

        private void ProcessTaskStartEvent(TaskStartedEvent e, TimelineBuilderContext context)
        {
            Debug.Assert(context.HasOpenBuilds);
            Debug.Assert(context.HasOpenProjects);
            Debug.Assert(context.HasOpenTargets);

            BuildEntry taskEntry = new BuildEntry(e);
            context.OpenTaskEntries.Add(taskEntry);

            // a task's parent is a Target
            TaskEventContext taskContext = taskEntry.Context as TaskEventContext;
            Debug.Assert(taskContext != null);
            BuildEntry parentEntry = context.OpenTargetEntries.Find(targetEntry =>
            {
                TargetEventContext targetContext = targetEntry.Context as TargetEventContext;
                Debug.Assert(targetContext != null);

                return targetContext.ContextId == taskContext.ContextId &&
                       targetContext.ProjectId == taskContext.ProjectId &&
                       targetContext.TargetId  == taskContext.TargetId;
            });

            Debug.Assert(parentEntry != null);
            parentEntry.AddChild(e);
            parentEntry.AddChild(taskEntry);
        }

        private void ProcessTaskEndEvent(TaskFinishedEvent e, TimelineBuilderContext context)
        {
            Debug.Assert(context.HasOpenBuilds);
            Debug.Assert(context.HasOpenProjects);
            Debug.Assert(context.HasOpenTargets);

            BuildEntry taskEntry = context.OpenTaskEntries.Find(_ => _.Context.Equals(e.Context));
            Debug.Assert(taskEntry != null);

            taskEntry.Parent.AddChild(e);
            taskEntry.CloseWith(e);
            context.OpenTaskEntries.Remove(taskEntry);
        }

        private void ProcessMessageEvent(MessageEvent e, TimelineBuilderContext context)
        {
            Debug.Assert(context.HasOpenBuilds);

            // a message can be executed as part of any entry: build, project, target or task
            BuildEntry parentEntry = null;

            // part of the build?
            if(e.Context == null)
            {
                parentEntry = context.RootEntry;
            }
            else
            {
                MessageEventContext messageContext = e.Context as MessageEventContext;

                // part of a task?
                if(messageContext.TaskId != null)
                {
                    Debug.Assert(messageContext.ProjectId != null);
                    Debug.Assert(messageContext.TargetId != null);

                    parentEntry = context.OpenTaskEntries.Find(taskEntry =>
                    {
                        TaskEventContext taskContext = taskEntry.Context as TaskEventContext;
                        Debug.Assert(taskContext != null);

                        return taskContext.NodeId == messageContext.NodeId &&
                               taskContext.ContextId == messageContext.ContextId &&
                               taskContext.ProjectId == messageContext.ProjectId &&
                               taskContext.TargetId == messageContext.TargetId &&
                               taskContext.TaskId == messageContext.TaskId;
                    });
                }
                // part of a target?
                else if(messageContext.TargetId != null)
                {
                    Debug.Assert(messageContext.ProjectId != null);
                    Debug.Assert(messageContext.TaskId == null);

                    parentEntry = context.OpenTargetEntries.Find(targetEntry =>
                    {
                        TargetEventContext targetContext = targetEntry.Context as TargetEventContext;
                        Debug.Assert(targetContext != null);

                        return targetContext.NodeId == messageContext.NodeId &&
                               targetContext.ContextId == messageContext.ContextId &&
                               targetContext.ProjectId == messageContext.ProjectId &&
                               targetContext.TargetId == messageContext.TargetId;
                    });
                }
                // part of a project?
                else if(messageContext.ProjectId != null)
                {
                    Debug.Assert(messageContext.TargetId == null);
                    Debug.Assert(messageContext.TaskId == null);

                    parentEntry = context.OpenProjectEntries.Find(projectEntry =>
                    {
                        ProjectEventContext projectContext = projectEntry.Context as ProjectEventContext;
                        Debug.Assert(projectContext != null);

                        return projectContext.NodeId == messageContext.NodeId &&
                               projectContext.ContextId == messageContext.ContextId &&
                               projectContext.ProjectId == messageContext.ProjectId;
                    });
                }
                // part of the build itself?
                else
                {
                    parentEntry = context.RootEntry;
                }
            }

            Debug.Assert(parentEntry != null);
            parentEntry.AddChild(e);
        }

        private Timeline BuildTimelineFrom(BuildData buildData, TimelineBuilderContext context)
        {
            Timeline timeline = new Timeline(buildData.BuildConfiguration.MaxParallelProjects);

            // build belongs to NodeId 0, as reported by MSBuild, while other entries start at NodeId 1
            Debug.Assert(context.RootEntry.Context == null);
            TimelineBuildEntry topLevelTimelineBuildEntry = new TimelineBuildEntry(context.RootEntry, buildData);
            timeline.AddRoot(topLevelTimelineBuildEntry);

            // process other entries
            BuildTimelineEntries(timeline, topLevelTimelineBuildEntry, buildData);

            return timeline;
        }

        private void BuildTimelineEntries(Timeline timeline, TimelineBuildEntry parent, BuildData buildData)
        {
            foreach(BuildEntry childEntry in parent.BuildEntry.ChildEntries)
            {
                TimelineBuildEntry timelineEntry = new TimelineBuildEntry(childEntry, buildData);

                // same NodeId? there's a TimelineEntry hierarchy
                if(parent.BuildEntry.Context?.NodeId == childEntry.Context.NodeId)
                {
                    parent.AddChild(timelineEntry);
                }
                // different NodeId? we've got a new root
                else
                {
                    timeline.AddRoot(timelineEntry);
                }

                Debug.Assert(timelineEntry != null);
                BuildTimelineEntries(timeline, timelineEntry, buildData);
            }
        }
        
        private void CalculateParallelEntries(Timeline timeline)
        {
            PerNodeThreadRootEntries calculatedPerNodeThreadRootEntries = new PerNodeThreadRootEntries();

            foreach(List<TimelineEntry> rootsInNode in timeline.PerNodeRootEntries)
            {
                foreach(TimelineEntry root in rootsInNode)
                {
                    CalculateParallelEntriesFor(root, calculatedPerNodeThreadRootEntries);
                }
            }
        }

        private void CalculateParallelEntriesFor(TimelineEntry entry, PerNodeThreadRootEntries nodeThreadRootEntries)
        {
            if(entry is TimelineBuildEntry)
            {
                entry.ThreadAffinity.SetParameters(entry.ThreadAffinity.ThreadId, 0, ThreadAffinity.s_OffsetMSBuildEntries);
            }

            if(entry.Parent != null)
            {
                // retrieve all of the overlapping siblings
                List<TimelineEntry> overlappingSiblings = new List<TimelineEntry>();

                foreach(TimelineEntry sibling in entry.Parent.ChildEntries)
                {
                    if(entry != sibling && entry.OverlapsWith(sibling))
                    {
                        overlappingSiblings.Add(sibling);
                    }
                }

                // we may have calculated some of the siblings, take their information into account
                foreach(TimelineEntry overlappingSibling in overlappingSiblings)
                {
                    if(overlappingSibling.ThreadAffinity.Calculated)
                    {
                        entry.ThreadAffinity.AddInvalid(overlappingSibling.ThreadAffinity.ThreadId);
                    }
                }
            }

            // also, check the overlapping root entries from each calculated thread within the same node
            foreach(var pair in nodeThreadRootEntries)
            {
                if(pair.Key.Item1 == entry.NodeId)
                {
                    foreach(TimelineEntry root in pair.Value)
                    {
                        Debug.Assert(root.ThreadAffinity.Calculated);
                        if(!root.IsAncestorOf(entry) && entry.OverlapsWith(root))
                        {
                            entry.ThreadAffinity.AddInvalid(root.ThreadAffinity.ThreadId);
                        }
                    }
                }
            }

            // now calculate where we think the entry was executed
            entry.ThreadAffinity.Calculate();

            // are we a new root?
            if(entry.Parent == null || entry.ThreadAffinity.ThreadId != entry.Parent.ThreadAffinity.ThreadId)
            {
                // get or create root list for the <node id, calculated thread id>
                Tuple<int, int> key = new Tuple<int, int>(entry.NodeId, entry.ThreadAffinity.ThreadId);
                List<TimelineEntry> rootsInNodeThread = null;
                if(!nodeThreadRootEntries.TryGetValue(key, out rootsInNodeThread))
                {
                    rootsInNodeThread = new List<TimelineEntry>();
                    nodeThreadRootEntries[key] = rootsInNodeThread;
                }

                rootsInNodeThread.Add(entry);
            }

            // now that we've decided where the entry was executed, transfer this data to child entries
            foreach(TimelineEntry child in entry.ChildEntries)
            {
                child.ThreadAffinity.InheritDataFrom(entry.ThreadAffinity);
            }

            // continue with child entries
            foreach(TimelineEntry child in entry.ChildEntries)
            {
                CalculateParallelEntriesFor(child, nodeThreadRootEntries);
            }
        }

        private void PostProcess(Timeline timeline, TimelineEntryPostProcessor.Processor perEntryPostProcessors)
        {
            foreach(List<TimelineEntry> rootsInNode in timeline.PerNodeRootEntries)
            {
                foreach(TimelineEntry root in rootsInNode)
                {
                    PostProcess(root, perEntryPostProcessors);
                }
            }
        }

        private void PostProcess(TimelineEntry entry, TimelineEntryPostProcessor.Processor perEntryPostProcessors)
        {
            perEntryPostProcessors(entry);

            foreach(TimelineEntry child in entry.ChildEntries)
            {
                PostProcess(child, perEntryPostProcessors);
            }
        }

        private void EnsureNoEntryOverflowsParent(Timeline timeline)
        {
            foreach (List<TimelineEntry> rootsInNode in timeline.PerNodeRootEntries)
            {
                foreach (TimelineEntry root in rootsInNode)
                {
                    EnsureNoEntryOverflowsParent(root);
                }
            }
        }

        private void EnsureNoEntryOverflowsParent(TimelineEntry entry)
        {
            entry.FitChildEntries();
            /*
            if (entry.ChildEntries.Count > 0)
            {
                DateTime childrenFirstStartTimestamp = entry.ChildEntries.First().StartTimestamp;
                DateTime childrenLastEndTimestamp = entry.ChildEntries.Last().EndTimestamp;

                // no child should start before its parent! overflow occurs on end timestamps
                Debug.Assert(childrenFirstStartTimestamp >= entry.StartTimestamp);
                
                if(childrenLastEndTimestamp > entry.EndTimestamp)
                {
                    DateTime minDate = entry.StartTimestamp;
                    DateTime maxDate = childrenLastEndTimestamp;
                    TimeSpan elapsedWithOverflow = maxDate - minDate;
                    TimeSpan elapsedParent = entry.ElapsedTime;
                    
                    double ratio = (double) elapsedParent.Ticks / elapsedWithOverflow.Ticks;
                    entry.ScaleChildrenTimestamps(ratio);
                }
                else
                {
                    foreach(TimelineEntry child in entry.ChildEntries)
                    {
                        EnsureNoEntryOverflowsParent(child);
                    }
                }
            }
            */
        }
    }
}
