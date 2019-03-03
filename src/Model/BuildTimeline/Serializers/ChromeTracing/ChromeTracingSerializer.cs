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
            Debug.Assert(timeline.PerNodeRootEntries.Count > 0 && timeline.PerNodeRootEntries[0].Count == 1);
            DateTime buildStartTimestamp = timeline.PerNodeRootEntries[0][0].StartBuildEvent.Timestamp;
            ChromeTrace trace = new ChromeTrace();
            
            foreach(var perNodeRootEntries in timeline.PerNodeRootEntries)
            {
                foreach(var rootEntry in perNodeRootEntries)
                {
                    ExtractEventsIntoTrace(rootEntry, buildStartTimestamp, trace.traceEvents);
                }
            }

            return trace;
        }

        private static void ExtractEventsIntoTrace(BuildTimelineEntry entry, DateTime startTimestamp, List<ChromeTracingEvent> events)
        {
            if(entry.ElapsedTime == TimeSpan.Zero)
            {
                return;
            }

            // start event
            ChromeTracingEvent startEvent = new ChromeTracingEvent()
            {
                ph = 'B',
                pid = entry.StartBuildEvent.BuildEventContext != null ? entry.StartBuildEvent.BuildEventContext.NodeId : 0,
                tid = entry.ThreadAffinity.ThreadId,
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
                pid = entry.EndBuildEvent.BuildEventContext != null ? entry.EndBuildEvent.BuildEventContext.NodeId : 0,
                tid = entry.ThreadAffinity.ThreadId,
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
