using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    public class ParallelFileCompilation
    {
        public BuildTimelineEntry Parent
        {
            get;
            private set;
        }

        public bool IsCompleted
        {
            get
            {
                return m_unfinishedCompilations.Count == 0;
            }
        }

        public List<FileCompilation> Compilations
        {
            get;
            private set;
        }

        private List<FileCompilation> m_unfinishedCompilations;

        public ParallelFileCompilation(BuildTimelineEntry entry)
        {
            Parent = entry;
            m_unfinishedCompilations = new List<FileCompilation>();
            Compilations = new List<FileCompilation>();
        }

        public void StartFileCompilation(string fileName, DateTime timestamp)
        {
            FileCompilation fileCompilation = new FileCompilation()
            {
                FileName = fileName,
                StartTimestamp = timestamp,
                ThreadId = 0,
            };

            // find out which thread it was assigned to
            while (m_unfinishedCompilations.Exists(cl => cl.ThreadId == fileCompilation.ThreadId))
            {
                ++fileCompilation.ThreadId;
            }

            m_unfinishedCompilations.Add(fileCompilation);
        }

        public void EndFrontEndCompilation(string fileName, DateTime timestamp, string message, string frontEndDLL)
        {
            FileCompilation fileCompilation = m_unfinishedCompilations.Find(cl => cl.FileName == fileName);
            if (fileCompilation != null)
            {
                fileCompilation.FrontEndFinishTime = timestamp;
                fileCompilation.FrontEndFinishMessage = message;
                fileCompilation.FrontEndDLL = frontEndDLL;
            }
        }

        public void EndFileCompilation(string fileName, DateTime timestamp, string message)
        {
            int index = m_unfinishedCompilations.FindIndex(cl => cl.FileName == fileName);
            if (index != -1)
            {
                FileCompilation fileCompilation = m_unfinishedCompilations[index];
                fileCompilation.BackEndFinishTime = timestamp;
                fileCompilation.EndTimestamp = timestamp;
                fileCompilation.BackEndFinishMessage = message;

                Debug.Assert(fileCompilation.ElapsedTime >= TimeSpan.Zero);

                m_unfinishedCompilations.RemoveAt(index);
                Compilations.Add(fileCompilation);
            }
        }
    }
}
