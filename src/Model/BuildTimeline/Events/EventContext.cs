using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.BuildTimeline
{
    class EventContext
    {
        // the ID of this context
        public int ContextId { get; set; }

        // the ID of the node where this context was created
        public int NodeId { get; set; }
    }
}
