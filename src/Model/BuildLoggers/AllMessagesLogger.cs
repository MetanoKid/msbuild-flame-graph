using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Model
{
    class AllMessagesLogger : Logger
    {
        private OnBuildMessage m_onBuildMessage;
        private int m_currentIndentation;

        public AllMessagesLogger(OnBuildMessage onBuildMessage)
        {
            m_onBuildMessage = onBuildMessage;
            m_currentIndentation = 0;
        }

        public override void Initialize(IEventSource eventSource)
        {
            eventSource.BuildStarted += OnBuildStarted;
            eventSource.BuildFinished += OnBuildFinished;

            eventSource.ProjectStarted += OnProjectStarted;
            eventSource.ProjectFinished += OnProjectFinished;

            eventSource.TaskStarted += OnTaskStarted;
            eventSource.TaskFinished += OnTaskFinished;
        }

        private void OnBuildStarted(object sender, BuildStartedEventArgs e)
        {
            OnAnyMessage(sender, e);
            ++m_currentIndentation;
        }

        private void OnBuildFinished(object sender, BuildFinishedEventArgs e)
        {
            --m_currentIndentation;
            OnAnyMessage(sender, e);
        }

        private void OnProjectStarted(object sender, ProjectStartedEventArgs e)
        {
            OnAnyMessage(sender, e);
            ++m_currentIndentation;
        }

        private void OnProjectFinished(object sender, ProjectFinishedEventArgs e)
        {
            --m_currentIndentation;
            OnAnyMessage(sender, e);
        }

        private void OnTaskStarted(object sender, TaskStartedEventArgs e)
        {
            OnAnyMessage(sender, e);
            ++m_currentIndentation;
        }

        private void OnTaskFinished(object sender, TaskFinishedEventArgs e)
        {
            --m_currentIndentation;
            OnAnyMessage(sender, e);
        }

        private void OnAnyMessage(object sender, BuildEventArgs e)
        {
            // careful, this whole system only works properly when building sequentially
            //requires taking a look at build context
            string indentation = "";
            for(int i = 0; i < m_currentIndentation; ++i)
            {
                indentation += "    ";
            }

            BuildMessage message = new BuildMessage()
            {
                Type = e.GetType().Name,
                Message = indentation + e.Message,
                ThreadId = e.ThreadId
            };

            m_onBuildMessage(message);
        }
    }
}
