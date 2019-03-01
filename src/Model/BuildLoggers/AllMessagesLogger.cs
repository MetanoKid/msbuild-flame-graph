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
            eventSource.BuildStarted += OnAnyMessage;
            eventSource.BuildFinished += OnAnyMessage;
            eventSource.ProjectStarted += OnAnyMessage;
            eventSource.ProjectFinished += OnAnyMessage;
            eventSource.TargetStarted += OnAnyMessage;
            eventSource.TargetFinished += OnAnyMessage;
            eventSource.TaskStarted += OnAnyMessage;
            eventSource.TaskFinished += OnAnyMessage;
        }

        private void OnAnyMessage(object sender, BuildEventArgs e)
        {
            BuildMessage message = new BuildMessage()
            {
                Type = e.GetType().Name,
                Message = e.Message,
                Context = e.BuildEventContext,
                ParentContext = e is ProjectStartedEventArgs ? (e as ProjectStartedEventArgs).ParentProjectBuildEventContext : null,
                Timestamp = e.Timestamp,
            };

            m_onBuildMessage(message);
        }
    }
}
