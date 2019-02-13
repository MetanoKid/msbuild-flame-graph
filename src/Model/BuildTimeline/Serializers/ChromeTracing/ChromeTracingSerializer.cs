using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Model
{
    public class ChromeTracingSerializer
    {
        public static void Serialize(BuildTimeline timeline)
        {
            // build timeline
            ChromeTrace trace = BuildTrace(timeline);

            // serialize to JSON
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(trace);

            // write to file
            File.WriteAllText("Build timeline.json", json);
        }

        private static ChromeTrace BuildTrace(BuildTimeline timeline)
        {
            ChromeTrace trace = new ChromeTrace();

            ExtractEventsIntoTrace(timeline.RootTimelineEntry, timeline.RootTimelineEntry.StartBuildEvent.Timestamp, trace.traceEvents);

            return trace;
        }

        private static void ExtractEventsIntoTrace(BuildTimelineEntry entry, DateTime startTimestamp, List<ChromeTracingEvent> events)
        {
            // start event
            ChromeTracingEvent startEvent = new ChromeTracingEvent()
            {
                ph = 'B',
                tid = entry.StartBuildEvent.BuildEventContext != null ? entry.StartBuildEvent.BuildEventContext.NodeId : 0,
                ts = (entry.StartBuildEvent.Timestamp - startTimestamp).TotalMilliseconds * 1000.0,
                name = ExtractTracingNameFrom(entry.StartBuildEvent),
            };

            events.Add(startEvent);

            // child events
            foreach(BuildTimelineEntry child in entry.Children)
            {
                ExtractEventsIntoTrace(child, startTimestamp, events);
            }

            // end event
            ChromeTracingEvent endEvent = new ChromeTracingEvent()
            {
                ph = 'E',
                tid = entry.EndBuildEvent.BuildEventContext != null ? entry.EndBuildEvent.BuildEventContext.NodeId : 0,
                ts = (entry.EndBuildEvent.Timestamp - startTimestamp).TotalMilliseconds * 1000.0,
                name = ExtractTracingNameFrom(entry.StartBuildEvent),
            };
            events.Add(endEvent);
        }

        private static string ExtractTracingNameFrom(BuildEventArgs entryEvent)
        {
            string name = "???";

            if (entryEvent is BuildStartedEventArgs)
            {
                BuildStartedEventArgs buildStarted = entryEvent as BuildStartedEventArgs;
                name = "Build";
            }
            else if (entryEvent is ProjectStartedEventArgs)
            {
                ProjectStartedEventArgs projectStarted = entryEvent as ProjectStartedEventArgs;
                name = projectStarted.ProjectFile;
            }
            else if (entryEvent is TargetStartedEventArgs)
            {
                TargetStartedEventArgs targetStarted = entryEvent as TargetStartedEventArgs;
                name = targetStarted.TargetName;
            }
            else if (entryEvent is TaskStartedEventArgs)
            {
                TaskStartedEventArgs taskStarted = entryEvent as TaskStartedEventArgs;
                name = taskStarted.TaskName;
            }

            return name;
        }
    }
}
