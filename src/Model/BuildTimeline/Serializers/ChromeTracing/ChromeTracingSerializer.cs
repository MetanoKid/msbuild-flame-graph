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
using System.Reflection;
using Model.BuildTimeline;

namespace Model
{
    public class ChromeTracingSerializer
    {
        // extracted from Chrome Tracing GitHub repo (catapult-project, ColorScheme)
        private static readonly string s_BuildSucceededColor = "good";
        private static readonly string s_BuildFailedColor = "terrible";

        public static ChromeTrace BuildTrace(Timeline timeline)
        {
            Debug.Assert(timeline.PerNodeRootEntries.Length > 0);
            Debug.Assert(timeline.PerNodeRootEntries[0].Count == 1);
            DateTime buildStartTimestamp = timeline.PerNodeRootEntries[0][0].StartTimestamp;
            ChromeTrace trace = new ChromeTrace();
            
            // dump per process metadata
            ExtractProcessNamesIntoTrace(timeline, trace.traceEvents);

            // dump per thread metadata
            ExtractThreadNamesIntoTrace(timeline, trace.traceEvents);

            // dump projects, targets and tasks as extracted from the timeline
            foreach(var perNodeRootEntries in timeline.PerNodeRootEntries)
            {
                foreach(var rootEntry in perNodeRootEntries)
                {
                    ExtractEventsIntoTrace(rootEntry, buildStartTimestamp, trace.traceEvents);
                }
            }

            return trace;
        }

        private static void ExtractEventsIntoTrace(TimelineEntry timelineEntry, DateTime buildStartTimestamp, List<ChromeTracingEvent> events)
        {
            Debug.Assert(timelineEntry.ThreadAffinity.Calculated);

            TimelineBuildEntry timelineBuildEntry = timelineEntry as TimelineBuildEntry;

            // skip instantaneous entries
            if(timelineEntry.ElapsedTime == TimeSpan.Zero)
            {
                return;
            }

            Dictionary<string, string> args = new Dictionary<string, string>();

            // Guid
            args.Add("GUID", timelineEntry.GUID.ToString());
            if(timelineEntry.Parent != null)
            {
                args.Add("Parent GUID", timelineEntry.Parent.GUID.ToString());
            }

            // start
            if(timelineBuildEntry != null)
            {
                args.Add("Start event", timelineBuildEntry.BuildEntry.StartEvent.Message);
            }

            events.Add(new ChromeTracingEvent()
            {
                ph = 'B',
                pid = timelineEntry.NodeId,
                tid = timelineEntry.ThreadAffinity.ThreadId,
                ts = (timelineEntry.StartTimestamp - buildStartTimestamp).TotalMilliseconds * 1000.0d,
                name = timelineEntry.Name,
            });

            // child events
            foreach(TimelineEntry child in timelineEntry.ChildEntries)
            {
                ExtractEventsIntoTrace(child, buildStartTimestamp, events);
            }

            if(timelineBuildEntry != null)
            {
                // messages within this entry
                List<Event> messageEvents = timelineBuildEntry.BuildEntry.ChildEvents.Where(_ => _.GetType() == typeof(MessageEvent)).ToList();
                for(int i = 0; i < messageEvents.Count(); ++i)
                {
                    double millisecondsSinceStart = (messageEvents[i].Timestamp - buildStartTimestamp).TotalMilliseconds;
                    args.Add($"Message #{i}", $"[{millisecondsSinceStart:0.###} ms] {messageEvents[i].Message}");
                }

                // warnings within this entry
                List<Event> warningEvents = timelineBuildEntry.BuildEntry.ChildEvents.Where(_ => _.GetType() == typeof(WarningEvent)).ToList();
                for(int i = 0; i < warningEvents.Count(); ++i)
                {
                    double millisecondsSinceStart = (warningEvents[i].Timestamp - buildStartTimestamp).TotalMilliseconds;
                    args.Add($"Warning #{i}", $"[{millisecondsSinceStart:0.###} ms] {warningEvents[i].Message}");
                }

                // errors within this entry
                List<Event> errorEvents = timelineBuildEntry.BuildEntry.ChildEvents.Where(_ => _.GetType() == typeof(ErrorEvent)).ToList();
                for (int i = 0; i < errorEvents.Count(); ++i)
                {
                    double millisecondsSinceStart = (errorEvents[i].Timestamp - buildStartTimestamp).TotalMilliseconds;
                    args.Add($"Error #{i}", $"[{millisecondsSinceStart:0.###} ms] {errorEvents[i].Message}");
                }
            }

            // end
            if(timelineBuildEntry != null)
            {
                args.Add("End event", timelineBuildEntry.BuildEntry.EndEvent.Message);
            }

            events.Add(new ChromeTracingEvent()
            {
                ph = 'E',
                pid = timelineEntry.NodeId,
                tid = timelineEntry.ThreadAffinity.ThreadId,
                ts = (timelineEntry.EndTimestamp - buildStartTimestamp).TotalMilliseconds * 1000.0d,
                name = timelineEntry.Name,
                args = args,
                cname = ExtractEventColor(timelineBuildEntry, timelineEntry)
            });
        }

