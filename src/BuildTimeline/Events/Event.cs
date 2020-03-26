using System;

namespace BuildTimeline
{
    public class Event
    {
        // context this event was created in
        public EventContext Context { get; set; }

        // information from this event
        public string Message { get; set; }

        // instant in time where this event was created
        public DateTime Timestamp { get; set; }
    }
}
