using Microsoft.Build.Framework;
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

        public BuildEventContext Context
        {
            get;
            set;
        }

        public BuildEventContext ParentContext
        {
            get;
            set;
        }
    }
}
