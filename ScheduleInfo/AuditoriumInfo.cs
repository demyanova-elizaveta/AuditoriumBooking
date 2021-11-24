using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Serialization;

namespace ScheduleInfo
{
    [Serializable]
    public abstract class AuditoriumInfo: IAuditoriumInfo
    {
        public string TeacherName { get; set; }
        public string RoomNumber { get; set; }
        public string SubjectName { get; set; }
        public string Week { get; set; }
        public string GroupName { get; set; }
        public bool IsScheduled { get; set; }
        public string Background { get; set; }
        public List<ScheduleInfo.KeyValuePair<DateTime, string>> SpecifiedColors { get; set; }

        [XmlIgnore]
        public TimeSpan Start { get; set; }
        // Pretend property for serialization
        [XmlElement("StartTime")]
        public long StartTimeTicks
        {
            get { return Start.Ticks; }
            set { Start = new TimeSpan(value); }
        }

        [XmlIgnore]
        public TimeSpan End { get; set; }
        // Pretend property for serialization
        [XmlElement("EndTime")]
        public long EndTimeTicks
        {
            get { return End.Ticks; }
            set { End = new TimeSpan(value); }
        }

        [XmlIgnore]
        public List<DateTime> CancelledDates
        {
            get { return Days.Select(item => DateTime.Parse(item)).ToList(); }
            set { Days = value.Select(item => item.ToString("yyyy-MM-dd")).ToList(); }
        }

        // Pretend property for serialization
        [XmlArray("CancelledDays")]
        [XmlArrayItem("Day")]
        public List<string> Days { get; set; }
    }
}
