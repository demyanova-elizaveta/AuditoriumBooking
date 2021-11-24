using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ScheduleInfo
{
    public class Period
    {
        public string Name { get; set; }
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
    }
}
