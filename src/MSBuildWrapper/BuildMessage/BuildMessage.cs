﻿using Microsoft.Build.Framework;
using System;

namespace MSBuildWrapper
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
