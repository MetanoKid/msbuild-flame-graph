using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Model
{
    public delegate void OnBuildMessage(BuildMessage message);

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

        public ObservableCollection<BuildMessage> BuildMessages
        {
            get;
            private set;
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
        private OnBuildMessage m_onBuildMessage;

        public SolutionCompiler(Solution solution, OnBuildMessage onBuildMessage)
        {
            Solution = solution;
            IsReady = true;
            BuildMessages = new ObservableCollection<BuildMessage>();
            m_onBuildMessage = onBuildMessage;
        }

        public void Start()
        {
            LastCompilationResult = LastCompilationResult.None;
            IsReady = false;

            ////////////////////////
            ProjectCollection projectCollection = new ProjectCollection();
            BuildParameters parameters = new BuildParameters(projectCollection);
            parameters.MaxNodeCount = 4;
            parameters.Culture = new System.Globalization.CultureInfo("en-US");

            AllMessagesLogger logger = new AllMessagesLogger(m_onBuildMessage);
            logger.Verbosity = LoggerVerbosity.Quiet;
            parameters.Loggers = new[]
            {
                logger
            };

            Dictionary<string, string> globalProperties = new Dictionary<string, string>();
            globalProperties.Add("Configuration", "Debug");
            globalProperties.Add("Platform", "x64");

            string target = "Build";

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
