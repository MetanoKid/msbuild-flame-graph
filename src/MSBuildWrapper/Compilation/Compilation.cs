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

            // build the data to invoke MSBuild
            // failing to provide a ProjectCollection won't ensure project order
            ProjectCollection projectCollection = new ProjectCollection();
            System.Globalization.CultureInfo cultureInfo = System.Globalization.CultureInfo.GetCultureInfo("en-US");
            BuildParameters parameters = new BuildParameters(projectCollection)
            {
                MaxNodeCount = buildConfiguration.MaxParallelProjects,
                UICulture = cultureInfo,
                Culture = cultureInfo,
                Loggers = dataExtractors.Select(e => e.Logger),
            };

            // these properties are fed to MSBuild
            Dictionary<string, string> globalProperties = new Dictionary<string, string>
            {
                { "Configuration", buildConfiguration.Configuration },
                { "Platform", buildConfiguration.Platform },
                { "CL_MPCount", buildConfiguration.MaxParallelCLTasksPerProject.ToString() }
            };

            // add extra info to some tasks (CL, Lib/Link, ...)
            string extraFlagsFileName = GetExtraFlagsFileName(buildConfiguration);
            if(extraFlagsFileName != null)
            {
                globalProperties.Add("ForceImportBeforeCppTargets", Path.GetFullPath($@"Resources\{extraFlagsFileName}"));
            }

            // let data extractors know we're about to start
            dataExtractors.ForEach(e => e.BeforeBuildStarted(buildConfiguration));

            // this represents our build
            BuildRequestData data = new BuildRequestData(m_solution.Path, globalProperties, null, new[] { buildConfiguration.Target }, null);

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
