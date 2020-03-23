using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Model;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MSBuildWrapper
{
    public class Compilation : PropertyChangeNotifier
    {
        public CompilationStatus Status
        {
            get
            {
                return m_status;
            }

            private set
            {
                m_status = value;
                OnPropertyChanged();
            }
        }

        public CompilationResult Result
        {
            get
            {
                return m_result;
            }

            private set
            {
                m_result = value;
                OnPropertyChanged();
            }
        }

        private Solution m_solution;
        private CompilationStatus m_status;
        private CompilationResult m_result;

        public Compilation(Solution solution)
        {
            Status = CompilationStatus.NotStarted;
            Result = CompilationResult.None;
            m_solution = solution;
        }

        private string GetExtraFlagsFileName(BuildConfiguration buildConfiguration)
        {
            string flagsAsFileName = string.Empty;

            if(buildConfiguration.UseBtPlusFlag)
            {
                flagsAsFileName += "_BtPlus";
            }

            if(buildConfiguration.UseTimePlusFlag)
            {
                flagsAsFileName += "_TimePlus";
            }

            if(buildConfiguration.UseD1ReportTimeFlag)
            {
                flagsAsFileName += "_D1ReportTime";
            }

            if(!string.IsNullOrEmpty(flagsAsFileName))
            {
                return $"ExtraFlags{flagsAsFileName}.props";
            }

            return null;
        }

        public void Start(BuildConfiguration buildConfiguration, List<CompilationDataExtractor> dataExtractors)
        {
            Debug.Assert(Status == CompilationStatus.NotStarted);

            Status = CompilationStatus.InProgress;

            // MSBuild uses "Project:Target" syntax when building a single project, "Target" for full solution
            bool anyProjectSelected = buildConfiguration.Project != SolutionCompiler.s_CompileFullSolution;
            string projectTargetToBuild = anyProjectSelected ? $"{buildConfiguration.Project}:{buildConfiguration.Target}" : buildConfiguration.Target;

            // build the data to invoke MSBuild
            ProjectCollection projectCollection = new ProjectCollection();
            BuildParameters parameters = new BuildParameters(projectCollection);
            parameters.MaxNodeCount = buildConfiguration.MaxParallelProjects;
            parameters.UICulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
            parameters.Loggers = dataExtractors.Select(e => e.Logger);

            Dictionary<string, string> globalProperties = new Dictionary<string, string>();
            globalProperties.Add("Configuration", buildConfiguration.ConfigurationPlatform.Configuration);
            globalProperties.Add("Platform", buildConfiguration.ConfigurationPlatform.Platform);
            globalProperties.Add("CL_MPCount", buildConfiguration.MaxParallelCLTasksPerProject.ToString());

            // add extra info to some tasks (CL, Lib/Link, ...)
            string extraFlagsFileName = GetExtraFlagsFileName(buildConfiguration);
            if(extraFlagsFileName != null)
            {
                globalProperties.Add("ForceImportBeforeCppTargets", Path.GetFullPath($@"Resources\{extraFlagsFileName}"));
            }

            // let data extractors know we're about to start
            dataExtractors.ForEach(e => e.BeforeBuildStarted(new CompilationDataExtractor.BuildStartedData()
            {
                SolutionPath = m_solution.Path,
                Project = anyProjectSelected ? buildConfiguration.Project : null,
                Configuration = buildConfiguration.ConfigurationPlatform.Configuration,
                Platform = buildConfiguration.ConfigurationPlatform.Platform,
                Target = projectTargetToBuild,
                MaxParallelProjects = buildConfiguration.MaxParallelProjects,
                MaxParallelCLTasksPerProject = buildConfiguration.MaxParallelCLTasksPerProject,
                UseBtPlusFlag = buildConfiguration.UseBtPlusFlag,
                UseTimePlusFlag = buildConfiguration.UseTimePlusFlag,
                UseD1ReportTimeFlag = buildConfiguration.UseD1ReportTimeFlag,
            }));

            // this represents our build
            BuildRequestData data = new BuildRequestData(m_solution.Path, globalProperties, null, new[] { projectTargetToBuild }, null);

            // this call is synchronous, so it will stop here until build ends
            BuildResult result = BuildManager.DefaultBuildManager.Build(parameters, data);

            // process the result
            switch(result.OverallResult)
            {
                case BuildResultCode.Success:
                    Result = CompilationResult.Success;
                    break;
                case BuildResultCode.Failure:
                    Result = CompilationResult.Failure;
                    break;
            }

            dataExtractors.ForEach(e => e.AfterBuildFinished(Result));

            Status = CompilationStatus.Completed;
        }
    }
}
