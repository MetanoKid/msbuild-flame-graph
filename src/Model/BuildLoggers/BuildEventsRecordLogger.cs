using Microsoft.Build.Utilities;
using System.Collections.Generic;
using Microsoft.Build.Framework;

namespace Model
{
    public class BuildEventsRecordLogger : Logger
    {
        public List<BuildEventArgs> BuildEvents
        {
            get;
            private set;
        }

        public override void Initialize(IEventSource eventSource)
        {
            BuildEvents = new List<BuildEventArgs>();
            eventSource.AnyEventRaised += RecordEvent;
        }

        private void RecordEvent(object sender, BuildEventArgs e)
        {
            BuildEvents.Add(e);
        }
    }
}
