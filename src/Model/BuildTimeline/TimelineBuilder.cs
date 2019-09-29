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

            //timeline.CalculateParallelExecutions();

            return timeline;
        }

        private void ProcessBuildStartEvent(BuildStartedEvent e, TimelineBuilderContext context)
        {
            Debug.Assert(!context.HasOpenBuilds);
            Debug.Assert(!context.HasOpenProjects);
            Debug.Assert(!context.HasOpenTargets);
            Debug.Assert(!context.HasOpenTasks);

            Entry entry = new Entry()
            {
                StartEvent = e,
                Parent = null,
            };
            context.OpenBuildEntries.Add(entry);
        }

        private void ProcessBuildEndEvent(BuildFinishedEvent e, TimelineBuilderContext context)
        {
            Debug.Assert(context.OpenBuildEntries.Count == 1);
            Debug.Assert(!context.HasOpenProjects);
            Debug.Assert(!context.HasOpenTargets);
            Debug.Assert(!context.HasOpenTasks);

            Entry entry = context.OpenBuildEntries.Find(_ => _.StartEvent.Context == e.Context);
            Debug.Assert(entry != null);
            entry.EndEvent = e;
        }

        private void ProcessProjectStartEvent(ProjectStartedEvent e, TimelineBuilderContext context)
        {
            Debug.Assert(context.HasOpenBuilds);
        }

        private void ProcessProjectEndEvent(ProjectFinishedEvent e, TimelineBuilderContext context)
        {
            Debug.Assert(context.HasOpenBuilds);
        }

        private void ProcessTargetStartEvent(TargetStartedEvent e, TimelineBuilderContext context)
        {
            Debug.Assert(context.HasOpenBuilds);
            Debug.Assert(context.HasOpenProjects);
        }

        private void ProcessTargetEndEvent(TargetFinishedEvent e, TimelineBuilderContext context)
        {
            Debug.Assert(context.HasOpenBuilds);
            Debug.Assert(context.HasOpenProjects);
        }

        private void ProcessTaskStartEvent(TaskStartedEvent e, TimelineBuilderContext context)
        {
            Debug.Assert(context.HasOpenBuilds);
            Debug.Assert(context.HasOpenProjects);
            Debug.Assert(context.HasOpenTargets);
        }

        private void ProcessTaskEndEvent(TaskFinishedEvent e, TimelineBuilderContext context)
        {
            Debug.Assert(context.HasOpenBuilds);
            Debug.Assert(context.HasOpenProjects);
            Debug.Assert(context.HasOpenTargets);
        }

        private void ProcessMessageEvent(MessageEvent e, TimelineBuilderContext context)
        {
            Debug.Assert(context.HasOpenBuilds);
        }
    }
}
