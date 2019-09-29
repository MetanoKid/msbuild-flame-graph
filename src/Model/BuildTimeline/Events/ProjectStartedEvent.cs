using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.BuildTimeline
{
    public class ProjectStartedEvent : Event
    {
        // context of the parent project that started this project, if any
        public EventContext ParentProjectEventContext { get; set; }

        // path to the file that contains the project data
        public string ProjectFile { get; set; }

        // the ID of the project, used within event contexts
        public int ProjectId { get; set; }
    }
}
