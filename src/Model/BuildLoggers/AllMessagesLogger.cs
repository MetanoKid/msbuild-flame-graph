using System;
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
            eventSource.BuildStarted += OnAnyMessage;
            eventSource.BuildFinished += OnAnyMessage;
            eventSource.ProjectStarted += OnAnyMessage;
            eventSource.ProjectFinished += OnAnyMessage;
            eventSource.TargetStarted += OnAnyMessage;
            eventSource.TargetFinished += OnAnyMessage;
            eventSource.TaskStarted += OnAnyMessage;
            eventSource.TaskFinished += OnAnyMessage;
            eventSource.MessageRaised += OnAnyMessage;
            eventSource.WarningRaised += OnAnyMessage;
            eventSource.ErrorRaised += OnAnyMessage;
        }

        protected abstract void OnAnyMessage(object sender, BuildEventArgs e);
    }
}
