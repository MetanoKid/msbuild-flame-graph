using System.Collections.Generic;

namespace Model
{
    public class Solution : DiskFile
    {
        public class SolutionMetadata
        {
            public string VisualStudioVersion { get; set; }
            public string MinimumVisualStudioVersion { get; set; }
        }
        
        public class ConfigurationPlatform
        {
            public string Configuration { get; set; }
            public string Platform { get; set; }

            public override string ToString()
            {
                return $"{Configuration} | {Platform}";
            }
        }

        public SolutionMetadata Metadata { get; set; }
        public List<Project> Projects { get; }
        public List<ConfigurationPlatform> ValidConfigurationPlatforms { get; }

        public Solution(string path)
        {
            Path = path;
            Projects = new List<Project>();
            ValidConfigurationPlatforms = new List<ConfigurationPlatform>();
        }
    }
}
