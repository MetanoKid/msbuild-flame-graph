using System;
using System.Collections.Generic;

namespace Model
{
    class ChromeTrace
    {
        public List<ChromeTracingEvent> traceEvents = new List<ChromeTracingEvent>();
    }

    class ChromeTracingEvent
    {
        public Nullable<char> ph;
        public Nullable<int> pid;
        public Nullable<int> tid;
        public Nullable<double> ts;
        public Nullable<double> dur;
        public string cname;
        public string name;
        public Dictionary<string, string> args;
    }
}
