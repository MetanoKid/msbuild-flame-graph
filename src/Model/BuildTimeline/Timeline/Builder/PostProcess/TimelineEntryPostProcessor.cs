using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Model.BuildTimeline
{

    public class TimelineEntryPostProcessor
    {
        public delegate void Processor(TimelineEntry entry);

        private static readonly Regex s_CompileFileStart = new Regex(@"^[^\s]+\.(cpp|cc|c)$");
        private static readonly Regex s_CompileFrontEndFinish = new Regex(@"^time\(.+(c1\.dll|c1xx\.dll)\).+\[(.+)\]$");
        private static readonly Regex s_CompileBackEndFinish = new Regex(@"^time\(.+(c2.dll)\).+\[(.+)\]$");

        private static readonly string s_frontendDefaultName = "frontend";
        private static readonly string s_backendDefaultName = "backend";

        public static void TaskCL(TimelineEntry entry)
        {
            // only take TimelineBuildEntry into account
            TimelineBuildEntry timelineBuildEntry = entry as TimelineBuildEntry;
            if(timelineBuildEntry == null)
            {
                return;
            }

            // we're looking for Task ones
            TaskStartedEvent taskStartedEvent = timelineBuildEntry.BuildEntry.StartEvent as TaskStartedEvent;
            if(taskStartedEvent == null)
            {
                return;
            }

            // and only the ones called "CL"
            if(taskStartedEvent.TaskName != "CL")
            {
                return;
            }

            // because these messages aren't related to each other in any way other than parsing messages,
            // we'll be keeping a list of timeline entries which will open/close as the messages are processed
            List<TimelineEntry> compilationEntries = new List<TimelineEntry>();

            IEnumerable<MessageEvent> messages = timelineBuildEntry.BuildEntry.ChildEvents.Where(_ => _ is MessageEvent)
                                                                                          .Select(_ => _ as MessageEvent);
            foreach(MessageEvent message in messages)
            {
                // file compilation started
                Match matchFileStarted = s_CompileFileStart.Match(message.Message);
                if(matchFileStarted.Success)
                {
                    TimelineEntry compilationEntry = new TimelineEntry(matchFileStarted.Value, message.Context.NodeId, message.Timestamp, message.Timestamp);
                    compilationEntries.Add(compilationEntry);

                    // add a front-end entry
                    TimelineEntry frontend = new TimelineEntry(s_frontendDefaultName, message.Context.NodeId, message.Timestamp, message.Timestamp);
                    compilationEntry.AddChild(frontend);

                    continue;
                }

                // front-end compilation finished
                Match matchFrontendFinished = s_CompileFrontEndFinish.Match(message.Message);
                if(matchFrontendFinished.Success)
                {
                    TimelineEntry compilationEntry = compilationEntries.Find(_ => _.Name == matchFrontendFinished.Groups[2].Value.Split('\\').Last());
                    Debug.Assert(compilationEntry != null);

                    // edit front-end entry
                    TimelineEntry frontend = compilationEntry.ChildEntries.Find(_ => _.Name == s_frontendDefaultName);
                    Debug.Assert(frontend != null);

                    frontend.Name = matchFrontendFinished.Groups[1].Value;
                    frontend.SetEndTimestamp(message.Timestamp);

                    // add a back-end entry
                    TimelineEntry backend = new TimelineEntry(s_backendDefaultName, message.Context.NodeId, message.Timestamp, message.Timestamp);
                    compilationEntry.AddChild(backend);

                    continue;
                }

                // back-end compilation finished
                Match matchBackendFinished = s_CompileBackEndFinish.Match(message.Message);
                if(matchBackendFinished.Success)
                {
                    TimelineEntry compilationEntry = compilationEntries.Find(_ => _.Name == matchBackendFinished.Groups[2].Value.Split('\\').Last());
                    Debug.Assert(compilationEntry != null);

                    // edit back-end entry
                    TimelineEntry backend = compilationEntry.ChildEntries.Find(_ => _.Name == s_backendDefaultName);
                    Debug.Assert(backend != null);

                    backend.Name = matchBackendFinished.Groups[1].Value;
                    backend.SetEndTimestamp(message.Timestamp);

                    compilationEntry.SetEndTimestamp(message.Timestamp);

                    continue;
                }
            }

            compilationEntries.ForEach(timelineBuildEntry.AddChild);
        }
    }
}
