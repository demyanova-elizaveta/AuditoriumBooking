using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ScheduleInfo
{
    public interface IAuditoriumInfo
    {
        string TeacherName { get; set; }
        string RoomNumber { get; set; }
        string SubjectName { get; set; }
        string Week { get; set; }
        string GroupName { get; set; }
        bool IsScheduled { get; set; }
        string Background { get; set; }
        TimeSpan Start { get; set; }
        TimeSpan End { get; set; }
        List<DateTime> CancelledDates { get; set; }
        List<KeyValuePair<DateTime, string>> SpecifiedColors { get; set; }
    }
}
