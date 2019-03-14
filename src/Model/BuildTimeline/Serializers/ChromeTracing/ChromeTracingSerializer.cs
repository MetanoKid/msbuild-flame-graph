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

namespace Model
{
    public class IgnoreNullValuesConverter : JavaScriptConverter
    {
        public override IEnumerable<Type> SupportedTypes
        {
            get
            {
                return new Type[] { typeof(ChromeTrace), typeof(ChromeTracingEvent) };
            }
        }

        public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
        {
            Dictionary<string, object> jsonObject = new Dictionary<string, object>();
            foreach(var field in obj.GetType().GetFields())
            {
                var value = field.GetValue(obj);
                if(value != null)
                {
                    jsonObject.Add(field.Name, value);
                }
            }

            return jsonObject;
        }
    }

    public class ChromeTracingSerializer
    {
        private static int s_ParallelProjectThreadOffset = 100;

        public static string Serialize(BuildTimeline timeline)
        {
            // build timeline
            ChromeTrace trace = BuildTrace(timeline);

            // serialize to JSON
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            serializer.RegisterConverters(new JavaScriptConverter[] { new IgnoreNullValuesConverter() });
            return serializer.Serialize(trace);
        }

        private static ChromeTrace BuildTrace(BuildTimeline timeline)
        {
            Debug.Assert(timeline.PerNodeRootEntries.Count > 0 && timeline.PerNodeRootEntries[0].Count == 1);
            DateTime buildStartTimestamp = timeline.PerNodeRootEntries[0][0].StartBuildEvent.Timestamp;
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
            foreach(var parallelFileCompilation in timeline.ParallelFileCompilations)
            {
                ExtractParallelFileCompilationIntoTrace(parallelFileCompilation, buildStartTimestamp, trace.traceEvents);
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
            events.Add(new ChromeTracingEvent()
            {
                ph = 'B',
                pid = entry.StartBuildEvent.BuildEventContext != null ? entry.StartBuildEvent.BuildEventContext.NodeId : 0,
                tid = entry.ThreadAffinity.ThreadId * s_ParallelProjectThreadOffset,
                ts = (entry.StartBuildEvent.Timestamp - startTimestamp).TotalMilliseconds * 1000.0,
                name = ExtractTracingNameFrom(entry.StartBuildEvent),
            });

            // child events
            foreach(BuildTimelineEntry child in entry.Children)
            {
                ExtractEventsIntoTrace(child, startTimestamp, events);
            }

            // end event
            events.Add(new ChromeTracingEvent()
            {
                ph = 'E',
                pid = entry.EndBuildEvent.BuildEventContext != null ? entry.EndBuildEvent.BuildEventContext.NodeId : 0,
                tid = entry.ThreadAffinity.ThreadId * s_ParallelProjectThreadOffset,
                ts = (entry.EndBuildEvent.Timestamp - startTimestamp).TotalMilliseconds * 1000.0,
                name = ExtractTracingNameFrom(entry.StartBuildEvent),
            });
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

        private static void ExtractParallelFileCompilationIntoTrace(ParallelFileCompilation parallelFileCompilation, DateTime startTimestamp, List<ChromeTracingEvent> events)
        {
            Debug.Assert(parallelFileCompilation.Parent.StartBuildEvent.BuildEventContext != null);
            Debug.Assert(parallelFileCompilation.Parent.StartBuildEvent.BuildEventContext.NodeId != BuildEventContext.InvalidNodeId);

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
                    name = "c1xx.dll",
                    args = new Dictionary<string, string> {
                        { "/Bt+", fileCompilation.FrontEndFinishMessage }
                    },
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
        }

        private static void ExtractProcessNamesIntoTrace(BuildTimeline timeline, List<ChromeTracingEvent> events)
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
            for(int i = 1; i < timeline.PerNodeRootEntries.Count; ++i)
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

        private static void ExtractRegisteredTIDsFromEntry(BuildTimelineEntry entry, HashSet<Tuple<int, int, int>> registeredTIDs)
        {
            registeredTIDs.Add(new Tuple<int, int, int>(
                entry.StartBuildEvent.BuildEventContext != null ? entry.StartBuildEvent.BuildEventContext.NodeId : 0,
                entry.ThreadAffinity.ThreadId * s_ParallelProjectThreadOffset,
                entry.ThreadAffinity.ThreadId
            ));

            foreach(var child in entry.Children)
            {
                ExtractRegisteredTIDsFromEntry(child, registeredTIDs);
            }
        }

        private static void ExtractThreadNamesIntoTrace(BuildTimeline timeline, List<ChromeTracingEvent> events)
        {
            HashSet<Tuple<int, int, int>> registeredTIDs = new HashSet<Tuple<int, int, int>>();
            
            foreach(var perNodeRootEntries in timeline.PerNodeRootEntries)
            {
                foreach (var rootEntry in perNodeRootEntries)
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
    }
}
