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
        public List<Entry> ChildEntries { get; private set; }

        // child events (including those grouped within the child entries, but not the start/end from this entry), if any
        public List<Event> ChildEvents { get; private set; }

        public Entry()
        {
            ChildEntries = new List<Entry>();
            ChildEvents = new List<Event>();
        }

        public void AddChild(Entry entry)
        {
            ChildEntries.Add(entry);
            entry.Parent = this;
        }

        public void AddChild(Event e)
        {
            ChildEvents.Add(e);
        }

        public bool OverlapsWith(Entry entry)
        {
            return StartEvent.Timestamp < entry.EndEvent.Timestamp &&
                   entry.StartEvent.Timestamp < EndEvent.Timestamp;
        }
    }
}
