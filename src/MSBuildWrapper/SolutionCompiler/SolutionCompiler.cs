using Model;
using System.Collections.Generic;

namespace MSBuildWrapper
{
    public class SolutionCompiler : PropertyChangeNotifier
    {
        public static readonly string s_CompileFullSolution = "Full solution";

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

        private Compilation m_currentCompilation;

        public SolutionCompiler()
        {
        }

        public void Start(Solution solution, string project, string configuration, string platform, string target, int maxParallelProjects, int maxParallelCL, List<CompilationDataExtractor> dataExtractors)
        {
            CurrentCompilation = new Compilation(solution);
            CurrentCompilation.Start(project, configuration, platform, target, maxParallelProjects, maxParallelCL, dataExtractors);
        }
    }
}
