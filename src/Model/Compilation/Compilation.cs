using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Diagnostics;
using System.IO;

namespace Model
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

        /*public bool IsReady
        {
            get
            {
                return m_status != CompilationStatus.InProgress;
            }
        }

        public int NodeCount
        {
            get;
            private set;
        }*/

        private Solution m_solution;
        private CompilationStatus m_status;
        private CompilationResult m_result;

        private BuildEventsRecordLogger m_buildEventsRecordLogger;

        public Compilation(Solution solution)
        {
            Status = CompilationStatus.NotStarted;
            Result = CompilationResult.None;
            m_solution = solution;
            //m_buildEventsRecordLogger = new BuildEventsRecordLogger();
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
            dataExtractors.ForEach(e => e.BeforeBuildStarted());

            // this represents our build
            BuildRequestData data = new BuildRequestData(m_solution.Path, globalProperties, null, new[] { target }, null);

            // this call is synchronous, so it will stop here until it ends
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

            dataExtractors.ForEach(e => e.AfterBuildFinished());

            Status = CompilationStatus.Completed;
        }

        /*public List<BuildEventArgs> GetBuildEvents()
        {
            if(Status == CompilationStatus.Completed)
            {
                return m_buildEventsRecordLogger.BuildEvents;
            }

            return null;
        }*/
    }
}
