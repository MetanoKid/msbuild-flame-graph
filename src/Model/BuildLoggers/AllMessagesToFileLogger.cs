using Microsoft.Build.Framework;
using System;

namespace Model
{
    public class AllMessagesToFileLogger : AllMessagesLogger
    {
        private string m_fileName;

        public AllMessagesToFileLogger(string fileName)
        {
            m_fileName = fileName;
        }

        protected override void OnAnyMessage(object sender, BuildEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
