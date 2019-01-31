using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Model
{
    class AllMessagesLogger : Logger
    {
        private OnBuildMessage m_onBuildMessage;

        public AllMessagesLogger(OnBuildMessage onBuildMessage)
        {
            m_onBuildMessage = onBuildMessage;
        }

        public override void Initialize(IEventSource eventSource)
        {
            //eventSource.AnyEventRaised += OnAnyMessage;
            eventSource.ProjectStarted += OnAnyMessage;
            eventSource.ProjectFinished += OnAnyMessage;
        }

        private void OnAnyMessage(object sender, BuildEventArgs e)
        {
            BuildMessage message = new BuildMessage()
            {
                Type = e.GetType().ToString(),
                Message = e.Message
            };

            m_onBuildMessage(message);
        }
    }
}
