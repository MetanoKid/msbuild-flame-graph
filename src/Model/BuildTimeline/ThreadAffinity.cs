using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    public class ThreadAffinity
    {
        public int ThreadId
        {
            get;
            set;
        }

        public HashSet<int> InvalidThreadIds
        {
            get;
            set;
        }

        public bool Calculated
        {
            get;
            set;
        }

        public ThreadAffinity()
        {
            ThreadId = 0;
            InvalidThreadIds = new HashSet<int>();
            Calculated = false;
        }
    }
}
