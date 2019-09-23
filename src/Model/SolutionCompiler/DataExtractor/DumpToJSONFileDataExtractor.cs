using Microsoft.Build.Framework;
using Model.BuildTimeline;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Model
{
    struct BuildData
    {
        public List<Event> Events;
    }
    
    public class DumpToJSONFileDataExtractor : CompilationDataExtractor
    {
        private string m_fileName;
        private BuildData m_buildData;
        private List<BuildEventArgs> m_rawEvents;
        private EventConverter m_converter;

        public DumpToJSONFileDataExtractor(string fileName)
        {
            m_fileName = fileName;
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
            
            string json = JsonConvert.SerializeObject(m_buildData,
                Formatting.Indented,
                new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                });

            File.WriteAllText(m_fileName, json);

            // TODO: implement
            //  done: iterate over all build events
            //  done:  convert event to custom abstraction, via factory or similar
            //  done: dump custom events to JSON file, as a backup
            // build timeline from custom events
            // dump timeline with requested format

            /*
            // read file, deserialize data and dump it to another file so we can compare
            // can't use m_buildData.Events.SequenceEqual(data.Events) because no equality is implemented
            string json2 = File.ReadAllText(m_fileName);
            BuildData data = JsonConvert.DeserializeObject<BuildData>(json2,
                new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.Auto
                });

            string fileName2 = Path.GetDirectoryName(m_fileName) + Path.DirectorySeparatorChar +
                               Path.GetFileNameWithoutExtension(m_fileName) + "_2" + Path.GetExtension(m_fileName);
            File.WriteAllText(fileName2, JsonConvert.SerializeObject(data,
                Formatting.Indented,
                new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto
                }));
            */
        }
    }
}
