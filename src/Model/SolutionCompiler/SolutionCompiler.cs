﻿using Microsoft.Build.Evaluation;
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

        private Compilation m_currentCompilation;

        public SolutionCompiler()
        {
        }

        public void Start(Solution solution, string configuration, string platform, string target, int maxParallelProjects, int maxParallelCL, List<CompilationDataExtractor> dataExtractors)
        {
            CurrentCompilation = new Compilation(solution);
            CurrentCompilation.Start(configuration, platform, target, maxParallelProjects, maxParallelCL, dataExtractors);
        }
    }
}
