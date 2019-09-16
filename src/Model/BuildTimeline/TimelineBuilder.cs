using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.BuildTimeline
{
    public class TimelineBuilder
    {
        private Compilation m_compilation;

        public TimelineBuilder(Compilation compilation)
        {
            m_compilation = compilation;
        }

        public Timeline Process()
        {
            if(m_compilation.Status != CompilationStatus.Completed)
            {
                return null;
            }

            Timeline timeline = new Timeline(0/*m_compilation.NodeCount*/);
            List<BuildEventArgs> messages = new List<BuildEventArgs>();
            foreach(BuildEventArgs e in messages/*m_compilation.GetBuildEvents()*/)
            {
                if(e is BuildStartedEventArgs)
                {
                    timeline.ProcessBuildStartEvent(e as BuildStartedEventArgs);
                }
                else if (e is BuildFinishedEventArgs)
                {
                    timeline.ProcessBuildEndEvent(e as BuildFinishedEventArgs);
                }
                else if (e is ProjectStartedEventArgs)
                {
                    timeline.ProcessProjectStartEvent(e as ProjectStartedEventArgs);
                }
                else if (e is ProjectFinishedEventArgs)
                {
                    timeline.ProcessProjectEndEvent(e as ProjectFinishedEventArgs);
                }
                else if (e is TargetStartedEventArgs)
                {
                    timeline.ProcessTargetStartEvent(e as TargetStartedEventArgs);
                }
                else if (e is TargetFinishedEventArgs)
                {
                    timeline.ProcessTargetEndEvent(e as TargetFinishedEventArgs);
                }
                else if (e is TaskStartedEventArgs)
                {
                    timeline.ProcessTaskStartEvent(e as TaskStartedEventArgs);
                }
                else if (e is TaskFinishedEventArgs)
                {
                    timeline.ProcessTaskEndEvent(e as TaskFinishedEventArgs);
                }
                else if(e is BuildMessageEventArgs)
                {
                    timeline.ProcessMessageEvent(e as BuildMessageEventArgs);
                }
            }

            timeline.CalculateParallelExecutions();

            return timeline;
        }
    }
}
