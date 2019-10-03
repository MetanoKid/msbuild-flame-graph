using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Model.BuildTimeline
{
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

        public Timeline Build()
        {
            TimelineBuilderContext context = ProcessEvents(m_buildData.Events);
            Debug.Assert(!context.HasOpenBuilds);
            Debug.Assert(!context.HasOpenProjects);
            Debug.Assert(!context.HasOpenTargets);
            Debug.Assert(!context.HasOpenTasks);
            Debug.Assert(context.RootEntry != null);

            Timeline timeline = BuildTimelineFrom(m_buildData, context);
            CalculateParallelEntries(timeline);

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
                else
                {
                    Debug.Assert(messageContext.ProjectId != null);
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
            }

            Debug.Assert(parentEntry != null);
            parentEntry.AddChild(e);
        }

        private Timeline BuildTimelineFrom(BuildData buildData, TimelineBuilderContext context)
        {
            // TODO: extract processor count from build data
            Timeline timeline = new Timeline(Environment.ProcessorCount);

            // build belongs to NodeId 0, as reported by MSBuild, while other entries start at NodeId 1
            Debug.Assert(context.RootEntry.Context == null);
            TimelineEntry buildTimelineEntry = new TimelineEntry(context.RootEntry);
            timeline.AddRoot(buildTimelineEntry);

            // process other entries
            BuildTimelineEntries(timeline, buildTimelineEntry);

            return timeline;
        }

        private void BuildTimelineEntries(Timeline timeline, TimelineEntry parent)
        {
            foreach(BuildEntry childEntry in parent.BuildEntry.ChildEntries)
            {
                TimelineEntry timelineEntry = new TimelineEntry(childEntry);

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
                BuildTimelineEntries(timeline, timelineEntry);
            }
        }
        
        private void CalculateParallelEntries(Timeline timeline)
        {
            foreach(List<TimelineEntry> rootsInNode in timeline.PerNodeRootEntries)
            {
                // TODO: iterate over roots, assign different "thread IDs" when they overlap
                //       be careful when assigning a new "thread ID", it can also overlap!
                //       children live within the same "thread ID" as their parent, unless
                //       they overlap with their siblings, when we have to assign a new "thread ID"
                //       and be careful with overlaps within there
            }
        }
    }
}
