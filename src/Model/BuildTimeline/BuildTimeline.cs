using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Model
{
    public class BuildTimeline
    {
        public DateTime StartTimestamp
        {
            get;
            private set;
        }

        public DateTime EndTimestamp
        {
            get;
            private set;
        }

        public void ProcessBuildStartEvent(BuildStartedEventArgs e)
        {
            StartTimestamp = e.Timestamp;
        }

        public void ProcessBuildEndEvent(BuildFinishedEventArgs e)
        {
            EndTimestamp = e.Timestamp;
        }

        public void ProcessProjectStartEvent(ProjectStartedEventArgs e)
        {

        }

        public void ProcessProjectEndEvent(ProjectFinishedEventArgs e)
        {

        }

        public void ProcessTargetStartEvent(TargetStartedEventArgs e)
        {

        }

        public void ProcessTargetEndEvent(TargetFinishedEventArgs e)
        {

        }

        public void ProcessTaskStartEvent(TaskStartedEventArgs e)
        {

        }

        public void ProcessTaskEndEvent(TaskFinishedEventArgs e)
        {

        }
    }
}
