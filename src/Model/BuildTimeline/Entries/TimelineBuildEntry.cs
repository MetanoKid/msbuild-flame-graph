using System;

namespace Model.BuildTimeline
{
    public class TimelineBuildEntry : TimelineEntry
    {
        public BuildEntry BuildEntry { get; private set; }

        public TimelineBuildEntry(BuildEntry buildEntry, BuildData buildData) :
            base(ExtractNameFrom(buildEntry, buildData),
                buildEntry.Context == null ? 0 : buildEntry.Context.NodeId,
                buildEntry.StartEvent.Timestamp,
                buildEntry.EndEvent.Timestamp)
        {
            BuildEntry = buildEntry;
        }

        private static string ExtractNameFrom(BuildEntry buildEntry, BuildData buildData)
        {
            Event e = buildEntry.StartEvent;
            string name = null;

            if (e is BuildStartedEvent)
            {
                name = $"{buildData.Target} - {buildData.Configuration}|{buildData.Platform} - Max parallel: {buildData.MaxParallelProjects} projects, {buildData.MaxParallelCLPerProject} CL tasks per project - {buildData.SolutionPath}";
            }
            else if (e is ProjectStartedEvent)
            {
                name = (e as ProjectStartedEvent).ProjectFile;

                // find the longest common prefix between the project path and the solution path, then remove it from project's
                int minLength = Math.Min(name.Length, buildData.SolutionPath.Length);
                int firstDifferentCharacterIndex = 0;
                while(firstDifferentCharacterIndex < minLength &&
                      name[firstDifferentCharacterIndex] == buildData.SolutionPath[firstDifferentCharacterIndex])
                {
                    ++firstDifferentCharacterIndex;
                }

                // only remove prefix when we've found it (and there's any character left after the removal)
                if(firstDifferentCharacterIndex < minLength)
                {
                    name = name.Substring(firstDifferentCharacterIndex);
                }
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
