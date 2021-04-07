using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSJob
{
    public class WorkerConfig
    {
        public int MultiThread { get; set; }
        public int CmdTimeout { get; set; }
        public int ScheduleTime { get; set; }
    }
}
