using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeetScheduler
{
    public class Participant
    {
        public string Name { get; set; }
        public List<int> EventIds { get; set; } // List of events the participant is part of
    }
}
