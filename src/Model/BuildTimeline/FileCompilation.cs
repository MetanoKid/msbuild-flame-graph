using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    public class FileCompilation
    {
        public string FileName;
        public DateTime StartTimestamp;
        public DateTime EndTimestamp;
        public int ThreadId;

        public DateTime FrontEndFinishTime;
        public string FrontEndFinishMessage;
        public string FrontEndDLL;
        public DateTime BackEndFinishTime;
        public string BackEndFinishMessage;

        public TimeSpan ElapsedTime
        {
            get
            {
                return EndTimestamp - StartTimestamp;
            }
        }
    }
}
