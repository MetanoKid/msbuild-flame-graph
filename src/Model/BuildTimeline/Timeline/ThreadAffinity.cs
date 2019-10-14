using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.BuildTimeline
{
    public class ThreadAffinity
    {
        public int ThreadId { get; private set; }
        public bool Calculated { get; private set; }

        private HashSet<int> m_invalidThreadIDs;

        public ThreadAffinity()
        {
            ThreadId = 0;
            Calculated = false;
            m_invalidThreadIDs = new HashSet<int>();
        }

        public void AddInvalid(int threadID)
        {
            m_invalidThreadIDs.Add(threadID);
        }

        public void Calculate()
        {
            Debug.Assert(!Calculated);

            // parent set an ID but it's become invalid?
            if(m_invalidThreadIDs.Contains(ThreadId))
            {
                ThreadId = 0;
            }

            // find the first valid one
            while(m_invalidThreadIDs.Contains(ThreadId))
            {
                ++ThreadId;
            }

            Calculated = true;
        }

        public void InheritDataFrom(ThreadAffinity other)
        {
            Debug.Assert(!Calculated);

            foreach(int invalidThreadId in other.m_invalidThreadIDs)
            {
                m_invalidThreadIDs.Add(invalidThreadId);
            }

            ThreadId = other.ThreadId;
        }
    }
}
