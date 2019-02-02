using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    public class BuildTimelineBuilder
    {
        private Compilation m_compilation;

        public BuildTimelineBuilder(Compilation compilation)
        {
            m_compilation = compilation;
        }

        public BuildTimeline Process()
        {
            if(m_compilation.Status != CompilationStatus.Completed)
            {
                return null;
            }

            BuildTimeline timeline = new BuildTimeline();
            List<BuildEventArgs> rawBuildEvents = m_compilation.GetBuildEvents();

            // filter out BuildMessageEventArgs, we don't want the extra data
            IEnumerable<BuildEventArgs> buildEvents = rawBuildEvents.Where(e => !(e is BuildMessageEventArgs));

            foreach(BuildEventArgs e in buildEvents)
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
            }

            return timeline;
        }
    }
}
