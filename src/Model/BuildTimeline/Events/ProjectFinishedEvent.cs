using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.BuildTimeline
{
    class ProjectFinishedEvent : Event
    {
        // path to the file that contains the project data
        public string ProjectFile { get; set; }

        // whether the project built successfully
        public bool Succeeded { get; set; }
    }
}
