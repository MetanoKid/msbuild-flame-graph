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
                OnPropertyChanged("IsReady");
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

        public void Start(string configuration, string platform, string target, int maxParallelProjects, int maxParallelCL, List<CompilationDataExtractor> dataExtractors)
        {
            Debug.Assert(Status == CompilationStatus.NotStarted);

            Status = CompilationStatus.InProgress;

            // build the data to invoke MSBuild
            ProjectCollection projectCollection = new ProjectCollection();
            BuildParameters parameters = new BuildParameters(projectCollection);
            parameters.MaxNodeCount = maxParallelProjects;
            parameters.UICulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
            parameters.Loggers = dataExtractors.Select(e => e.Logger);

            Dictionary<string, string> globalProperties = new Dictionary<string, string>();
            globalProperties.Add("Configuration", configuration);
            globalProperties.Add("Platform", platform);
            globalProperties.Add("CL_MPCount", maxParallelCL.ToString());
            
            // add extra info to some tasks (CL, Lib/Link, ...)
            globalProperties.Add("ForceImportBeforeCppTargets", Path.GetFullPath(@"Resources\ExtraCompilerLinkerOptions.props"));

            // let data extractors know we're about to start
            dataExtractors.ForEach(e => e.BeforeBuildStarted(new CompilationDataExtractor.BuildStartedData()
            {
                SolutionPath = m_solution.Path,
                Configuration = configuration,
                Platform = platform,
                Target = target,
                MaxParallelProjects = parameters.MaxNodeCount,
                MaxParallelCLPerProject = maxParallelCL,
            }));

            // this represents our build
            BuildRequestData data = new BuildRequestData(m_solution.Path, globalProperties, null, new[] { target }, null);

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
