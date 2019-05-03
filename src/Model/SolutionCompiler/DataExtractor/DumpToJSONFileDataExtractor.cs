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
    class MSBuildEventTypeNameProvider : IValueProvider
    {
        public object GetValue(object target)
        {
            if(!(target is BuildEventArgs))
            {
                throw new InvalidOperationException();
            }

            return target.GetType().Name;
        }

        public void SetValue(object target, object value)
        {
            throw new InvalidOperationException();
        }
    }

    class AddTypeIngoreNonInterestingPropertiesContractResolver : DefaultContractResolver
    {
        private static List<string> s_ignoredPropertyNames = new List<string>()
        {
            "BuildEnvironment",
            "Properties",
            "Items",
            "GlobalProperties"
        };

        private static MSBuildEventTypeNameProvider s_eventTypeNameProvider = new MSBuildEventTypeNameProvider();

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);

            // add type to MSBuild's BuildEventArgs
            if(type.IsSubclassOf(typeof(BuildEventArgs)) || type == typeof(BuildEventArgs))
            {
                JsonProperty typeProperty = new JsonProperty()
                {
                    PropertyName = "EventType",
                    PropertyType = typeof(string),
                    Readable = true,
                    Writable = false,
                    ValueProvider = s_eventTypeNameProvider
                };
                properties.Add(typeProperty);
            }

            return properties.Where(p => !s_ignoredPropertyNames.Contains(p.PropertyName)).ToList();
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
                    ContractResolver = new AddTypeIngoreNonInterestingPropertiesContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore
                });

            File.WriteAllText(m_fileName, json);
        }
    }
}
