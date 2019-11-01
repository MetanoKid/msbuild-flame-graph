using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;

namespace Model
{
    // logger that executes a callback when an event is logged
    public abstract class AllMessagesToCallbackLogger<T> : AllMessagesLogger
    {
        protected T m_onBuildEvent;

        public AllMessagesToCallbackLogger(T onBuildEvent)
        {
            m_onBuildEvent = onBuildEvent;
        }
    }
}
