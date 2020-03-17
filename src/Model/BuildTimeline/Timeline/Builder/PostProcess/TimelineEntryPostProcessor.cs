using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

        private static readonly Regex s_LinkPass1 = new Regex(@"^Linker:(\s\(.+\))? Pass 1.+time = (.+)s .+$");
        private static readonly Regex s_LinkPass2 = new Regex(@"^Linker:(\s\(.+\))? Pass 2.+time = (.+)s .+$");
        private static readonly Regex s_LinkCommon = new Regex(@"^Linker:(\s\(.+\))? ([^:]+).* Total time = (.+)s .+$");

        private static readonly string s_TaskCLSingleThreadFrontEndFinishedMessage = "Generating Code...";
        private static readonly string s_TaskCLSingleThreadFrontEndStartedMessage = "Compiling...";

        private static readonly string s_FrontendDefaultName = "frontend";
        private static readonly string s_BackendDefaultName = "backend";

        private static readonly string s_LinkerPass1Name = "Pass 1";
        private static readonly string s_LinkerPass2Name = "Pass 2";

        private static readonly int s_CompilationThreadAffinityOffsetFromParent = 100;
        private static readonly int s_CompilationThreadAffinityIncrement = 1;

        private static bool IsTaskCL(TimelineEntry entry)
        {
            // only take TimelineBuildEntry into account
            TimelineBuildEntry timelineBuildEntry = entry as TimelineBuildEntry;
            if (timelineBuildEntry == null)
            {
                return false;
            }

            // we're looking for Task ones
            TaskStartedEvent taskStartedEvent = timelineBuildEntry.BuildEntry.StartEvent as TaskStartedEvent;
            if (taskStartedEvent == null)
            {
                return false;
            }

            // and only the ones called "CL"
            if (taskStartedEvent.TaskName != "CL")
            {
                return false;
            }

            return true;
        }

        private static bool HasTaskCLSingleThreadMessage(TimelineBuildEntry timelineBuildEntry)
        {
            return timelineBuildEntry.BuildEntry.ChildEvents.Where(_ => _ is MessageEvent)
                                                            .Select(_ => _ as MessageEvent)
                                                            .Any(_ => _.Message == s_TaskCLSingleThreadFrontEndFinishedMessage);
        }

        public static void TaskCLSingleThread(TimelineEntry entry)
        {
            // only CL tasks
            if(!IsTaskCL(entry))
            {
                return;
            }

            TimelineBuildEntry timelineBuildEntry = entry as TimelineBuildEntry;

            // single threaded compilations contain a message that multithreaded ones don't
            if(!HasTaskCLSingleThreadMessage(timelineBuildEntry))
            {
                return;
            }

            // because it's single threaded, it will first execute the compiler frontend for all
            // files, then the compiler backend for all files, sequentially
            // so, we have to parse messages and expect frontend ones first, then the special message
            // saying that phase is over, then the backend ones (and maybe cycle to a frontend phase)
            List<TimelineEntry> compilationEntries = new List<TimelineEntry>();
            IEnumerable<MessageEvent> messages = timelineBuildEntry.BuildEntry.ChildEvents.Where(_ => _ is MessageEvent)
                                                                                          .Select(_ => _ as MessageEvent);

            bool isInFrontendPhase = true;
            foreach(MessageEvent message in messages)
            {
                if(isInFrontendPhase)
                {
                    Debug.Assert(!s_CompileBackEndFinish.IsMatch(message.Message));

                    // file compilation started
                    Match matchFileStarted = s_CompileFileStart.Match(message.Message);
                    if(matchFileStarted.Success)
                    {
                        TimelineEntry compilationEntry = new TimelineEntry(matchFileStarted.Value, message.Context.NodeId, message.Timestamp, message.Timestamp);
                        compilationEntries.Add(compilationEntry);

                        compilationEntry.ThreadAffinity.SetParameters(compilationEntry.ThreadAffinity.ThreadId, s_CompilationThreadAffinityOffsetFromParent, s_CompilationThreadAffinityIncrement);

                        // add a front-end entry
                        TimelineEntry frontend = new TimelineEntry(s_FrontendDefaultName, message.Context.NodeId, message.Timestamp, message.Timestamp);
                        compilationEntry.AddChild(frontend);

                        continue;
                    }

                    // front-end compilation finished
                    Match matchFrontendFinished = s_CompileFrontEndFinish.Match(message.Message);
                    if (matchFrontendFinished.Success)
                    {
                        TimelineEntry compilationEntry = compilationEntries.Find(_ => _.Name == matchFrontendFinished.Groups[2].Value.Split('\\').Last());
                        Debug.Assert(compilationEntry != null);

                        // edit front-end entry
                        TimelineEntry frontend = compilationEntry.ChildEntries.Find(_ => _.Name == s_FrontendDefaultName);
                        Debug.Assert(frontend != null);

                        frontend.Name = matchFrontendFinished.Groups[1].Value;
                        frontend.SetEndTimestamp(message.Timestamp);
                        compilationEntry.SetEndTimestamp(message.Timestamp);

                        continue;
                    }

                    if (message.Message == s_TaskCLSingleThreadFrontEndFinishedMessage)
                    {
                        isInFrontendPhase = false;
                    }
                }
                else
                {
                    Debug.Assert(!s_CompileFrontEndFinish.IsMatch(message.Message));

                    // back-end compilation finished
                    Match matchBackendFinished = s_CompileBackEndFinish.Match(message.Message);
                    if (matchBackendFinished.Success)
                    {
                        DateTime entryBackendCompilationStartTimestamp = compilationEntries.Last().EndTimestamp;

                        TimelineEntry compilationEntry = new TimelineEntry(matchBackendFinished.Groups[2].Value.Split('\\').Last(), message.Context.NodeId, entryBackendCompilationStartTimestamp, message.Timestamp);
                        compilationEntries.Add(compilationEntry);

                        compilationEntry.ThreadAffinity.SetParameters(compilationEntry.ThreadAffinity.ThreadId, s_CompilationThreadAffinityOffsetFromParent, s_CompilationThreadAffinityIncrement);

                        // add a back-end entry
                        TimelineEntry backend = new TimelineEntry(matchBackendFinished.Groups[1].Value, message.Context.NodeId, entryBackendCompilationStartTimestamp, message.Timestamp);
                        compilationEntry.AddChild(backend);

                        continue;
                    }

                    if (message.Message == s_TaskCLSingleThreadFrontEndStartedMessage)
                    {
                        isInFrontendPhase = true;
                    }
                }
            }

            // add them all to the CL task
            compilationEntries.ForEach(timelineBuildEntry.AddChild);
        }

        public static void TaskCLMultiThread(TimelineEntry entry)
        {
            if(!IsTaskCL(entry))
            {
                return;
            }

            TimelineBuildEntry timelineBuildEntry = entry as TimelineBuildEntry;

            // single threaded compilations contain a message that multithreaded ones don't
            if(HasTaskCLSingleThreadMessage(timelineBuildEntry))
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

                    compilationEntry.ThreadAffinity.SetParameters(compilationEntry.ThreadAffinity.ThreadId, s_CompilationThreadAffinityOffsetFromParent, s_CompilationThreadAffinityIncrement);

                    // add a front-end entry
                    TimelineEntry frontend = new TimelineEntry(s_FrontendDefaultName, message.Context.NodeId, message.Timestamp, message.Timestamp);
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
                    TimelineEntry frontend = compilationEntry.ChildEntries.Find(_ => _.Name == s_FrontendDefaultName);
                    Debug.Assert(frontend != null);

                    frontend.Name = matchFrontendFinished.Groups[1].Value;
                    frontend.SetEndTimestamp(message.Timestamp);

                    // just in case the front-end fails, update entry's timestamp
                    compilationEntry.SetEndTimestamp(message.Timestamp);

                    // add a back-end entry
                    TimelineEntry backend = new TimelineEntry(s_BackendDefaultName, message.Context.NodeId, message.Timestamp, message.Timestamp);
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
                    TimelineEntry backend = compilationEntry.ChildEntries.Find(_ => _.Name == s_BackendDefaultName);
                    Debug.Assert(backend != null);

                    backend.Name = matchBackendFinished.Groups[1].Value;
                    backend.SetEndTimestamp(message.Timestamp);

                    compilationEntry.SetEndTimestamp(message.Timestamp);

                    continue;
                }
            }

            // add them all to the CL task
            compilationEntries.ForEach(timelineBuildEntry.AddChild);
        }

        private static bool IsTaskLink(TimelineEntry entry)
        {
            // only take TimelineBuildEntry into account
            TimelineBuildEntry timelineBuildEntry = entry as TimelineBuildEntry;
            if (timelineBuildEntry == null)
            {
                return false;
            }

            // we're looking for Task ones
            TaskStartedEvent taskStartedEvent = timelineBuildEntry.BuildEntry.StartEvent as TaskStartedEvent;
            if (taskStartedEvent == null)
            {
                return false;
            }

            // and only the ones called "Link"
            if (taskStartedEvent.TaskName != "Link")
            {
                return false;
            }

            return true;
        }

        public static void TaskLink(TimelineEntry entry)
        {
            if(!IsTaskLink(entry))
            {
                return;
            }

            TimelineBuildEntry timelineBuildEntry = entry as TimelineBuildEntry;

            // because these messages aren't related to each other in any way other than parsing messages,
            // we'll be keeping a list of timeline entries which will open/close as the messages are processed
            List<TimelineEntry> linkEntries = new List<TimelineEntry>();
            IEnumerable<MessageEvent> messages = timelineBuildEntry.BuildEntry.ChildEvents.Where(_ => _ is MessageEvent)
                                                                                          .Select(_ => _ as MessageEvent);

            // find Pass 1 and Pass 2
            foreach (MessageEvent message in messages)
            {
                // Linker: Pass 1
                Match matchLinkerPass1 = s_LinkPass1.Match(message.Message);
                if(matchLinkerPass1.Success)
                {
                    double elapsedTimeFromMessage = Double.Parse(matchLinkerPass1.Groups[2].Value, CultureInfo.InvariantCulture);
                    TimeSpan elapsedTime = TimeSpan.FromSeconds(elapsedTimeFromMessage);
                    DateTime startTimestamp = message.Timestamp - elapsedTime;
                    if(startTimestamp < timelineBuildEntry.StartTimestamp)
                    {
                        startTimestamp = timelineBuildEntry.StartTimestamp;
                    }

                    TimelineEntry pass1Entry = new TimelineEntry(s_LinkerPass1Name, message.Context.NodeId, startTimestamp, message.Timestamp);
                    linkEntries.Add(pass1Entry);

                    pass1Entry.ThreadAffinity.SetParameters(pass1Entry.ThreadAffinity.ThreadId, s_CompilationThreadAffinityOffsetFromParent, s_CompilationThreadAffinityIncrement);

                    continue;
                }

                // Linker: Pass 2
                Match matchLinkerPass2 = s_LinkPass2.Match(message.Message);
                if(matchLinkerPass2.Success)
                {
                    double elapsedTimeFromMessage = Double.Parse(matchLinkerPass2.Groups[2].Value, CultureInfo.InvariantCulture);
                    TimeSpan elapsedTime = TimeSpan.FromSeconds(elapsedTimeFromMessage);
                    DateTime startTimestamp = message.Timestamp - elapsedTime;
                    if(linkEntries.Count > 0 && startTimestamp < linkEntries.Last().EndTimestamp)
                    {
                        startTimestamp = linkEntries.Last().EndTimestamp;
                    }

                    TimelineEntry pass2Entry = new TimelineEntry(s_LinkerPass2Name, message.Context.NodeId, startTimestamp, message.Timestamp);
                    linkEntries.Add(pass2Entry);

                    pass2Entry.ThreadAffinity.SetParameters(pass2Entry.ThreadAffinity.ThreadId, s_CompilationThreadAffinityOffsetFromParent, s_CompilationThreadAffinityIncrement);

                    continue;
                }
            }

            // this is the function that will be applied to all common linker messages except Pass 1 and Pass 2 messages
            Action<TimelineEntry, Tuple<MessageEvent, Match>> perCommonLinkerMessage = (parent, tuple) =>
            {
                MessageEvent message = tuple.Item1;
                Match match = tuple.Item2;

                double elapsedTimeFromMessage = Double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
                TimeSpan elapsedTime = TimeSpan.FromSeconds(elapsedTimeFromMessage);
                DateTime startTimestamp = message.Timestamp - elapsedTime;

                if (parent.ChildEntries.Count == 0 && startTimestamp < parent.StartTimestamp)
                {
                    startTimestamp = parent.StartTimestamp;
                }
                else if (parent.ChildEntries.Count > 0 && startTimestamp < parent.ChildEntries.Last().EndTimestamp)
                {
                    startTimestamp = parent.ChildEntries.Last().EndTimestamp;
                }

                TimelineEntry linkerEntry = new TimelineEntry(match.Groups[2].Value.Trim(), message.Context.NodeId, startTimestamp, message.Timestamp);
                parent.AddChild(linkerEntry);

                linkerEntry.ThreadAffinity.SetParameters(linkerEntry.ThreadAffinity.ThreadId, s_CompilationThreadAffinityOffsetFromParent, s_CompilationThreadAffinityIncrement);
            };

            // add entries within Pass 1
            TimelineEntry parentEntry = linkEntries.Find(_ => _.Name == s_LinkerPass1Name);
            if(parentEntry != null)
            {
                IEnumerable<Tuple<MessageEvent, Match>> messagesBeforePass1 = messages.TakeWhile(_ => !s_LinkPass1.Match(_.Message).Success)                         // take messages until Pass 1 message
                                                                                      .Select(_ => new Tuple<MessageEvent, Match>(_, s_LinkCommon.Match(_.Message))) // match with common regex
                                                                                      .Where(_ => _.Item2.Success);                                                  // only take positive ones
                foreach (Tuple<MessageEvent, Match> tuple in messagesBeforePass1)
                {
                    perCommonLinkerMessage(parentEntry, tuple);
                }
            }

            // add entries within Pass 2
            parentEntry = linkEntries.Find(_ => _.Name == s_LinkerPass2Name);
            if(parentEntry != null)
            {
                IEnumerable<Tuple<MessageEvent, Match>> messagesBeforePass2 = messages.SkipWhile(_ => !s_LinkPass1.Match(_.Message).Success)                         // skip all messages until Pass 1
                                                                                      .Skip(1)                                                                       // skip Pass 1 included
                                                                                      .TakeWhile(_ => !s_LinkPass2.Match(_.Message).Success)                         // take messages until Pass 2 message
                                                                                      .Select(_ => new Tuple<MessageEvent, Match>(_, s_LinkCommon.Match(_.Message))) // match with common regex
                                                                                      .Where(_ => _.Item2.Success);                                                  // only take positive ones
                foreach (Tuple<MessageEvent, Match> tuple in messagesBeforePass2)
                {
                    perCommonLinkerMessage(parentEntry, tuple);
                }
            }

            // add them all to the Link task
            linkEntries.ForEach(timelineBuildEntry.AddChild);
        }
    }
}
