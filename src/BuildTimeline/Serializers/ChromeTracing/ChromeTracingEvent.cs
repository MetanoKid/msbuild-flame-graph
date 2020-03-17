using System;
using System.Collections.Generic;

namespace Model
{
    public class ChromeTrace
    {
        public List<ChromeTracingEvent> traceEvents = new List<ChromeTracingEvent>();
        public string displayTimeUnit = "ms";
    }

    public class ChromeTracingEvent
    {
        public Nullable<char> ph;
        public Nullable<int> pid;
        public Nullable<int> tid;
        public Nullable<double> ts;
        public Nullable<double> dur;
        public string cname;
        public string name;

        // TODO: substitute with JObject so we can add data freely?
        public Dictionary<string, string> args;
    }
}
