using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace BuildTimeline
{

    public class TimelineEntryPostProcessor
    {
        public delegate void Processor(TimelineEntry entry);

        private static readonly Regex s_CompileFileStart = new Regex(@"^[^\s]+\.(cpp|cc|c)$");
        private static readonly Regex s_CompileFrontEndFinish = new Regex(@"^time\(.+(c1\.dll|c1xx\.dll)\)=(.+)s.+\[(.+)\]$");
        private static readonly Regex s_CompileBackEndFinish = new Regex(@"^time\(.+(c2.dll)\)=(.+)s.+\[(.+)\]$");

        private static readonly Regex s_LinkPass1 = new Regex(@"^Linker:(\s\(.+\))? Pass 1.+time = (.+)s .+$");
        private static readonly Regex s_LinkPass2 = new Regex(@"^Linker:(\s\(.+\))? Pass 2.+time = (.+)s .+$");
        private static readonly Regex s_LinkCommon = new Regex(@"^Linker:(\s\(.+\))? ([^:]+).* Total time = (.+)s .+$");

        private static readonly string s_TaskCLSingleThreadFrontEndFinishedMessage = "Generating Code...";
        private static readonly string s_TaskCLSingleThreadFrontEndStartedMessage = "Compiling...";

        private static readonly string s_LinkerPass1Name = "Pass 1";
        private static readonly string s_LinkerPass2Name = "Pass 2";

        private static readonly Regex s_D1ReportTimeSectionHeader = new Regex(@"^(Include Headers):$");
        private static readonly Regex s_D1ReportTimeSectionClassDefinition = new Regex(@"^(Class Definitions):$");
        private static readonly Regex s_D1ReportTimeSectionFunctionDefintition = new Regex(@"^(Function Definitions):$");
        private static readonly Regex s_D1ReportTimeSectionCount = new Regex(@"^\s+Count: (\d+)$");
        private static readonly Regex s_D1ReportTimeSectionTotal = new Regex(@"^\s+Total: (.+)s$");
        private static readonly Regex s_D1ReportTimeEntry = new Regex(@"^(\t+)(.+): (.+)s$");
        private static readonly Regex s_D1ReportTimeBlockEnd = new Regex(@"^$");

        private static int s_D1ReportTimeEntryBaseIndentation = 2;

        enum D1ReportTimeSection
        {
            None,
            Headers,
            ClassDefinitions,
            FunctionDefinitions
        };

        private static TimeSpan TimeSpanFromSeconds(double seconds)
        {
            double milliseconds = seconds * 1000.0;
            double microseconds = milliseconds * 1000.0;
            double ticks = microseconds * 10.0;    // 100 nanoseconds
            return TimeSpan.FromTicks((long) ticks);
        }

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

                        compilationEntry.ThreadAffinity.SetParameters(compilationEntry.ThreadAffinity.ThreadId, ThreadAffinity.s_OffsetFromParentPostProcessedEntries, ThreadAffinity.s_OffsetFromParentPostProcessedEntriesIncrement);

                        continue;
                    }

                    // front-end compilation finished
                    Match matchFrontendFinished = s_CompileFrontEndFinish.Match(message.Message);
                    if (matchFrontendFinished.Success)
                    {
                        TimelineEntry compilationEntry = compilationEntries.Find(_ => _.Name == matchFrontendFinished.Groups[3].Value.Split('\\').Last());
                        if(compilationEntry != null)
                        {
                            double elapsedTimeFromMessage = Double.Parse(matchFrontendFinished.Groups[2].Value, CultureInfo.InvariantCulture);
                            TimeSpan elapsedTime = TimeSpanFromSeconds(elapsedTimeFromMessage);
                            DateTime startTimestamp = message.Timestamp - elapsedTime;
                            if (startTimestamp < compilationEntry.StartTimestamp)
                            {
                                startTimestamp = compilationEntry.StartTimestamp;
                            }

                            // add a front-end entry
                            TimelineEntry frontend = new TimelineEntry(matchFrontendFinished.Groups[1].Value, message.Context.NodeId, startTimestamp, message.Timestamp);
                            compilationEntry.AddChild(frontend);
                            compilationEntry.SetEndTimestamp(message.Timestamp);
                        }

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
                        Debug.Assert(compilationEntries.Count > 0);

                        // create new entry as parent of the back-end entry
                        TimelineEntry compilationEntry = new TimelineEntry(matchBackendFinished.Groups[3].Value.Split('\\').Last(), message.Context.NodeId, compilationEntries.Last().EndTimestamp, message.Timestamp);
                        compilationEntries.Add(compilationEntry);

                        compilationEntry.ThreadAffinity.SetParameters(compilationEntry.ThreadAffinity.ThreadId, ThreadAffinity.s_OffsetFromParentPostProcessedEntries, ThreadAffinity.s_OffsetFromParentPostProcessedEntriesIncrement);

                        // add a back-end entry
                        double elapsedTimeFromMessage = Double.Parse(matchBackendFinished.Groups[2].Value, CultureInfo.InvariantCulture);
                        TimeSpan elapsedTime = TimeSpanFromSeconds(elapsedTimeFromMessage);
                        DateTime startTimestamp = message.Timestamp - elapsedTime;
                        if(startTimestamp < compilationEntry.StartTimestamp)
                        {
                            startTimestamp = compilationEntry.StartTimestamp;
                        }

                        TimelineEntry backend = new TimelineEntry(matchBackendFinished.Groups[1].Value, message.Context.NodeId, startTimestamp, message.Timestamp);
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
                    // with /Wall flag enabled some warnings will be mixed up with this kind of message
                    // meaning we may not match the regex and won't add the entry

                    TimelineEntry compilationEntry = new TimelineEntry(matchFileStarted.Value, message.Context.NodeId, message.Timestamp, message.Timestamp);
                    compilationEntries.Add(compilationEntry);

                    compilationEntry.ThreadAffinity.SetParameters(compilationEntry.ThreadAffinity.ThreadId, ThreadAffinity.s_OffsetFromParentPostProcessedEntries, ThreadAffinity.s_OffsetFromParentPostProcessedEntriesIncrement);

                    continue;
                }

                // front-end compilation finished
                Match matchFrontendFinished = s_CompileFrontEndFinish.Match(message.Message);
                if(matchFrontendFinished.Success)
                {
                    // with /Wall flag enabled some warnings will be mixed up with this kind of message
                    // meaning we may not match the regex and won't add the entry
                    // we may not even find the parent compilation entry

                    TimelineEntry compilationEntry = compilationEntries.Find(_ => _.Name == matchFrontendFinished.Groups[3].Value.Split('\\').Last());
                    if(compilationEntry != null)
                    {
                        double elapsedTimeFromMessage = Double.Parse(matchFrontendFinished.Groups[2].Value, CultureInfo.InvariantCulture);
                        TimeSpan elapsedTime = TimeSpanFromSeconds(elapsedTimeFromMessage);
                        DateTime startTimestamp = message.Timestamp - elapsedTime;
                        if (startTimestamp < compilationEntry.StartTimestamp)
                        {
                            startTimestamp = compilationEntry.StartTimestamp;
                        }

                        // add a front-end entry
                        TimelineEntry frontend = new TimelineEntry(matchFrontendFinished.Groups[1].Value, message.Context.NodeId, startTimestamp, message.Timestamp);
                        compilationEntry.AddChild(frontend);

                        // just in case the back-end fails, update entry's timestamp
                        compilationEntry.SetEndTimestamp(message.Timestamp);
                    }

                    continue;
                }

                // back-end compilation finished
                Match matchBackendFinished = s_CompileBackEndFinish.Match(message.Message);
                if(matchBackendFinished.Success)
                {
                    // with /Wall flag enabled some warnings will be mixed up with this kind of message
                    // meaning we may not match the regex and won't add the entry
                    // we may not even find the parent compilation entry nor the frontend sibling

                    TimelineEntry compilationEntry = compilationEntries.Find(_ => _.Name == matchBackendFinished.Groups[3].Value.Split('\\').Last());
                    if(compilationEntry != null)
                    {
                        TimelineEntry frontendEntry = compilationEntry.ChildEntries.FirstOrDefault();

                        double elapsedTimeFromMessage = Double.Parse(matchBackendFinished.Groups[2].Value, CultureInfo.InvariantCulture);
                        TimeSpan elapsedTime = TimeSpanFromSeconds(elapsedTimeFromMessage);
                        DateTime startTimestamp = message.Timestamp - elapsedTime;

                        if(frontendEntry != null)
                        {
                            if(startTimestamp < frontendEntry.EndTimestamp)
                            {
                                startTimestamp = frontendEntry.EndTimestamp;
                            }
                        }
                        else if(startTimestamp < compilationEntry.StartTimestamp)
                        {
                            startTimestamp = compilationEntry.StartTimestamp;
                        }

                        // add a back-end entry
                        TimelineEntry backend = new TimelineEntry(matchBackendFinished.Groups[1].Value, message.Context.NodeId, startTimestamp, message.Timestamp);
                        compilationEntry.AddChild(backend);

                        // complete compilation entry
                        compilationEntry.SetEndTimestamp(message.Timestamp);
                    }
                    
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
                    TimeSpan elapsedTime = TimeSpanFromSeconds(elapsedTimeFromMessage);
                    DateTime startTimestamp = message.Timestamp - elapsedTime;
                    if(startTimestamp < timelineBuildEntry.StartTimestamp)
                    {
                        startTimestamp = timelineBuildEntry.StartTimestamp;
                    }

                    TimelineEntry pass1Entry = new TimelineEntry(s_LinkerPass1Name, message.Context.NodeId, startTimestamp, message.Timestamp);
                    linkEntries.Add(pass1Entry);

                    pass1Entry.ThreadAffinity.SetParameters(pass1Entry.ThreadAffinity.ThreadId, ThreadAffinity.s_OffsetFromParentPostProcessedEntries, ThreadAffinity.s_OffsetFromParentPostProcessedEntriesIncrement);

                    continue;
                }

                // Linker: Pass 2
                Match matchLinkerPass2 = s_LinkPass2.Match(message.Message);
                if(matchLinkerPass2.Success)
                {
                    double elapsedTimeFromMessage = Double.Parse(matchLinkerPass2.Groups[2].Value, CultureInfo.InvariantCulture);
                    TimeSpan elapsedTime = TimeSpanFromSeconds(elapsedTimeFromMessage);
                    DateTime startTimestamp = message.Timestamp - elapsedTime;
                    if(linkEntries.Count > 0 && startTimestamp < linkEntries.Last().EndTimestamp)
                    {
                        startTimestamp = linkEntries.Last().EndTimestamp;
                    }

                    TimelineEntry pass2Entry = new TimelineEntry(s_LinkerPass2Name, message.Context.NodeId, startTimestamp, message.Timestamp);
                    linkEntries.Add(pass2Entry);

                    pass2Entry.ThreadAffinity.SetParameters(pass2Entry.ThreadAffinity.ThreadId, ThreadAffinity.s_OffsetFromParentPostProcessedEntries, ThreadAffinity.s_OffsetFromParentPostProcessedEntriesIncrement);

                    continue;
                }
            }

            // this is the function that will be applied to all common linker messages except Pass 1 and Pass 2 messages
            Action<TimelineEntry, Tuple<MessageEvent, Match>> perCommonLinkerMessage = (parent, tuple) =>
            {
                MessageEvent message = tuple.Item1;
                Match match = tuple.Item2;

                double elapsedTimeFromMessage = Double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
                TimeSpan elapsedTime = TimeSpanFromSeconds(elapsedTimeFromMessage);
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

        public static void FlagD1ReportTime(TimelineEntry entry)
        {
            if(!IsTaskCL(entry))
            {
                return;
            }

            TimelineBuildEntry timelineBuildEntry = entry as TimelineBuildEntry;

            // flag /d1reportTime operates within the front-end and has three sections
            //   - Headers: time spent including headers
            //   - Class Definitions: time spent defining classes
            //   - Function Definitions: time spent defining functions
            // each section ends with a blank line and a Total
            // because of how MSBuild reports messages, we require build to compile each
            // file in a single-threaded fashion or we'd get data mixed up

            IEnumerable<MessageEvent> messages = timelineBuildEntry.BuildEntry.ChildEvents.Where(_ => _ is MessageEvent)
                                                                                          .Select(_ => _ as MessageEvent);

            // kind of a simplified state machine
            TimelineEntry currentFrontendEntry = null;
            TimelineEntry currentSubEntry = null;
            D1ReportTimeSection currentSection = D1ReportTimeSection.None;
            int currentIndentationLevel = 0;
            bool processEntries = false;

            foreach (MessageEvent message in messages)
            {
                // file compilation started
                Match matchFileStarted = s_CompileFileStart.Match(message.Message);
                if (matchFileStarted.Success)
                {
                    currentFrontendEntry = timelineBuildEntry.ChildEntries.Find(_ => _.Name == matchFileStarted.Value);

                    Debug.Assert(currentFrontendEntry.ChildEntries.Count > 0, "Processing /d1reportTime without an existing front-end child");

                    // first entry is the front-end one, second one (if any) is the back-end one
                    currentSubEntry = currentFrontendEntry.ChildEntries.First();
                    continue;
                }

                // processing /d1reportTime
                if (currentFrontendEntry != null)
                {
                    switch(currentSection)
                    {
                        case D1ReportTimeSection.None:
                            {
                                // look for a new section
                                Match matchSectionHeaders = s_D1ReportTimeSectionHeader.Match(message.Message);
                                if(matchSectionHeaders.Success)
                                {
                                    currentSection = D1ReportTimeSection.Headers;

                                    // first message starts with two tabs
                                    currentIndentationLevel = s_D1ReportTimeEntryBaseIndentation;

                                    TimelineEntry headersEntry = new TimelineEntry(matchSectionHeaders.Groups[1].Value, message.Context.NodeId, currentSubEntry.StartTimestamp, message.Timestamp);
                                    currentSubEntry.AddChild(headersEntry);
                                    currentSubEntry = headersEntry;

                                    processEntries = true;

                                    continue;
                                }

                                Match matchSectionClasses = s_D1ReportTimeSectionClassDefinition.Match(message.Message);
                                if(matchSectionClasses.Success)
                                {
                                    currentSection = D1ReportTimeSection.ClassDefinitions;

                                    // first message starts with two tabs
                                    currentIndentationLevel = s_D1ReportTimeEntryBaseIndentation;

                                    TimelineEntry classesEntry = new TimelineEntry(matchSectionClasses.Groups[1].Value, message.Context.NodeId, currentSubEntry.ChildEntries.Last().EndTimestamp, message.Timestamp);
                                    currentSubEntry.AddChild(classesEntry);
                                    currentSubEntry = classesEntry;

                                    processEntries = true;

                                    continue;
                                }

                                Match matchSectionFunctions = s_D1ReportTimeSectionFunctionDefintition.Match(message.Message);
                                if(matchSectionFunctions.Success)
                                {
                                    currentSection = D1ReportTimeSection.FunctionDefinitions;

                                    // first message starts with two tabs
                                    currentIndentationLevel = s_D1ReportTimeEntryBaseIndentation;

                                    TimelineEntry classesEntry = new TimelineEntry(matchSectionFunctions.Groups[1].Value, message.Context.NodeId, currentSubEntry.ChildEntries.Last().EndTimestamp, message.Timestamp);
                                    currentSubEntry.AddChild(classesEntry);
                                    currentSubEntry = classesEntry;

                                    processEntries = true;

                                    continue;
                                }
                            }
                            break;

                        case D1ReportTimeSection.Headers:
                        case D1ReportTimeSection.ClassDefinitions:
                        case D1ReportTimeSection.FunctionDefinitions:
                            {
                                // get total time spent in this section
                                Match matchTotalTime = s_D1ReportTimeSectionTotal.Match(message.Message);
                                if (matchTotalTime.Success)
                                {
                                    double elapsedTimeFromMessage = Double.Parse(matchTotalTime.Groups[1].Value, CultureInfo.InvariantCulture);
                                    TimeSpan elapsedTime = TimeSpanFromSeconds(elapsedTimeFromMessage);
                                    DateTime endTimestamp = currentSubEntry.StartTimestamp + elapsedTime;

                                    currentSubEntry.SetEndTimestamp(endTimestamp);

                                    currentSection = D1ReportTimeSection.None;
                                    currentSubEntry = currentSubEntry.Parent;
                                    continue;
                                }

                                // block completed?
                                Match matchBlockCompleted = s_D1ReportTimeBlockEnd.Match(message.Message);
                                if(matchBlockCompleted.Success)
                                {
                                    if(processEntries)
                                    {
                                        // pop entries until we get to the top level
                                        for(int i = currentIndentationLevel; i > s_D1ReportTimeEntryBaseIndentation; --i)
                                        {
                                            currentSubEntry = currentSubEntry.Parent;
                                        }

                                        processEntries = false;
                                    }
                                    continue;
                                }

                                if(processEntries)
                                {
                                    // match with an element
                                    Match matchElement = s_D1ReportTimeEntry.Match(message.Message);
                                    if(matchElement.Success)
                                    {
                                        // \t is a single character
                                        int indentationLevel = matchElement.Groups[1].Value.Length;

                                        // go to a parent, if needed
                                        if(indentationLevel < currentIndentationLevel)
                                        {
                                            for(int i = 0; i < currentIndentationLevel - indentationLevel; ++i)
                                            {
                                                currentSubEntry = currentSubEntry.Parent;
                                            }
                                        }
                                        // move to a child, if needed
                                        else if(indentationLevel > currentIndentationLevel)
                                        {
                                            currentSubEntry = currentSubEntry.ChildEntries.Last();
                                        }

                                        // find out the start timestamp
                                        DateTime startTimestamp = currentSubEntry.StartTimestamp;
                                        if(currentSubEntry.ChildEntries.Count > 0)
                                        {
                                            startTimestamp = currentSubEntry.ChildEntries.Last().EndTimestamp;
                                        }

                                        // find out the end timestamp
                                        double elapsedTimeFromMessage = Double.Parse(matchElement.Groups[3].Value, CultureInfo.InvariantCulture);
                                        TimeSpan elapsedTime = TimeSpanFromSeconds(elapsedTimeFromMessage);
                                        DateTime endTimestamp = startTimestamp + elapsedTime;

                                        // build element
                                        TimelineEntry elementEntry = new TimelineEntry(matchElement.Groups[2].Value, message.Context.NodeId, startTimestamp, endTimestamp);
                                        currentSubEntry.AddChild(elementEntry);

                                        currentIndentationLevel = indentationLevel;

                                        continue;
                                    }
                                }
                            }
                            break;
                    }
                }

                // front-end finished
                Match matchFrontendFinished = s_CompileFrontEndFinish.Match(message.Message);
                if (matchFrontendFinished.Success)
                {
                    currentFrontendEntry = null;
                    currentSubEntry = null;
                    currentSection = D1ReportTimeSection.None;
                    continue;
                }
            }
        }
    }
}
