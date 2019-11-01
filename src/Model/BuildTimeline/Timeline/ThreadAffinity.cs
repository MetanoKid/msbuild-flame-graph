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
        private static readonly int s_DefaultBaseThreadId = 0;
        private static readonly int s_DefaultThreadIdCalculationIncrement = 1;

        public int ThreadId { get; private set; }
        public bool Calculated { get; private set; }

        private HashSet<int> m_invalidThreadIDs;
        private int m_baseThreadId;
        private int m_threadIdCalculationIncrement;

        public ThreadAffinity()
        {
            m_baseThreadId = s_DefaultBaseThreadId;
            m_threadIdCalculationIncrement = s_DefaultThreadIdCalculationIncrement;

            ThreadId = m_baseThreadId;
            Calculated = false;
            m_invalidThreadIDs = new HashSet<int>();
        }

        public void SetParameters(int baseThreadId, int threadIdCalculationIncrement)
        {
            Debug.Assert(!Calculated);

            m_baseThreadId = baseThreadId;
            m_threadIdCalculationIncrement = threadIdCalculationIncrement;

            ThreadId = m_baseThreadId;
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
                ThreadId = m_baseThreadId;
            }

            // find the first valid one
            for(int i = 0; m_invalidThreadIDs.Contains(ThreadId); ++i)
            {
                ThreadId = m_baseThreadId + i * m_threadIdCalculationIncrement;
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

            m_baseThreadId = other.m_baseThreadId;
            m_threadIdCalculationIncrement = other.m_threadIdCalculationIncrement;
            ThreadId = other.ThreadId;
        }
    }
}
