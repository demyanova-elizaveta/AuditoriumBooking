using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleInfo
{
    [Serializable]
    public class ScheduleItemInfo: AuditoriumInfo, IAuditoriumInfo
    {
        public string Day { get; set; }
    }
}
