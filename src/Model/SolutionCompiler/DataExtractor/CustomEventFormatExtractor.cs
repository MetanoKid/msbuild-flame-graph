using Microsoft.Build.Framework;
using Model.BuildTimeline;
using System.Collections.Generic;
using System.Linq;

namespace Model
{
    public struct BuildData
    {
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

        public override void BeforeBuildStarted()
        {
            m_buildData = new BuildData()
            {
                Events = new List<Event>()
            };
        }

        public void OnBuildEvent(BuildEventArgs e)
        {
            m_rawEvents.Add(e);
        }

        public override void AfterBuildFinished()
        {
            // there can be null values if they aren't handled by the converter
            IEnumerable<Event> events = m_rawEvents.Select((e) => m_converter.Convert(e))
                                                   .Where((e) => e != null);

            // collect all elements
            m_buildData.Events = events.ToList();
            
            base.AfterBuildFinished();
        }
    }
}
