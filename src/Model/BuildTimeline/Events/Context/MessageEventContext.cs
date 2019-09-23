using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.BuildTimeline
{
    class MessageEventContext : EventContext
    {
        // the ID of the project where this context lives in, if any
        public int ProjectId { get; set; }

        // the ID of the target where this context lives in, if any
        public int TargetId { get; set; }

        // the ID of the task where this context lives in, if any
        public int TaskId { get; set; }
    }
}
