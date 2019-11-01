using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.BuildTimeline
{
    public class Event
    {
        // context this event was created in
        public EventContext Context { get; set; }

        // information from this event
        public string Message { get; set; }

        // instant in time where this event was created
        public DateTime Timestamp { get; set; }

        // id of the thread this event was created in (as reported by MSBuild)
        public int ThreadId { get; set; }
    }
}
