using System;
using System.Collections.Generic;
using ScheduleInfo;

namespace AuditoriumBooking.SharedResources
{
    public enum Week
    {
        Both,
        Chislitel,
        Znamenatel
    }
    public class Resources
    {
        public Resources()
        {
            Auditoriums = new Dictionary<Guid, ScheduleItemInfo>();
            Applications = new Dictionary<Guid, ApplicationInfo>();
            Classrooms = new List<Classroom>();
            Periods = new List<Period>();
        }

        private DateTime _chosenDate;
        public DateTime ChosenDate
        {
            get { return _chosenDate; }
            set 
            {
                if (_chosenDate != value)
                {
                    _chosenDate = value;
                }
            }
        }


        private Dictionary<Guid, ScheduleItemInfo> _auditoriums;
        public Dictionary<Guid, ScheduleItemInfo> Auditoriums
        {
            get { return _auditoriums; }
            set
            {
                if (_auditoriums != value)
                {
                    _auditoriums = value;
                }
            }
        }

        private Dictionary<Guid, ApplicationInfo> _applications;

        public Dictionary<Guid, ApplicationInfo> Applications
        {
            get { return _applications; }
            set
            {
                if (_applications != value)
                {
                    _applications = value;
                }
            }
        }

        public List<Classroom> Classrooms;
        public Week FilterWeek;
        public List<Period> Periods;
    }
}
