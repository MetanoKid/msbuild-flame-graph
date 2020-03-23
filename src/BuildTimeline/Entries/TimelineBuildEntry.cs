using System;
using System.IO;
using System.Text;

namespace BuildTimeline
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
                StringBuilder builder = new StringBuilder();
                builder.Append($"{buildData.BuildConfiguration.Target}");
                builder.Append($" - {buildData.BuildConfiguration.Configuration}|{buildData.BuildConfiguration.Platform}");
                builder.Append($" - Max parallel: {buildData.BuildConfiguration.MaxParallelProjects} projects, {buildData.BuildConfiguration.MaxParallelCLTasksPerProject} CL tasks per project");
                builder.Append($" - {buildData.BuildConfiguration.SolutionPath}");

                name = builder.ToString();
            }
            else if (e is ProjectStartedEvent)
            {
                name = (e as ProjectStartedEvent).ProjectFile;

                // find the longest common path between project and solution, then remove it from project file path
                string[] solutionPath = buildData.BuildConfiguration.SolutionPath.Split(Path.DirectorySeparatorChar);
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
