using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.BuildTimeline
{
    class ProjectEventContext : EventContext
    {
        // the ID of the project where this context lives in
        public int ProjectId { get; set; }
    }
}
