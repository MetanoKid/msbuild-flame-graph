using Microsoft.Build.Utilities;

namespace MSBuildWrapper
{
    public abstract class CompilationDataExtractor
    {
        // data used when starting the build
        public class BuildStartedData
        {
            public string SolutionPath { get; set; }
            public string Project { get; set; }
            public string Configuration { get; set; }
            public string Platform { get; set; }
            public string Target { get; set; }
            public int MaxParallelProjects { get; set; }
            public int MaxParallelCLPerProject { get; set; }
        }

        // which Logger instance is used to perform the extraction
        public Logger Logger { get; protected set; }

        // whether the extraction has finished
        public bool IsFinished { get; private set; }

        public CompilationDataExtractor()
        {
            IsFinished = false;
        }

        public virtual void BeforeBuildStarted(BuildStartedData data)
        {
            IsFinished = false;
        }

        public virtual void AfterBuildFinished(CompilationResult result)
        {
            IsFinished = true;
        }
    }
}
