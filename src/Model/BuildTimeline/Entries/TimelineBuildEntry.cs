using System;

namespace Model.BuildTimeline
{
    public class TimelineBuildEntry : TimelineEntry
    {
        public BuildEntry BuildEntry { get; private set; }

        public TimelineBuildEntry(BuildEntry buildEntry) :
            base(ExtractNameFrom(buildEntry),
                buildEntry.Context == null ? 0 : buildEntry.Context.NodeId,
                buildEntry.StartEvent.Timestamp,
                buildEntry.EndEvent.Timestamp)
        {
            BuildEntry = buildEntry;
        }

        private static string ExtractNameFrom(BuildEntry buildEntry)
        {
            Event e = buildEntry.StartEvent;
            string name = null;

            if (e is BuildStartedEvent)
            {
                // TODO: display build file, requested configuration, platform and target?
                //       may need to have a reference to that info, then?
                name = "Build data";
            }
            else if (e is ProjectStartedEvent)
            {
                name = (e as ProjectStartedEvent).ProjectFile;
            }
            else if (e is TargetStartedEvent)
            {
                name = (e as TargetStartedEvent).TargetName;
            }
            else if (e is TaskStartedEvent)
            {
                name = (e as TaskStartedEvent).TaskName;
            }

            return name;
        }
    }
}
