using Microsoft.Build.Framework;

namespace MSBuildWrapper
{
    // callback to execute with the raw BuildEventData message
    public delegate void OnBuildEventRawCallback(BuildEventArgs e);

    public class AllMessagesToCallbackRawLogger : AllMessagesToCallbackLogger<OnBuildEventRawCallback>
    {
        public AllMessagesToCallbackRawLogger(OnBuildEventRawCallback onBuildEvent) : base(onBuildEvent)
        {
        }

        protected override void OnAnyMessage(object sender, BuildEventArgs e)
        {
            m_onBuildEvent(e);
        }
    }
}
