using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BuildTimeline
{
    public class TimelineEntry
    {
        public DateTime StartTimestamp { get; private set; }
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

        public void FitChildEntries()
        {
            if(ChildEntries.Count == 0)
            {
                return;
            }

            // we've defined fitting only when overflow occurs towards the end
            Debug.Assert(ChildEntries.First().StartTimestamp >= StartTimestamp);

            DateTime childrenLastEndTimestamp = ChildEntries.Last().EndTimestamp;
            DateTime maxEndTimestamp = childrenLastEndTimestamp > EndTimestamp ? childrenLastEndTimestamp : EndTimestamp;

            TimeSpan elapsedTimeWithOverflow = maxEndTimestamp - StartTimestamp;
            double scale = elapsedTimeWithOverflow.Ticks > 0 ? (double) ElapsedTime.Ticks / elapsedTimeWithOverflow.Ticks : 1.0;
            Debug.Assert(scale <= 1.0);

            if(scale < 1.0)
            {
                ScaleTimestamps(scale);
            }
            
            foreach(TimelineEntry child in ChildEntries)
            {
                child.FitChildEntries();
            }
        }

        private void ScaleTimestamps(double scale)
        {
            foreach (TimelineEntry child in ChildEntries)
            {
                // take the local offset from parent's start (we can't modify it in absolute values, but relative ones)
                TimeSpan originalLocalStartOffset = child.StartTimestamp - StartTimestamp;
                TimeSpan originalLocalEndOffset = child.EndTimestamp - StartTimestamp;

                // scale that local offset
                TimeSpan scaledLocalStartOffset = TimeSpan.FromTicks((long) (originalLocalStartOffset.Ticks * scale));
                TimeSpan scaledLocalEndOffset = TimeSpan.FromTicks((long) (originalLocalEndOffset.Ticks * scale));

                // calculate new timestamps                
                child.StartTimestamp = StartTimestamp + scaledLocalStartOffset;
                child.EndTimestamp = StartTimestamp + scaledLocalEndOffset;

                Debug.Assert(child.StartTimestamp >= StartTimestamp);
                Debug.Assert(child.EndTimestamp <= EndTimestamp);

                // because we've moved the origin (start timestamp) our relative offsets are no longer valid!
                // so, apply the same offset to all children (absolute value, because it's an offset and not a timestamp)
                child.MoveChildrenTimestamps(scaledLocalStartOffset - originalLocalStartOffset);

                // propagate it
                child.ScaleTimestamps(scale);
            }
        }

        private void MoveChildrenTimestamps(TimeSpan offset)
        {
            foreach(TimelineEntry child in ChildEntries)
            {
                child.StartTimestamp += offset;
                child.EndTimestamp += offset;

                child.MoveChildrenTimestamps(offset);
            }
        }
    }
}
