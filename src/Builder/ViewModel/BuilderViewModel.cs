using Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Builder
{
    public class BuilderViewModel : Model.PropertyChangeNotifier
    {
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

        public string BuildConfiguration
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

        public string BuildPlatform
        {
            get
            {
                return m_buildPlatform;
            }

            set
            {
                m_buildPlatform = value;
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
        private string m_buildConfiguration;
        private string m_buildTarget;
        private string m_buildPlatform;
    }
}
