using Model;
using System.Collections.Generic;
using Microsoft.Build.Locator;

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
            if(!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }
        }

        public void Start(Solution solution, BuildConfiguration buildConfiguration, List<CompilationDataExtractor> dataExtractors)
        {
            CurrentCompilation = new Compilation(solution);
            CurrentCompilation.Start(buildConfiguration, dataExtractors);
        }
    }
}
