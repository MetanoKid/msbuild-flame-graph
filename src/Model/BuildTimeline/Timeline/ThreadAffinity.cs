using System.Collections.Generic;
using System.Diagnostics;

namespace Model.BuildTimeline
{
    public class ThreadAffinity
    {
        public static readonly int s_OffsetMSBuildEntries = 1000;
        public static readonly int s_OffsetFromParentPostProcessedEntries = 100;
        public static readonly int s_OffsetFromParentPostProcessedEntriesIncrement = 1;

        private static readonly int s_DefaultBaseThreadId = 0;
        private static readonly int s_DefaultBaseOffset = 0;
        private static readonly int s_DefaultThreadIdCalculationIncrement = 1;

        public int ThreadId { get; private set; }
        public bool Calculated { get; private set; }

        private HashSet<int> m_invalidThreadIDs;
        private int m_baseThreadId;
        private int m_baseOffset;
        private int m_threadIdCalculationIncrement;

        public ThreadAffinity()
        {
            m_baseThreadId = s_DefaultBaseThreadId;
            m_baseOffset = s_DefaultBaseOffset;
            m_threadIdCalculationIncrement = s_DefaultThreadIdCalculationIncrement;

            ThreadId = m_baseThreadId;
            Calculated = false;
            m_invalidThreadIDs = new HashSet<int>();
        }

        public void SetParameters(int baseThreadId, int baseOffset, int threadIdCalculationIncrement)
        {
            Debug.Assert(!Calculated);

            m_baseThreadId = baseThreadId;
            m_baseOffset = baseOffset;
            m_threadIdCalculationIncrement = threadIdCalculationIncrement;

            ThreadId = GetInitialThreadId();
        }

        public void AddInvalid(int threadID)
        {
            m_invalidThreadIDs.Add(threadID);
        }

        public void Calculate()
        {
            Debug.Assert(!Calculated);

            // we've been set an ID but it's become invalid?
            int initialThreadId = GetInitialThreadId();
            if(m_invalidThreadIDs.Contains(ThreadId))
            {
                ThreadId = initialThreadId;
            }

            // find the first valid one
            for(int i = 0; m_invalidThreadIDs.Contains(ThreadId); ++i)
            {
                ThreadId = initialThreadId + i * m_threadIdCalculationIncrement;
            }

            Calculated = true;
        }

        public void InheritDataFrom(ThreadAffinity other)
        {
            Debug.Assert(!Calculated);
            Debug.Assert(other.Calculated);

            foreach(int invalidThreadId in other.m_invalidThreadIDs)
            {
                m_invalidThreadIDs.Add(invalidThreadId);
            }
            
            m_baseThreadId = other.ThreadId;

            ThreadId = GetInitialThreadId();
        }

        private int GetInitialThreadId()
        {
            return m_baseThreadId + m_baseOffset;
        }
    }
}
