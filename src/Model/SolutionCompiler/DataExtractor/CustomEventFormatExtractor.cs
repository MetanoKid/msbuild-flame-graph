using Microsoft.Build.Framework;
using Model.BuildTimeline;
using System.Collections.Generic;
using System.Linq;

namespace Model
{
    public class BuildData
    {
        public string SolutionPath { get; set; }
        public string Configuration { get; set; }
        public string Platform { get; set; }
        public string Target { get; set; }
        public int MaxParallelProjects { get; set; }
        public int MaxParallelCLPerProject { get; set; }
        public List<Event> Events;
    }
    
    public class CustomEventFormatExtractor : CompilationDataExtractor
    {
        private BuildData m_buildData;
        private List<BuildEventArgs> m_rawEvents;
        private EventConverter m_converter;

        public BuildData BuildData
        {
            get
            {
                return m_buildData;
            }
        }

        public CustomEventFormatExtractor()
        {
            m_rawEvents = new List<BuildEventArgs>();
            m_converter = new EventConverter();
            Logger = new AllMessagesToCallbackRawLogger(OnBuildEvent);
        }

        public override void BeforeBuildStarted(BuildStartedData data)
        {
            base.BeforeBuildStarted(data);

            m_buildData = new BuildData()
            {
                SolutionPath = data.SolutionPath,
                Configuration = data.Configuration,
                Platform = data.Platform,
                Target = data.Target,
                MaxParallelProjects = data.MaxParallelProjects,
                MaxParallelCLPerProject = data.MaxParallelCLPerProject,
                Events = new List<Event>()
            };
        }

        public void OnBuildEvent(BuildEventArgs e)
        {
            m_rawEvents.Add(e);
        }

        public override void AfterBuildFinished(CompilationResult result)
        {
            // there can be null values if they aren't handled by the converter
            IEnumerable<Event> events = m_rawEvents.Select(_ => m_converter.Convert(_))
                                                   .Where(_ => _ != null);

            // collect all elements
            m_buildData.Events = events.ToList();
            
            base.AfterBuildFinished(result);
        }
    }
}
