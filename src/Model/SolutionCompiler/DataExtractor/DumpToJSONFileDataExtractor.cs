using Microsoft.Build.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    class IgnoreNonInterestingPropertiesContractResolver : DefaultContractResolver
    {
        private List<string> m_ignoredPropertyNames = new List<string>()
        {
            "BuildEnvironment",
            "Properties",
            "Items",
            "GlobalProperties"
        };

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);
            return properties.Where(p => !m_ignoredPropertyNames.Contains(p.PropertyName)).ToList();
        }
    }

    //---------------------------------

    public class DumpToJSONFileDataExtractor : CompilationDataExtractor
    {
        private string m_fileName;
        private List<BuildEventArgs> m_buildEvents;

        public DumpToJSONFileDataExtractor(string fileName)
        {
            m_fileName = fileName;
            Logger = new AllMessagesToCallbackRawLogger(OnBuildEvent);
            m_buildEvents = new List<BuildEventArgs>();
        }

        public override void BeforeBuildStarted()
        {
        }

        public void OnBuildEvent(BuildEventArgs e)
        {
            m_buildEvents.Add(e);
        }

        public override void AfterBuildFinished()
        {
            string json = JsonConvert.SerializeObject(m_buildEvents,
                Formatting.Indented,
                new JsonSerializerSettings()
                {
                    ContractResolver = new IgnoreNonInterestingPropertiesContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore
                });

            File.WriteAllText(m_fileName, json);
        }
    }
}
