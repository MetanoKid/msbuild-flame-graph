using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.BuildTimeline
{
    public class TimelineEntryPostProcessor
    {
        public delegate void Processor(TimelineEntry entry);

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

            // TODO: access the ChildEvents (filter only MessageEvent ones) and start processing them
        }
    }
}
