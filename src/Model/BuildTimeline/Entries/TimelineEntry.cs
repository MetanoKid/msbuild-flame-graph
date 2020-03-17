using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Model.BuildTimeline
{
    public class TimelineEntry
    {
        public DateTime StartTimestamp { get; }
        public DateTime EndTimestamp { get; private set; }

        public TimeSpan ElapsedTime
        {
            get
            {
                return EndTimestamp - StartTimestamp;
            }
        }

        public Guid GUID { get; }
        public string Name { get; set; }
        public TimelineEntry Parent { get; private set; }
        public List<TimelineEntry> ChildEntries { get; }
        public ThreadAffinity ThreadAffinity { get; }
        public int NodeId { get; }

        public TimelineEntry(string name, int nodeId, DateTime startTimestamp, DateTime endTimestamp)
        {
            GUID = Guid.NewGuid();
            Name = name;
            Parent = null;
            ChildEntries = new List<TimelineEntry>();
            ThreadAffinity = new ThreadAffinity();
            NodeId = nodeId;
            StartTimestamp = startTimestamp;
            EndTimestamp = endTimestamp;
        }

        public void AddChild(TimelineEntry entry)
        {
            ChildEntries.Add(entry);
            entry.Parent = this;
        }

        public bool OverlapsWith(TimelineEntry other)
        {
            return NodeId == other.NodeId &&
                   StartTimestamp < other.EndTimestamp &&
                   other.StartTimestamp < EndTimestamp;
        }

        public bool IsAncestorOf(TimelineEntry other)
        {
            if(other == null)
            {
                return false;
            }

            if(this == other.Parent)
            {
                return true;
            }

            return IsAncestorOf(other.Parent);
        }

        public void SetEndTimestamp(DateTime timestamp)
        {
            Debug.Assert(StartTimestamp <= timestamp);
            EndTimestamp = timestamp;
        }
    }
}
