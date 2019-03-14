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
            
            foreach(var perNodeRootEntries in timeline.PerNodeRootEntries)
            {
                foreach(var rootEntry in perNodeRootEntries)
                {
                    ExtractEventsIntoTrace(rootEntry, buildStartTimestamp, trace.traceEvents);
                }
            }

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
            ChromeTracingEvent startEvent = new ChromeTracingEvent()
            {
                ph = 'B',
                pid = entry.StartBuildEvent.BuildEventContext != null ? entry.StartBuildEvent.BuildEventContext.NodeId : 0,
                tid = entry.ThreadAffinity.ThreadId * s_ParallelProjectThreadOffset,
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
                tid = entry.ThreadAffinity.ThreadId * s_ParallelProjectThreadOffset,
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

        private static void ExtractParallelFileCompilationIntoTrace(ParallelFileCompilation parallelFileCompilation, DateTime startTimestamp, List<ChromeTracingEvent> events)
        {
            Debug.Assert(parallelFileCompilation.Parent.StartBuildEvent.BuildEventContext != null);
            Debug.Assert(parallelFileCompilation.Parent.StartBuildEvent.BuildEventContext.NodeId != BuildEventContext.InvalidNodeId);

            foreach (var fileCompilation in parallelFileCompilation.Compilations)
            {
                int pid = parallelFileCompilation.Parent.StartBuildEvent.BuildEventContext.NodeId;
                int tid = parallelFileCompilation.Parent.ThreadAffinity.ThreadId * s_ParallelProjectThreadOffset +
                          fileCompilation.ThreadId + 1;

                // start event
                ChromeTracingEvent startEvent = new ChromeTracingEvent()
                {
                    ph = 'B',
                    pid = pid,
                    tid = tid,
                    ts = (fileCompilation.StartTimestamp - startTimestamp).TotalMilliseconds * 1000.0,
                    name = fileCompilation.FileName,
                };
                events.Add(startEvent);

                // frontend compilation
                ChromeTracingEvent frontendEvent = new ChromeTracingEvent()
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
                };
                events.Add(frontendEvent);

                // backend compilation
                ChromeTracingEvent backendEvent = new ChromeTracingEvent()
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
                };
                events.Add(backendEvent);

                // end event
                ChromeTracingEvent endEvent = new ChromeTracingEvent()
                {
                    ph = 'E',
                    pid = pid,
                    tid = tid,
                    ts = (fileCompilation.EndTimestamp - startTimestamp).TotalMilliseconds * 1000.0,
                    name = fileCompilation.FileName,
                };
                events.Add(endEvent);
            }
        }
    }
}
