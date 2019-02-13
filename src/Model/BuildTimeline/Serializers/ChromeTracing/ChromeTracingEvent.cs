using System.Collections.Generic;

namespace Model
{
    class ChromeTrace
    {
        public List<ChromeTracingEvent> traceEvents = new List<ChromeTracingEvent>();
    }

    class ChromeTracingEvent
    {
        public char ph;
        public int tid = 0;
        public int pid = 0;
        public double ts;
        public string name;
    }
}
