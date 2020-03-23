using System;

namespace Model
{
    public class BuildConfiguration : PropertyChangeNotifier
    {
        public Solution.ConfigurationPlatform ConfigurationPlatform
        {
            get
            {
                return m_configurationPlatform;
            }

            set
            {
                m_configurationPlatform = value;
                OnPropertyChanged();
            }
        }

        public string Project
        {
            get
            {
                return m_project;
            }

            set
            {
                m_project = value;
                OnPropertyChanged();
            }
        }

        public string Target
        {
            get
            {
                return m_target;
            }

            set
            {
                m_target = value;
                OnPropertyChanged();
            }
        }

        public bool UseBtPlusFlag
        {
            get
            {
                return m_useBtPlusFlag;
            }

            set
            {
                m_useBtPlusFlag = value;
                OnPropertyChanged();
            }
        }

        public bool UseTimePlusFlag
        {
            get
            {
                return m_useTimePlusFlag;
            }

            set
            {
                m_useTimePlusFlag = value;
                OnPropertyChanged();
            }
        }

        public int MaxParallelProjects
        {
            get
            {
                return m_maxParallelProjects;
            }

            set
            {
                m_maxParallelProjects = value;
                OnPropertyChanged();
            }
        }

        public int MaxParallelCLTasksPerProject
        {
            get
            {
                return m_maxParallelCLTasksPerProject;
            }

            set
            {
                m_maxParallelCLTasksPerProject = value;
                OnPropertyChanged();
            }
        }

        private Solution.ConfigurationPlatform m_configurationPlatform;
        private string m_project;
        private string m_target;
        private bool m_useBtPlusFlag;
        private bool m_useTimePlusFlag;
        private int m_maxParallelProjects;
        private int m_maxParallelCLTasksPerProject;

        public BuildConfiguration()
        {
            ConfigurationPlatform = null;
            Project = null;
            Target = null;

            UseBtPlusFlag = false;
            UseTimePlusFlag = false;

            MaxParallelProjects = 1;
            MaxParallelCLTasksPerProject = 1;
        }
    }
}