        private static string ExtractEventColor(TimelineBuildEntry timelineBuildEntry, TimelineEntry timelineEntry)
        {
            if(timelineBuildEntry != null)
            {
                BuildFinishedEvent endEvent = timelineBuildEntry.BuildEntry.EndEvent as BuildFinishedEvent;
                if (endEvent != null)
                {
                    return endEvent.Succeeded ? s_BuildSucceededColor : s_BuildFailedColor;
                }
            }

            return null;
        }

        private static void ExtractProcessNamesIntoTrace(Timeline timeline, List<ChromeTracingEvent> events)
        {
            // node 0 is special: not tied to a node but the build itself
            events.Add(new ChromeTracingEvent()
            {
                ph = 'M',
                name = "process_name",
                pid = 0,
                args = new Dictionary<string, string> {
                    { "name", "Build" }
                },
            });

            // other nodes are as reported by MSBuild
            for(int i = 1; i < timeline.PerNodeRootEntries.Length; ++i)
            {
                events.Add(new ChromeTracingEvent()
                {
                    ph = 'M',
                    name = "process_name",
                    pid = i,
                    args = new Dictionary<string, string> {
                        { "name", $"Node {i}" }
                    },
                });
            }
        }

        private static void ExtractThreadNamesIntoTrace(Timeline timeline, List<ChromeTracingEvent> events)
        {
            // Tuple<NodeId, ProcessID, ThreadId>
            HashSet<Tuple<int, int, int>> registeredTIDs = new HashSet<Tuple<int, int, int>>();
            
            foreach(List<TimelineEntry> rootEntries in timeline.PerNodeRootEntries)
            {
                foreach (TimelineEntry rootEntry in rootEntries)
                {
                    ExtractRegisteredTIDsFromEntry(rootEntry, registeredTIDs);
                }
            }

            foreach (Tuple<int, int, int> tidTuple in registeredTIDs)
            {
                events.Add(new ChromeTracingEvent()
                {
                    ph = 'M',
                    name = "thread_name",
                    pid = tidTuple.Item1,
                    tid = tidTuple.Item2,
                    args = new Dictionary<string, string> {
                        { "name", $"Thread {tidTuple.Item3}" }
                    },
                });

                // because we've added a new name it will be sorted by name
                // we want to go back to sort them by offsetted tid
                events.Add(new ChromeTracingEvent()
                {
                    ph = 'M',
                    name = "thread_sort_index",
                    pid = tidTuple.Item1,
                    tid = tidTuple.Item2,
                    args = new Dictionary<string, string> {
                        { "sort_index", tidTuple.Item2.ToString() }
                    }
                });
            }
        }

        private static void ExtractRegisteredTIDsFromEntry(TimelineEntry entry, HashSet<Tuple<int, int, int>> registeredTIDs)
        {
            registeredTIDs.Add(new Tuple<int, int, int>(
                entry.NodeId,
                entry.ThreadAffinity.ThreadId,
                entry.ThreadAffinity.ThreadId
            ));

            foreach (TimelineEntry child in entry.ChildEntries)
            {
                ExtractRegisteredTIDsFromEntry(child, registeredTIDs);
            }
        }
    }
}
