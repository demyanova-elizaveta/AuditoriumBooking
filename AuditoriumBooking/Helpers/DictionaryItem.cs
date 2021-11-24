using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ScheduleInfo;

namespace AuditoriumBooking.Helpers
{
    [Serializable]
    public class ScheduleDictionaryItem
    {
        public Guid ID;
        public ScheduleItemInfo Value;
    }

    [Serializable]
    public class ApplicationDictionaryItem
    {
        public Guid ID;
        public ApplicationInfo Value;
    }
}
