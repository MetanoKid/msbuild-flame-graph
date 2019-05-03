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
        public Logger Logger
        {
            get;
            protected set;
        }

        public virtual void BeforeBuildStarted()
        {
        }

        public virtual void AfterBuildFinished()
        {
        }
    }
}
