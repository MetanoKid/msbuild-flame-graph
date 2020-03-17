using Model;
using MSBuildWrapper;
using System.Collections.ObjectModel;

namespace Builder
{
    public class BuilderViewModel : PropertyChangeNotifier
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

        public ObservableCollection<BuildMessage> BuildMessages
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

        public string SelectedProjectToBuild
        {
            get
            {
                return m_selectedProjectToBuild;
            }

            set
            {
                m_selectedProjectToBuild = value;
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

        public ObservableCollection<string> ProjectsToBuild
        {
            get;
            private set;
        }

        private Solution m_solution;
        private SolutionCompiler m_solutionCompiler;
        private ObservableCollection<BuildMessage> m_buildMessages;
        private Solution.ConfigurationPlatform m_selectedConfigurationPlatform;
        private string m_selectedProjectToBuild;
        private string m_buildTarget;

        public BuilderViewModel()
        {
            Commands = new Commands(this);
            BuildMessages = new ObservableCollection<BuildMessage>();
            ProjectsToBuild = new ObservableCollection<string>();
        }

        public void LoadSolution(string path)
        {
            Solution = null;
            SolutionCompiler = null;
            BuildMessages.Clear();

            Solution = SolutionLoader.From(path);
            SolutionCompiler = new SolutionCompiler();
            
            ProjectsToBuild.Clear();
            ProjectsToBuild.Add(SolutionCompiler.s_CompileFullSolution);
            Solution.Projects.ForEach(_ => ProjectsToBuild.Add(_.Name));

            SelectDefaultValues();
        }

        private void SelectDefaultValues()
        {
            if(ProjectsToBuild.Count > 0)
            {
                SelectedProjectToBuild = ProjectsToBuild[0];
            }

            if(Solution != null && Solution.ValidConfigurationPlatforms.Count > 0)
            {
                SelectedConfigurationPlatform = Solution.ValidConfigurationPlatforms[0];
            }

            BuildTarget = "Build";
        }
    }
}
