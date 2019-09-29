using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.BuildTimeline
{
    public class EventContext
    {
        // the ID of this context
        public int ContextId { get; set; }

        // the ID of the node where this context lives in
        public int NodeId { get; set; }
    }
}
