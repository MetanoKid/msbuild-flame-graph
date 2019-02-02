using Microsoft.Build.Framework;

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

        public BuildEventContext Context
        {
            get;
            set;
        }
    }
}
