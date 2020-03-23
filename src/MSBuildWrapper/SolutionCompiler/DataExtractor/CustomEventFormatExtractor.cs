using BuildTimeline;
using Model;
using Microsoft.Build.Framework;
using System.Collections.Generic;
using System.Linq;

namespace MSBuildWrapper
{    
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

        public override void BeforeBuildStarted(BuildConfiguration data)
        {
            base.BeforeBuildStarted(data);

            m_buildData = new BuildData()
            {
                BuildConfiguration = data,
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
