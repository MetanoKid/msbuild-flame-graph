using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using System.Collections.Generic;

namespace Model
{
    public enum LastCompilationResult
    {
        None,
        Success,
        Failure
    }

    public class SolutionCompiler : PropertyChangeNotifier
    {
        public Solution Solution
        {
            get;
            private set;
        }

        public bool IsReady
        {
            get
            {
                return m_isReady;
            }

            private set
            {
                m_isReady = value;
                OnPropertyChanged();
            }
        }

        public LastCompilationResult LastCompilationResult
        {
            get
            {
                return m_lastCompilationResult;
            }

            private set
            {
                m_lastCompilationResult = value;
                OnPropertyChanged();
            }
        }

        private bool m_isReady;
        private LastCompilationResult m_lastCompilationResult;

        public SolutionCompiler(Solution solution)
        {
            Solution = solution;
            IsReady = true;
        }

        public void Start()
        {
            IsReady = false;

            ////////////////////////
            ProjectCollection projectCollection = new ProjectCollection();
            BuildParameters parameters = new BuildParameters(projectCollection);

            /*MyLogger logger = new MyLogger();

            parameters.Loggers = new[]
            {
                logger
            };*/

            Dictionary<string, string> globalProperties = new Dictionary<string, string>();
            globalProperties.Add("Configuration", "Debug");
            globalProperties.Add("Platform", "x64");

            string target = "Rebuild";

            BuildRequestData data = new BuildRequestData(Solution.Path,
                globalProperties, null, new[] { target }, null);

            BuildResult result = BuildManager.DefaultBuildManager.Build(parameters, data);
            ////////////////////////

            switch(result.OverallResult)
            {
                case BuildResultCode.Success:
                    LastCompilationResult = LastCompilationResult.Success;
                    break;
                case BuildResultCode.Failure:
                    LastCompilationResult = LastCompilationResult.Failure;
                    break;
            }

            IsReady = true;
        }
    }
}
