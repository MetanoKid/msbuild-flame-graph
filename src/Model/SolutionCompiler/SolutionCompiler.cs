using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Model
{
    public delegate void OnBuildMessage(BuildMessage message);

    public class SolutionCompiler : PropertyChangeNotifier
    {
        public Compilation CurrentCompilation
        {
            get
            {
                return m_currentCompilation;
            }

            private set
            {
                m_currentCompilation = value;
                OnPropertyChanged();
            }
        }

        // TODO: not working properly, this property isn't re-evaluated and button is always enabled after a solution is loaded
        public bool CanCompile
        {
            get
            {
                return CurrentCompilation == null || CurrentCompilation.Status != CompilationStatus.InProgress;
            }
        }

        private Compilation m_currentCompilation;
        private Logger m_logger;

        public SolutionCompiler(OnBuildMessage onBuildMessage)
        {
            m_logger = new AllMessagesLogger(onBuildMessage);
        }

        public void Start(Solution solution, string configuration, string platform, string target)
        {
            CurrentCompilation = new Model.Compilation(solution, m_logger);
            CurrentCompilation.Start(configuration, platform, target);
        }
    }
}
