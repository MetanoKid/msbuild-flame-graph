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

            //Dictionary<BuildTimelineEntry, int> calculatedThreadAffinity = CalculateThreadAffinity(timeline);

            foreach(var perNodeRootEntries in timeline.PerNodeRootEntries)
            {
                foreach(var rootEntry in perNodeRootEntries)
                {
                    ExtractEventsIntoTrace(rootEntry, buildStartTimestamp, trace.traceEvents);
                }
            }

            return trace;
        }

        // some project may execute in parallel, this method aims to 
        private static Dictionary<BuildTimelineEntry, int> CalculateThreadAffinity(BuildTimeline timeline)
        {
            Dictionary<BuildTimelineEntry, int> calculatedAffinity = new Dictionary<BuildTimelineEntry, int>();

            foreach(var perNodeRootEntries in timeline.PerNodeRootEntries)
            {
                foreach(var rootEntry in perNodeRootEntries)
                {
                    AssignThreadAffinityToHierarchy(rootEntry, calculatedAffinity);
                }
            }

            return calculatedAffinity;
        }

        private static void AssignThreadAffinityToHierarchy(BuildTimelineEntry entry, Dictionary<BuildTimelineEntry, int> affinities)
        {
            int threadAffinity = CalculateOverlappingSiblings(entry, affinities);
            affinities.Add(entry, threadAffinity);

            // TODO: all children must use this thread affinity!

            foreach(var child in entry.Children)
            {
                AssignThreadAffinityToHierarchy(child, affinities);
            }
        }

        private static int CalculateOverlappingSiblings(BuildTimelineEntry entry, Dictionary<BuildTimelineEntry, int> affinities)
        {
            if(entry.Parent == null)
            {
                return 0;
            }
            
            int overlapCount = 0;
            foreach(var sibling in entry.Parent.Children)
            {
                if(sibling != entry)
                {
                    // affinities.ContainsKey isn't enough, siblings may have an affinity set by their parent!
                    if(!DoEventsOverlap(entry, sibling) || !affinities.ContainsKey(sibling))
                    {
                        break;
                    }

                    ++overlapCount;
                }
            }

            return overlapCount;
        }

        private static bool DoEventsOverlap(BuildTimelineEntry e1, BuildTimelineEntry e2)
        {
            return e1.StartBuildEvent.Timestamp <= e2.EndBuildEvent.Timestamp &&
                   e2.StartBuildEvent.Timestamp <= e1.EndBuildEvent.Timestamp;
        }

        private static void ExtractEventsIntoTrace(BuildTimelineEntry entry, DateTime startTimestamp, List<ChromeTracingEvent> events)
        {
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
