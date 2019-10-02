using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Model.BuildTimeline
{
    class TimelineBuilderContext
    {
        public List<Entry> OpenBuildEntries;
        public List<Entry> OpenProjectEntries;
        public List<Entry> OpenTargetEntries;
        public List<Entry> OpenTaskEntries;

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

        public Entry RootEntry { get; set; }

        public TimelineBuilderContext()
        {
            OpenBuildEntries = new List<Entry>();
            OpenProjectEntries = new List<Entry>();
            OpenTargetEntries = new List<Entry>();
            OpenTaskEntries = new List<Entry>();
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
            Timeline timeline = new Timeline(0/*m_buildData.NodeCount*/);
            TimelineBuilderContext context = new TimelineBuilderContext();
            
            foreach(Event e in m_buildData.Events)
            {
                if (e is BuildStartedEvent)
                {
                    ProcessBuildStartEvent(e as BuildStartedEvent, timeline, context);
                }
                else if (e is BuildFinishedEvent)
                {
                    ProcessBuildEndEvent(e as BuildFinishedEvent, timeline, context);
                }
                else if (e is ProjectStartedEvent)
                {
                    ProcessProjectStartEvent(e as ProjectStartedEvent, timeline, context);
                }
                else if (e is ProjectFinishedEvent)
                {
                    ProcessProjectEndEvent(e as ProjectFinishedEvent, timeline, context);
                }
                else if (e is TargetStartedEvent)
                {
                    ProcessTargetStartEvent(e as TargetStartedEvent, timeline, context);
                }
                else if (e is TargetFinishedEvent)
                {
                    ProcessTargetEndEvent(e as TargetFinishedEvent, timeline, context);
                }
                else if (e is TaskStartedEvent)
                {
                    ProcessTaskStartEvent(e as TaskStartedEvent, timeline, context);
                }
                else if (e is TaskFinishedEvent)
                {
                    ProcessTaskEndEvent(e as TaskFinishedEvent, timeline, context);
                }
                else if (e is MessageEvent)
                {
                    ProcessMessageEvent(e as MessageEvent, timeline, context);
                }
            }

            Debug.Assert(!context.HasOpenBuilds);
            Debug.Assert(!context.HasOpenProjects);
            Debug.Assert(!context.HasOpenTargets);
            Debug.Assert(!context.HasOpenTasks);
            Debug.Assert(context.RootEntry != null);

            //timeline.CalculateParallelExecutions();

            return timeline;
        }

        private void ProcessBuildStartEvent(BuildStartedEvent e, Timeline timeline, TimelineBuilderContext context)
        {
            Debug.Assert(!context.HasOpenBuilds);
            Debug.Assert(!context.HasOpenProjects);
            Debug.Assert(!context.HasOpenTargets);
            Debug.Assert(!context.HasOpenTasks);
            Debug.Assert(context.RootEntry == null);

            // open entry
            Entry buildEntry = new Entry(e);
            context.OpenBuildEntries.Add(buildEntry);
            context.RootEntry = buildEntry;
        }

        private void ProcessBuildEndEvent(BuildFinishedEvent e, Timeline timeline, TimelineBuilderContext context)
        {
            Debug.Assert(context.OpenBuildEntries.Count == 1);
            Debug.Assert(!context.HasOpenProjects);
            Debug.Assert(!context.HasOpenTargets);
            Debug.Assert(!context.HasOpenTasks);
            
            Entry buildEntry = context.OpenBuildEntries[0];
            Debug.Assert(buildEntry != null);

            buildEntry.CloseWith(e);
            context.OpenBuildEntries.Remove(buildEntry);
        }

        private void ProcessProjectStartEvent(ProjectStartedEvent e, Timeline timeline, TimelineBuilderContext context)
        {
            Debug.Assert(context.HasOpenBuilds);
            
            Entry projectEntry = new Entry(e);
            context.OpenProjectEntries.Add(projectEntry);

            // projects always have a parent, we know which via parent event context
            Entry parentEntry = null;

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

        private void ProcessProjectEndEvent(ProjectFinishedEvent e, Timeline timeline, TimelineBuilderContext context)
        {
            Debug.Assert(context.HasOpenBuilds);

            Entry projectEntry = context.OpenProjectEntries.Find(_ => _.Context.Equals(e.Context));
            Debug.Assert(projectEntry != null);

            projectEntry.Parent.AddChild(e);
            projectEntry.CloseWith(e);
            context.OpenProjectEntries.Remove(projectEntry);
        }

        private void ProcessTargetStartEvent(TargetStartedEvent e, Timeline timeline, TimelineBuilderContext context)
        {
            Debug.Assert(context.HasOpenBuilds);
            Debug.Assert(context.HasOpenProjects);

            Entry targetEntry = new Entry(e);
            context.OpenTargetEntries.Add(targetEntry);

            // a target's parent is a Project
            TargetEventContext targetContext = targetEntry.Context as TargetEventContext;
            Debug.Assert(targetContext != null);
            Entry parentEntry = context.OpenProjectEntries.Find(projectEntry =>
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

        private void ProcessTargetEndEvent(TargetFinishedEvent e, Timeline timeline, TimelineBuilderContext context)
        {
            Debug.Assert(context.HasOpenBuilds);
            Debug.Assert(context.HasOpenProjects);

            Entry targetEntry = context.OpenTargetEntries.Find(_ => _.Context.Equals(e.Context));
            Debug.Assert(targetEntry != null);

            targetEntry.Parent.AddChild(e);
            targetEntry.CloseWith(e);
            context.OpenTargetEntries.Remove(targetEntry);
        }

        private void ProcessTaskStartEvent(TaskStartedEvent e, Timeline timeline, TimelineBuilderContext context)
        {
            Debug.Assert(context.HasOpenBuilds);
            Debug.Assert(context.HasOpenProjects);
            Debug.Assert(context.HasOpenTargets);

            Entry taskEntry = new Entry(e);
            context.OpenTaskEntries.Add(taskEntry);

            // a task's parent is a Target
            TaskEventContext taskContext = taskEntry.Context as TaskEventContext;
            Debug.Assert(taskContext != null);
            Entry parentEntry = context.OpenTargetEntries.Find(targetEntry =>
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

        private void ProcessTaskEndEvent(TaskFinishedEvent e, Timeline timeline, TimelineBuilderContext context)
        {
            Debug.Assert(context.HasOpenBuilds);
            Debug.Assert(context.HasOpenProjects);
            Debug.Assert(context.HasOpenTargets);

            Entry taskEntry = context.OpenTaskEntries.Find(_ => _.Context.Equals(e.Context));
            Debug.Assert(taskEntry != null);

            taskEntry.Parent.AddChild(e);
            taskEntry.CloseWith(e);
            context.OpenTaskEntries.Remove(taskEntry);
        }

        private void ProcessMessageEvent(MessageEvent e, Timeline timeline, TimelineBuilderContext context)
        {
            Debug.Assert(context.HasOpenBuilds);

            // a message can be executed as part of any entry: build, project, target or task
            Entry parentEntry = null;

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
    }
}
