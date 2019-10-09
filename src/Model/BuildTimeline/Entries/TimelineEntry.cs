using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.BuildTimeline
{
    public class TimelineEntry
    {
        public TimelineEntry Parent { get; private set; }
        public BuildEntry BuildEntry { get; private set; }
        public List<TimelineEntry> ChildEntries { get; private set; }
        public ThreadAffinity ThreadAffinity { get; private set; }

        public Guid GUID { get; }

        public TimelineEntry(BuildEntry buildEntry)
        {
            BuildEntry = buildEntry;
            Parent = null;
            ChildEntries = new List<TimelineEntry>();
            ThreadAffinity = new ThreadAffinity();

            GUID = Guid.NewGuid();
        }

        public void AddChild(TimelineEntry entry)
        {
            ChildEntries.Add(entry);
            entry.Parent = this;
        }

        public bool OverlapsWith(TimelineEntry other)
        {
            return BuildEntry.StartEvent.Timestamp < other.BuildEntry.EndEvent.Timestamp &&
                   other.BuildEntry.StartEvent.Timestamp < BuildEntry.EndEvent.Timestamp;
        }
    }
}
