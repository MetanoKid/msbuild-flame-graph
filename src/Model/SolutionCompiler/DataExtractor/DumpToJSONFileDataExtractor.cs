using Microsoft.Build.Framework;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Model
{
    struct BuildData
    {
        public List<BuildEventArgs> Events;
    }
    
    public class DumpToJSONFileDataExtractor : CompilationDataExtractor
    {
        private string m_fileName;
        private BuildData m_buildData;

        public DumpToJSONFileDataExtractor(string fileName)
        {
            m_fileName = fileName;
            Logger = new AllMessagesToCallbackRawLogger(OnBuildEvent);
        }

        public override void BeforeBuildStarted()
        {
            m_buildData = new BuildData()
            {
                Events = new List<BuildEventArgs>()
            };
        }

        public void OnBuildEvent(BuildEventArgs e)
        {
            m_buildData.Events.Add(e);
        }

        public override void AfterBuildFinished()
        {
            string json = JsonConvert.SerializeObject(m_buildData,
                Formatting.Indented,
                new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto
                });

            File.WriteAllText(m_fileName, json);
        }
    }
}
