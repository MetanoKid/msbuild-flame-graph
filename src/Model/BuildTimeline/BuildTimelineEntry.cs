using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    public class BuildTimelineEntry
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

        public BuildTimelineEntry Parent
        {
            get;
            private set;
        }

        public List<BuildTimelineEntry> Children
        {
            get;
            private set;
        }

        public List<BuildMessageEventArgs> Messages
        {
            get;
            private set;
        }

        public BuildTimelineEntry()
        {
            ThreadAffinity = new ThreadAffinity();
            Children = new List<BuildTimelineEntry>();
            Messages = new List<BuildMessageEventArgs>();
        }

        public void AddChild(BuildTimelineEntry entry)
        {
            Children.Add(entry);
            entry.Parent = this;
        }

        public bool OverlapsWith(BuildTimelineEntry entry)
        {
            return StartBuildEvent.Timestamp < entry.EndBuildEvent.Timestamp &&
                   entry.StartBuildEvent.Timestamp < EndBuildEvent.Timestamp;
        }
    }
}
