using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Model
{
    public abstract class AllMessagesLogger : Logger
    {
        public AllMessagesLogger()
        {
        }

        public override void Initialize(IEventSource eventSource)
        {
            eventSource.AnyEventRaised += OnAnyMessage;
        }

        protected abstract void OnAnyMessage(object sender, BuildEventArgs e);
    }
}
