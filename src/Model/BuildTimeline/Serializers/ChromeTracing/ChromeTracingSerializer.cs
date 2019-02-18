using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Diagnostics;

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
            string name = null;

            if (entryEvent is BuildStartedEventArgs)
            {
                name = "Build";
            }
            else if (entryEvent is ProjectStartedEventArgs)
            {
                name = (entryEvent as ProjectStartedEventArgs).ProjectFile;
            }
            else if (entryEvent is TargetStartedEventArgs)
            {
                name = (entryEvent as TargetStartedEventArgs).TargetName;
            }
            else if (entryEvent is TaskStartedEventArgs)
            {
                name = (entryEvent as TaskStartedEventArgs).TaskName;
            }

            Debug.Assert(name != null);

            return name;
        }
    }
}
