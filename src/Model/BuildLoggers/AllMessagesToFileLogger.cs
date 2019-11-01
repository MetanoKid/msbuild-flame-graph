using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;

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
