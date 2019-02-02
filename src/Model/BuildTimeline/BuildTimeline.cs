using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Model
{
    public class BuildTimelineEntry
    {
        public BuildEventArgs StartBuildEvent
        {
            get;
            set;
        }

        public BuildEventArgs EndBuildEvent
        {
            get;
            set;
        }

        public TimeSpan ElapsedTime
        {
            get
            {
                return EndBuildEvent.Timestamp - StartBuildEvent.Timestamp;
            }
        }

        public List<BuildTimelineEntry> Children
        {
            get;
            private set;
        }

        public BuildTimelineEntry()
        {
            Children = new List<BuildTimelineEntry>();
        }
    }

    /////////////////////////////////////////////////

    public class BuildTimeline
    {
        public BuildTimelineEntry RootTimelineEntry
        {
            get;
            private set;
        }

        private Stack<BuildTimelineEntry> m_processingEntriesStack;

        public BuildTimeline()
        {
            RootTimelineEntry = new BuildTimelineEntry();
            m_processingEntriesStack = new Stack<BuildTimelineEntry>();
        }

        // Build

        public void ProcessBuildStartEvent(BuildStartedEventArgs e)
        {
            RootTimelineEntry.StartBuildEvent = e;
            m_processingEntriesStack.Push(RootTimelineEntry);
        }

        public void ProcessBuildEndEvent(BuildFinishedEventArgs e)
        {
            BuildTimelineEntry entry = m_processingEntriesStack.Pop();
            entry.EndBuildEvent = e;
        }

        // Project

        public void ProcessProjectStartEvent(ProjectStartedEventArgs e)
        {
            BuildTimelineEntry projectEntry = new BuildTimelineEntry()
            {
                StartBuildEvent = e
            };

            m_processingEntriesStack.Peek().Children.Add(projectEntry);

            m_processingEntriesStack.Push(projectEntry);
        }

        public void ProcessProjectEndEvent(ProjectFinishedEventArgs e)
        {
            BuildTimelineEntry entry = m_processingEntriesStack.Pop();
            entry.EndBuildEvent = e;
        }

        // Target

        public void ProcessTargetStartEvent(TargetStartedEventArgs e)
        {
            BuildTimelineEntry targetEntry = new BuildTimelineEntry()
            {
                StartBuildEvent = e
            };

            m_processingEntriesStack.Peek().Children.Add(targetEntry);

            m_processingEntriesStack.Push(targetEntry);
        }

        // Task

        public void ProcessTargetEndEvent(TargetFinishedEventArgs e)
        {
            BuildTimelineEntry entry = m_processingEntriesStack.Pop();
            entry.EndBuildEvent = e;
        }

        public void ProcessTaskStartEvent(TaskStartedEventArgs e)
        {
            BuildTimelineEntry taskEntry = new BuildTimelineEntry()
            {
                StartBuildEvent = e
            };

            m_processingEntriesStack.Peek().Children.Add(taskEntry);

            m_processingEntriesStack.Push(taskEntry);
        }

        public void ProcessTaskEndEvent(TaskFinishedEventArgs e)
        {
            BuildTimelineEntry entry = m_processingEntriesStack.Pop();
            entry.EndBuildEvent = e;
        }

        public bool IsCompleted()
        {
            return m_processingEntriesStack.Count == 0;
        }
    }
}
