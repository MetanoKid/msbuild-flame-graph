using System;

// TODO: refactor this class so it's UI-only

namespace Model
{
    public class BuildMessage
    {
        public string Type
        {
            get;
            set;
        }

        public string Message
        {
            get;
            set;
        }

        public DateTime Timestamp
        {
            get;
            set;
        }

        public Microsoft.Build.Framework.BuildEventContext Context
        {
            get;
            set;
        }

        public Microsoft.Build.Framework.BuildEventContext ParentContext
        {
            get;
            set;
        }
    }
}
