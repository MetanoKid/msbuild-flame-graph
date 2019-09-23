using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.BuildTimeline
{
    class TargetEventContext : EventContext
    {
        // the ID of the project where this context lives in
        public int ProjectId { get; set; }

        // the ID of the target where this context lives in
        public int TargetId { get; set; }
    }
}
