using Microsoft.Build.Framework;

namespace Model
{
    // callback to execute with the intermediate UI message data
    public delegate void OnBuildMessage(BuildMessage message);

    public class AllMessagesToCallbackUILogger : AllMessagesToCallbackLogger<OnBuildMessage>
    {
        public AllMessagesToCallbackUILogger(OnBuildMessage onBuildEvent) : base(onBuildEvent)
        {
        }

        protected override void OnAnyMessage(object sender, BuildEventArgs e)
        {
            // data that's being shown in the UI
            BuildMessage message = new BuildMessage()
            {
                Type = e.GetType().Name,
                Message = e.Message,
                Context = e.BuildEventContext,
                ParentContext = e is ProjectStartedEventArgs ? (e as ProjectStartedEventArgs).ParentProjectBuildEventContext : null,
                Timestamp = e.Timestamp,
            };

            m_onBuildEvent(message);
        }
    }
}
