using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Model
{
    public abstract class SolutionLoader
    {
        private readonly static Regex s_regexFileStart = new Regex(@"^Microsoft Visual Studio Solution File, Format Version ([\d.]+)$");
        private readonly static Regex s_regexVisualStudioVersion = new Regex(@"^VisualStudioVersion = (.+)$");
        private readonly static Regex s_regexMinimumVisualStudioVersion = new Regex(@"^MinimumVisualStudioVersion = (.+)$");
        private readonly static Regex s_regexProjectStart = new Regex(@"^Project\(""\{(.+)\}""\) = ""(.+)"", ""(.+)"", ""\{(.+)\}""$");
        private readonly static Regex s_regexProjectEnd = new Regex(@"^EndProject$");
        private readonly static Regex s_regexProjectDependenciesStart = new Regex(@"^\s+ProjectSection\(ProjectDependencies\) = postProject$");
        private readonly static Regex s_regexProjectDependenciesEnd = new Regex(@"^\s+EndProjectSection$");
        private readonly static Regex s_regexProjectDependency = new Regex(@"^\s+\{(.+)\} = \{(.+)\}$");
        private readonly static Regex s_regexGlobalDataStart = new Regex(@"^Global$");
        private readonly static Regex s_regexGlobalDataEnd = new Regex(@"^EndGlobal$");
        private readonly static Regex s_regexConfigurationPlatformStart = new Regex(@"^\s+GlobalSection\(SolutionConfigurationPlatforms\) = preSolution$");
        private readonly static Regex s_regexConfigurationPlatformEnd = new Regex(@"^\s+EndGlobalSection$");
        private readonly static Regex s_regexConfigurationPlatform = new Regex(@"^\s+(\w+)\|(\w+) = (\w+)\|(\w+)$");

        public static Solution From(string path)
        {
            IEnumerable<string> solutionLines = File.ReadAllLines(path).Where(_ => _ != "");

            if(!solutionLines.Any(_ => s_regexFileStart.IsMatch(_)))
            {
                throw new ArgumentException("Selected path doesn't belong to a valid Visual Studio solution");
            }

            Solution solution = new Solution(path);
            solution.Metadata = ProcessSolutionMetadata(solutionLines, solution);
            solution.Projects.AddRange(ProcessProjectEntries(solutionLines, solution));
            solution.ValidConfigurationPlatforms.AddRange(ProcessGlobalSections(solutionLines, solution));

            return solution;
        }

        private static Solution.SolutionMetadata ProcessSolutionMetadata(IEnumerable<string> solutionLines, Solution solution)
        {
            // extract metadata
            Solution.SolutionMetadata metadata = new Solution.SolutionMetadata()
            {
                VisualStudioVersion = solutionLines.Select(_ => s_regexVisualStudioVersion.Match(_)).Where(_ => _.Success).FirstOrDefault()?.Groups[1].Value,
                MinimumVisualStudioVersion = solutionLines.Select(_ => s_regexMinimumVisualStudioVersion.Match(_)).Where(_ => _.Success).FirstOrDefault()?.Groups[1].Value,
            };

            return metadata;
        }

        private static List<Project> ProcessProjectEntries(IEnumerable<string> solutionLines, Solution solution)
        {
            // find every project entry
            IEnumerable<string> projectStarts = solutionLines.Where(_ => s_regexProjectStart.IsMatch(_));
            
            // for each entry, select the lines between the project header and its end
            IEnumerable<IEnumerable<string>> perProjectLines = projectStarts.Select(header => solutionLines.SkipWhile(line => line != header)
                                                                                                            .TakeWhile(line => !s_regexProjectEnd.IsMatch(line)));

            // now process each group into a project
            List<Project> projects = new List<Project>();
            foreach(IEnumerable<string> projectLines in perProjectLines)
            {
                // first item is the one with the project info
                List<string> lines = projectLines.ToList();
                Debug.Assert(lines.Count > 0);

                Match projectDataMatch = s_regexProjectStart.Match(lines[0]);
                Debug.Assert(projectDataMatch.Success);

                // regex capture groups: type uuid, name, relative path, uuid
                Project p = new Project(Path.GetDirectoryName(solution.Path) + Path.DirectorySeparatorChar + projectDataMatch.Groups[3].Value,
                                        projectDataMatch.Groups[1].Value,
                                        projectDataMatch.Groups[2].Value,
                                        projectDataMatch.Groups[4].Value);

                projects.Add(p);
            }
            
            // process dependencies between projects, now that they're built
            IEnumerable<Tuple<Project, IEnumerable<string>>> projectsWithDefinitionLines = projects.Zip(perProjectLines, (project, lines) => new Tuple<Project, IEnumerable<string>>(project, lines));
            foreach(var tuple in projectsWithDefinitionLines)
            {
                // take the dependency block for each project
                IEnumerable<string> dependencies = tuple.Item2.SkipWhile(_ => !s_regexProjectDependenciesStart.IsMatch(_))
                                                              .Skip(1)
                                                              .TakeWhile(_ => !s_regexProjectDependenciesEnd.IsMatch(_));

                // extract each dependency and match it with the project instance with the same UUID
                tuple.Item1.DependsOn.AddRange(dependencies.Select(line =>
                {
                    Match projectDependencyMatch = s_regexProjectDependency.Match(line);
                    Debug.Assert(projectDependencyMatch.Success);
                    Debug.Assert(projectDependencyMatch.Groups[1].Value == projectDependencyMatch.Groups[2].Value);

                    return projects.Find(_ => _.UUID == projectDependencyMatch.Groups[1].Value);
                }));
            }

            Debug.Assert(projects.Count > 0);
            return projects;
        }

        private static List<Solution.ConfigurationPlatform> ProcessGlobalSections(IEnumerable<string> solutionLines, Solution solution)
        {
            // take the global data block
            IEnumerable<string> globalData = solutionLines.SkipWhile(_ => !s_regexGlobalDataStart.IsMatch(_))
                                                          .TakeWhile(_ => !s_regexGlobalDataEnd.IsMatch(_));

            // take the SolutionConfigurationPlatform block from there
            IEnumerable<string> configurationPlatformSectionLines = globalData.SkipWhile(_ => !s_regexConfigurationPlatformStart.IsMatch(_))
                                                                              .Skip(1)
                                                                              .TakeWhile(_ => !s_regexConfigurationPlatformEnd.IsMatch(_));

            // extract each pair of configuration and platform
            IEnumerable<Solution.ConfigurationPlatform> configurationPlatforms = configurationPlatformSectionLines.Select(line =>
            {
                Match match = s_regexConfigurationPlatform.Match(line);
                Debug.Assert(match.Success);
                Debug.Assert(match.Groups[1].Value == match.Groups[3].Value);
                Debug.Assert(match.Groups[2].Value == match.Groups[4].Value);

                return new Solution.ConfigurationPlatform()
                {
                    Configuration = match.Groups[1].Value,
                    Platform = match.Groups[2].Value
                };
            });

            return configurationPlatforms.ToList();
        }
    }
}
