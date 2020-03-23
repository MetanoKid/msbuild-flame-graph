using Microsoft.Build.Utilities;
using Model;

namespace MSBuildWrapper
{
    public abstract class CompilationDataExtractor
    {
        // which Logger instance is used to perform the extraction
        public Logger Logger { get; protected set; }

        // whether the extraction has finished
        public bool IsFinished { get; private set; }

        public CompilationDataExtractor()
        {
            IsFinished = false;
        }

        public virtual void BeforeBuildStarted(BuildConfiguration data)
        {
            IsFinished = false;
        }

        public virtual void AfterBuildFinished(CompilationResult result)
        {
            IsFinished = true;
        }
    }
}
