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
        public double ts = 0.0;
        public double dur = 0.0;
        public string name;
        public Dictionary<string, string> args = null;
        public string cat;
    }
}
