using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Model.BuildTimeline
{
    public class BuildEntry
    {
        // the event that started this entry
        public Event StartEvent { get; }

        // the event that ended this entry
        public Event EndEvent { get; private set; }

        // the context for both start and end events
        public EventContext Context
        {
            get
            {
                return StartEvent?.Context;
            }
        }

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
        public BuildEntry Parent { get; private set; }

        // child entries, if any
        public List<BuildEntry> ChildEntries { get; private set; }

        // child events (including those grouped within the child entries, but not the start/end from this entry), if any
        public List<Event> ChildEvents { get; private set; }

        public BuildEntry(Event startEvent)
        {
            ChildEntries = new List<BuildEntry>();
            ChildEvents = new List<Event>();

            StartEvent = startEvent;
        }

        public void CloseWith(Event endEvent)
        {
            EndEvent = endEvent;
        }

        public void AddChild(BuildEntry entry)
        {
            ChildEntries.Add(entry);
            entry.Parent = this;
        }

        public void AddChild(Event e)
        {
            ChildEvents.Add(e);
        }
    }
}
