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

        public bool IsReady
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
        }

        private Solution m_solution;
        private Logger m_logger;
        private CompilationStatus m_status;
        private CompilationResult m_result;

        private BuildEventsRecordLogger m_buildEventsRecordLogger;

        public Compilation(Solution solution, Logger logger)
        {
            Status = CompilationStatus.NotStarted;
            Result = CompilationResult.None;
            m_buildEventsRecordLogger = new BuildEventsRecordLogger();

            m_solution = solution;
            m_logger = logger;
        }

        public void Start(string configuration, string platform, string target)
        {
            Debug.Assert(Status == CompilationStatus.NotStarted);

            Status = CompilationStatus.InProgress;

            ProjectCollection projectCollection = new ProjectCollection();
            BuildParameters parameters = new BuildParameters(projectCollection);
            parameters.MaxNodeCount = Environment.ProcessorCount;
            NodeCount = parameters.MaxNodeCount;
            parameters.UICulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");

            parameters.Loggers = new[]
            {
                m_logger,
                m_buildEventsRecordLogger
            };

            Dictionary<string, string> globalProperties = new Dictionary<string, string>();
            globalProperties.Add("Configuration", configuration);
            globalProperties.Add("Platform", platform);

            // TODO: process non-parallel compilations correctly
            //globalProperties.Add("CL_MPCount", "1");
            
            globalProperties.Add("ForceImportBeforeCppTargets", Path.GetFullPath(@"Resources\ExtraCompilerLinkerOptions.props"));

            BuildRequestData data = new BuildRequestData(m_solution.Path, globalProperties, null, new[] { target }, null);

            // will hang until compilation completes
            BuildResult result = BuildManager.DefaultBuildManager.Build(parameters, data);

            switch(result.OverallResult)
            {
                case BuildResultCode.Success:
                    Result = CompilationResult.Success;
                    break;
                case BuildResultCode.Failure:
                    Result = CompilationResult.Failure;
                    break;
            }

            Status = CompilationStatus.Completed;
        }

        public List<BuildEventArgs> GetBuildEvents()
        {
            if(Status == CompilationStatus.Completed)
            {
                return m_buildEventsRecordLogger.BuildEvents;
            }

            return null;
        }
    }
}
