using System.Collections.Generic;

namespace BuildTimeline
{
    public class Timeline
    {
        public List<TimelineEntry>[] PerNodeRootEntries { get; private set; }

        public Timeline(int nodeCount)
        {
            // index 0 will be used for the build only
            PerNodeRootEntries = new List<TimelineEntry>[nodeCount + 1];

            for(int i = 0; i < PerNodeRootEntries.Length; ++i)
            {
                PerNodeRootEntries[i] = new List<TimelineEntry>();
            }
        }

        public void AddRoot(TimelineEntry root)
        {
            PerNodeRootEntries[root.NodeId].Add(root);
        }
    }
}
