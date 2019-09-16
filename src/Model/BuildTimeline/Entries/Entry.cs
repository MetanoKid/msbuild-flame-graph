using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Model.BuildTimeline
{
    class Entry
    {
        // the event that started this entry
        public Event StartEvent { get; set; }

        // the event that ended this entry
        public Event EndEvent { get; set; }

        // time elapsed between start and end events
        public TimeSpan ElapsedTime
        {
            get
            {
                Debug.Assert(StartEvent != null && EndEvent != null);
                return EndEvent.Timestamp - StartEvent.Timestamp;
            }
        }

        // parent entry, if any
        public Entry Parent { get; set; }

        // child entries, if any
        public List<Entry> Children { get; private set; }

        public Entry()
        {
            Children = new List<Entry>();
        }

        public void AddChild(Entry entry)
        {
            Children.Add(entry);
            entry.Parent = this;
        }

        public bool OverlapsWith(Entry entry)
        {
            return StartEvent.Timestamp < entry.EndEvent.Timestamp &&
                   entry.StartEvent.Timestamp < EndEvent.Timestamp;
        }
    }
}
