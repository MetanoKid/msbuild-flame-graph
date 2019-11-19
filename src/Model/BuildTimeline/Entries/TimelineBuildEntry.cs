using System;
using System.IO;

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

                // find the longest common path between project and solution, then remove it from project file path
                string[] solutionPath = buildData.SolutionPath.Split(Path.DirectorySeparatorChar);
                string[] projectPath = name.Split(Path.DirectorySeparatorChar);

                int firstDifferenceIndex = -1;
                int minPathSteps = Math.Min(solutionPath.Length, projectPath.Length);
                for(int i = 0; i < minPathSteps; ++i)
                {
                    if(solutionPath[i] != projectPath[i])
                    {
                        firstDifferenceIndex = i;
                        break;
                    }
                }

                if(firstDifferenceIndex >= 0)
                {
                    name = String.Join(Path.DirectorySeparatorChar.ToString(), projectPath, firstDifferenceIndex, projectPath.Length - firstDifferenceIndex);
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
