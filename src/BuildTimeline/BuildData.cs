using System.Collections.Generic;
using Model;

namespace BuildTimeline
{
    public class BuildData
    {
        public BuildConfiguration BuildConfiguration { get; set; }
        public List<Event> Events { get; set; }
    }
}
