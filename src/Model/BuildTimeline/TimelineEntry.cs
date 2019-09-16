using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.BuildTimeline
{
    public class TimelineEntry
    {
        public BuildEventArgs StartBuildEvent
        {
            get;
            set;
        }

        public BuildEventArgs EndBuildEvent
        {
            get;
            set;
        }

        public TimeSpan ElapsedTime
        {
            get
            {
                return EndBuildEvent.Timestamp - StartBuildEvent.Timestamp;
            }
        }

        public ThreadAffinity ThreadAffinity
        {
            get;
            set;
        }

        public TimelineEntry Parent
        {
            get;
            private set;
        }

        public List<TimelineEntry> Children
        {
            get;
            private set;
        }

        public List<BuildMessageEventArgs> Messages
        {
            get;
            private set;
        }

        public TimelineEntry()
        {
            ThreadAffinity = new ThreadAffinity();
            Children = new List<TimelineEntry>();
            Messages = new List<BuildMessageEventArgs>();
        }

        public void AddChild(TimelineEntry entry)
        {
            Children.Add(entry);
            entry.Parent = this;
        }

        public bool OverlapsWith(TimelineEntry entry)
        {
            return StartBuildEvent.Timestamp < entry.EndBuildEvent.Timestamp &&
                   entry.StartBuildEvent.Timestamp < EndBuildEvent.Timestamp;
        }
    }
}
