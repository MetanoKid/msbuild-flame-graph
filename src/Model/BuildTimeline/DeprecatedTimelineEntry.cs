using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;

namespace Model.BuildTimeline
{
    public class DeprecatedTimelineEntry
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

        public DeprecatedTimelineEntry Parent
        {
            get;
            private set;
        }

        public List<DeprecatedTimelineEntry> Children
        {
            get;
            private set;
        }

        public List<BuildMessageEventArgs> Messages
        {
            get;
            private set;
        }

        public DeprecatedTimelineEntry()
        {
            ThreadAffinity = new ThreadAffinity();
            Children = new List<DeprecatedTimelineEntry>();
            Messages = new List<BuildMessageEventArgs>();
        }

        public void AddChild(DeprecatedTimelineEntry entry)
        {
            Children.Add(entry);
            entry.Parent = this;
        }

        public bool OverlapsWith(DeprecatedTimelineEntry entry)
        {
            return StartBuildEvent.Timestamp < entry.EndBuildEvent.Timestamp &&
                   entry.StartBuildEvent.Timestamp < EndBuildEvent.Timestamp;
        }
    }
}
