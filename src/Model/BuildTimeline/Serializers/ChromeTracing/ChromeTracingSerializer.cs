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
        private static int s_ParallelProjectThreadOffset = 100;

        public static ChromeTrace BuildTrace(Timeline timeline)
        {
            Debug.Assert(timeline.PerNodeRootEntries.Length > 0);
            Debug.Assert(timeline.PerNodeRootEntries[0].Count == 1);
            DateTime buildStartTimestamp = timeline.PerNodeRootEntries[0][0].BuildEntry.StartEvent.Timestamp;
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

            // dump source file compilations
            /*foreach(var parallelFileCompilation in timeline.ParallelFileCompilations)
            {
                ExtractParallelFileCompilationIntoTrace(parallelFileCompilation, buildStartTimestamp, trace.traceEvents);
            }*/

            return trace;
        }

        private static void ExtractEventsIntoTrace(TimelineEntry timelineEntry, DateTime startTimestamp, List<ChromeTracingEvent> events)
        {
            BuildEntry entry = timelineEntry.BuildEntry;

            if(entry.ElapsedTime == TimeSpan.Zero)
            {
                return;
            }

            Dictionary<string, string> args = new Dictionary<string, string>();

            // start event
            args.Add("Start event", entry.StartEvent.Message);
            events.Add(new ChromeTracingEvent()
            {
                ph = 'B',
                pid = entry.StartEvent.Context != null ? entry.StartEvent.Context.NodeId : 0,
                tid = timelineEntry.ThreadAffinity.ThreadId * s_ParallelProjectThreadOffset,
                ts = (entry.StartEvent.Timestamp - startTimestamp).TotalMilliseconds * 1000.0,
                name = ExtractTracingNameFrom(entry.StartEvent),
            });

            // child events
            foreach(TimelineEntry child in timelineEntry.ChildEntries)
            {
                ExtractEventsIntoTrace(child, startTimestamp, events);
            }

            // messages during this entry
            List<Event> messageEvents = timelineEntry.BuildEntry.ChildEvents.Where(_ => _ is MessageEvent).ToList();
            for(int i = 0; i < messageEvents.Count(); ++i)
            {
                double millisecondsSinceStart = (messageEvents[i].Timestamp - startTimestamp).TotalMilliseconds;
                args.Add($"Message #{i}", $"[{millisecondsSinceStart:0.###} ms] {messageEvents[i].Message}");
            }

            // end event
            args.Add("End event", entry.EndEvent.Message);
            events.Add(new ChromeTracingEvent()
            {
                ph = 'E',
                pid = entry.EndEvent.Context != null ? entry.EndEvent.Context.NodeId : 0,
                tid = timelineEntry.ThreadAffinity.ThreadId * s_ParallelProjectThreadOffset,
                ts = (entry.EndEvent.Timestamp - startTimestamp).TotalMilliseconds * 1000.0,
                name = ExtractTracingNameFrom(entry.StartEvent),
                args = args,
            });
        }

        private static string ExtractTracingNameFrom(Event e)
        {
            string name = null;

            if (e is BuildStartedEvent)
            {
                // TODO: display build file, requested configuration, platform and target?
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

        /*private static void ExtractParallelFileCompilationIntoTrace(ParallelFileCompilation parallelFileCompilation, DateTime startTimestamp, List<ChromeTracingEvent> events)
        {
            Debug.Assert(parallelFileCompilation.Parent.StartBuildEvent.BuildEventContext != null);
            Debug.Assert(parallelFileCompilation.Parent.StartBuildEvent.BuildEventContext.NodeId != Microsoft.Build.Framework.BuildEventContext.InvalidNodeId);

            HashSet<Tuple<int, int>> registeredTIDs = new HashSet<Tuple<int, int>>();

            int pid = parallelFileCompilation.Parent.StartBuildEvent.BuildEventContext.NodeId;
            foreach (var fileCompilation in parallelFileCompilation.Compilations)
            {
                int tid = parallelFileCompilation.Parent.ThreadAffinity.ThreadId * s_ParallelProjectThreadOffset +
                          fileCompilation.ThreadId + 1;
                registeredTIDs.Add(new Tuple<int, int>(tid, fileCompilation.ThreadId));

                // start event
                events.Add(new ChromeTracingEvent()
                {
                    ph = 'B',
                    pid = pid,
                    tid = tid,
                    ts = (fileCompilation.StartTimestamp - startTimestamp).TotalMilliseconds * 1000.0,
                    name = fileCompilation.FileName,
                });

                // frontend compilation
                events.Add(new ChromeTracingEvent()
                {
                    ph = 'X',
                    pid = pid,
                    tid = tid,
                    ts = (fileCompilation.StartTimestamp - startTimestamp).TotalMilliseconds * 1000.0,
                    dur = (fileCompilation.FrontEndFinishTime - fileCompilation.StartTimestamp).TotalMilliseconds * 1000.0f,
                    name = fileCompilation.FrontEndDLL,
                    args = new Dictionary<string, string> {
                        { "/Bt+", fileCompilation.FrontEndFinishMessage }
                    },
                    cname = s_CompilerFrontendColor
                });

                // backend compilation
                events.Add(new ChromeTracingEvent()
                {
                    ph = 'X',
                    pid = pid,
                    tid = tid,
                    ts = (fileCompilation.FrontEndFinishTime - startTimestamp).TotalMilliseconds * 1000.0,
                    dur = (fileCompilation.BackEndFinishTime - fileCompilation.FrontEndFinishTime).TotalMilliseconds * 1000.0f,
                    name = "c2.dll",
                    args = new Dictionary<string, string> {
                        { "/Bt+", fileCompilation.BackEndFinishMessage }
                    },
                    cname = s_CompilerBackendColor
                });

                // end event
                events.Add(new ChromeTracingEvent()
                {
                    ph = 'E',
                    pid = pid,
                    tid = tid,
                    ts = (fileCompilation.EndTimestamp - startTimestamp).TotalMilliseconds * 1000.0,
                    name = fileCompilation.FileName,
                });
            }

            // TODO: move these to its own method
            // name of these pid+tid (tuple contains <offsetted tid, real tid>
            foreach(Tuple<int, int> tidPair in registeredTIDs)
            {
                events.Add(new ChromeTracingEvent()
                {
                    ph = 'M',
                    name = "thread_name",
                    pid = pid,
                    tid = tidPair.Item1,
                    args = new Dictionary<string, string> {
                        { "name", $"Thread {parallelFileCompilation.Parent.ThreadAffinity.ThreadId}, CL {tidPair.Item2}" }
                    },
                });

                // because we've added a new name it will be sorted by name
                // we want to go back to sort them by offsetted tid
                events.Add(new ChromeTracingEvent()
                {
                    ph = 'M',
                    name = "thread_sort_index",
                    pid = pid,
                    tid = tidPair.Item1,
                    args = new Dictionary<string, string> {
                        { "sort_index", tidPair.Item1.ToString() }
                    }
                });
            }
        }*/

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
                entry.BuildEntry.Context != null ? entry.BuildEntry.Context.NodeId : 0,
                entry.ThreadAffinity.ThreadId * s_ParallelProjectThreadOffset,
                entry.ThreadAffinity.ThreadId
            ));

            foreach (TimelineEntry child in entry.ChildEntries)
            {
                ExtractRegisteredTIDsFromEntry(child, registeredTIDs);
            }
        }
    }
}
