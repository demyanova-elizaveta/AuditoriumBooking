using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleInfo
{
    [Serializable]
    public class ApplicationInfo: AuditoriumInfo, IAuditoriumInfo
    {
        public DateTime ParticularDate { get; set; }
        public bool IsWeeklyEvent { get; set; }
    }
}
