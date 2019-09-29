using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.BuildTimeline
{
    public class MessageEvent : Event
    {
        // code associated with the event
        public string Code { get; set; }

        // path to the file that contains the project data
        public string ProjectFile { get; set; }

        // path to the file related to the message, if any
        public string File { get; set; }

        // the custom sub-type of the event (as reported by MSBuild)
        public string Subcategory { get; set; }

        // start line where the message points to, if any
        public int LineStart { get; set; }

        // end line where the message points to, if any
        public int LineEnd { get; set; }

        // start column where the message points to, if any
        public int ColumnStart { get; set; }

        // end column where the message points to, if any
        public int ColumnEnd { get; set; }
    }
}
