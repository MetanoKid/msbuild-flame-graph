using Model;
using System.Collections.ObjectModel;

namespace Builder
{
    public class BuilderViewModel : Model.PropertyChangeNotifier
    {
        public Commands Commands { get; private set; }

        public Solution Solution
        {
            get
            {
                return m_solution;
            }

            set
            {
                m_solution = value;
                OnPropertyChanged();
            }
        }

        public SolutionCompiler SolutionCompiler
        {
            get
            {
                return m_solutionCompiler;
            }

            set
            {
                m_solutionCompiler = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Model.BuildMessage> BuildMessages
        {
            get
            {
                return m_buildMessages;
            }

            set
            {
                m_buildMessages = value;
                OnPropertyChanged();
            }
        }

        public Solution.ConfigurationPlatform SelectedConfigurationPlatform
        {
            get
            {
                return m_selectedConfigurationPlatform;
            }

            set
            {
                m_selectedConfigurationPlatform = value;
                OnPropertyChanged();
            }
        }

        public string BuildTarget
        {
            get
            {
                return m_buildTarget;
            }

            set
            {
                m_buildTarget = value;
                OnPropertyChanged();
            }
        }

        private Solution m_solution;
        private SolutionCompiler m_solutionCompiler;
        private ObservableCollection<Model.BuildMessage> m_buildMessages;
        private Solution.ConfigurationPlatform m_selectedConfigurationPlatform;
        private string m_buildTarget;

        public BuilderViewModel()
        {
            Commands = new Commands(this);
            BuildMessages = new ObservableCollection<BuildMessage>();
        }

        public void LoadSolution(string path)
        {
            Solution = null;
            SolutionCompiler = null;
            BuildMessages.Clear();

            Solution = SolutionLoader.From(path);
            SolutionCompiler = new SolutionCompiler();
        }
    }
}
