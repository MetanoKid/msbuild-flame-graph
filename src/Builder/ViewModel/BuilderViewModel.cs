using Model;
using MSBuildWrapper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Builder
{
    public class BuilderViewModel : PropertyChangeNotifier
    {
        public bool HasSolution
        {
            get
            {
                return Solution != null;
            }
        }

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
                OnPropertyChanged("HasSolution");
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

        public BuildConfigurationViewModel CurrentBuildConfiguration
        {
            get
            {
                return m_buildConfiguration;
            }

            set
            {
                m_buildConfiguration = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> ProjectsToBuild { get; private set; }
        public List<string> BuildTargets { get; private set; }

        private Solution m_solution;
        private SolutionCompiler m_solutionCompiler;
        private ObservableCollection<BuildMessage> m_buildMessages;
        private BuildConfigurationViewModel m_buildConfiguration;

        public BuilderViewModel()
        {
            Commands = new Commands(this);
            BuildMessages = new ObservableCollection<BuildMessage>();
            ProjectsToBuild = new ObservableCollection<string>();

            BuildTargets = new List<string>()
            {
                "Build",
                "Rebuild",
                "Clean",
            };
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

            CurrentBuildConfiguration = new BuildConfigurationViewModel();

            SelectDefaultValues();
        }

        private void SelectDefaultValues()
        {
            if(ProjectsToBuild.Count > 0)
            {
                CurrentBuildConfiguration.Project = ProjectsToBuild[0];
            }

            if(Solution != null && Solution.ValidConfigurationPlatforms.Count > 0)
            {
                CurrentBuildConfiguration.ConfigurationPlatform = Solution.ValidConfigurationPlatforms[0];
            }

            CurrentBuildConfiguration.Target = BuildTargets[0];

            CurrentBuildConfiguration.MaxParallelProjects = Environment.ProcessorCount;
            CurrentBuildConfiguration.MaxParallelCLTasksPerProject = Environment.ProcessorCount;
        }
    }
}
