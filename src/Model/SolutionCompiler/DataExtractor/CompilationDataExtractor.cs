using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
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

        public virtual void BeforeBuildStarted()
        {
            IsFinished = false;
        }

        public virtual void AfterBuildFinished()
        {
            IsFinished = true;
        }
    }
}
