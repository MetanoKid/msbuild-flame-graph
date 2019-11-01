using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
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
