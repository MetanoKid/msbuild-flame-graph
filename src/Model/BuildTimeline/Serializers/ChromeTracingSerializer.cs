using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    public class ChromeTracingSerializer
    {
        public static void Serialize(BuildTimeline timeline)
        {
            using (FileStream outFile = new FileStream("BuildTimeline.json", FileMode.Create))
            {
                using (StreamWriter writer = new StreamWriter(outFile))
                {
                    SerializeEntry(timeline.RootTimelineEntry, 0, timeline.RootTimelineEntry.StartBuildEvent.Timestamp, writer);
                }
            }
        }

        private static void SerializeEntry(BuildTimelineEntry entry, int indentationLevel, DateTime buildStartTimestamp, StreamWriter writer)
        {
            string indentation = new string('\t', 1);
            writer.WriteLine(indentation + "{");
            writer.WriteLine(indentation + @"""pid"": 1,");
            writer.WriteLine(indentation + @"""tid"": 1,");
            writer.WriteLine(indentation + $@"""ts"": {(entry.StartBuildEvent.Timestamp - buildStartTimestamp).TotalMilliseconds.ToString(CultureInfo.InvariantCulture)},");
            writer.WriteLine(indentation + $@"""dur"": {entry.ElapsedTime.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)},");
            writer.WriteLine(indentation + @"""ph"": ""X"",");

            // per-type messages
            if (entry.StartBuildEvent is BuildStartedEventArgs)
            {
                BuildStartedEventArgs buildStarted = entry.StartBuildEvent as BuildStartedEventArgs;
                writer.WriteLine(indentation + @"""name"": ""Build started""");
            }
            else if (entry.StartBuildEvent is ProjectStartedEventArgs)
            {
                ProjectStartedEventArgs projectStarted = entry.StartBuildEvent as ProjectStartedEventArgs;
                writer.WriteLine(indentation + $@"""name"": ""Project {projectStarted.ProjectFile.Replace('\\', '/')}""");
            }
            else if (entry.StartBuildEvent is TargetStartedEventArgs)
            {
                TargetStartedEventArgs targetStarted = entry.StartBuildEvent as TargetStartedEventArgs;
                writer.WriteLine(indentation + $@"""name"": ""Target {targetStarted.TargetName}""");
            }
            else if (entry.StartBuildEvent is TaskStartedEventArgs)
            {
                TaskStartedEventArgs taskStarted = entry.StartBuildEvent as TaskStartedEventArgs;
                writer.WriteLine(indentation + $@"""name"": ""Task {taskStarted.TaskName}""");
            }
            writer.WriteLine(indentation + "},");

            // process children
            foreach (BuildTimelineEntry e in entry.Children)
            {
                SerializeEntry(e, indentationLevel + 1, buildStartTimestamp, writer);
            }

            /*if (entry.StartBuildEvent is BuildStartedEventArgs)
            {
                BuildStartedEventArgs buildStarted = entry.StartBuildEvent as BuildStartedEventArgs;
                writer.WriteLine(indentation + $"Build finished (elapsed {entry.ElapsedTime})");
            }
            else if (entry.StartBuildEvent is ProjectStartedEventArgs)
            {
                ProjectStartedEventArgs projectStarted = entry.StartBuildEvent as ProjectStartedEventArgs;
                writer.WriteLine(indentation + $"Project {projectStarted.ProjectFile} elapsed {entry.ElapsedTime}");
            }
            else if (entry.StartBuildEvent is TargetStartedEventArgs)
            {
                TargetStartedEventArgs targetStarted = entry.StartBuildEvent as TargetStartedEventArgs;
                writer.WriteLine(indentation + $"Target {targetStarted.TargetName} elapsed {entry.ElapsedTime}");
            }
            else if (entry.StartBuildEvent is TaskStartedEventArgs)
            {
                TaskStartedEventArgs taskStarted = entry.StartBuildEvent as TaskStartedEventArgs;
                writer.WriteLine(indentation + $"Task {taskStarted.TaskName} elapsed {entry.ElapsedTime}");
            }*/

        }
    }
}
