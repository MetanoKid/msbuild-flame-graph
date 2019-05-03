using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    public class AllMessagesToCallbackUILogger : AllMessagesToCallbackLogger<OnBuildMessage>
    {
        public AllMessagesToCallbackUILogger(OnBuildMessage onBuildEvent) : base(onBuildEvent)
        {
        }

        protected override void OnAnyMessage(object sender, BuildEventArgs e)
        {
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
