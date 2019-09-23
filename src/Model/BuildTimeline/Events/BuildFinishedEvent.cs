using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.BuildTimeline
{
    class BuildFinishedEvent : Event
    {
        // whether the build finished successfully
        public bool Succeeded { get; set; }
    }
}
