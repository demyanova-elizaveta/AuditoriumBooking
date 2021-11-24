using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;


namespace ScheduleInfo
{
    public static class XMLFileParser
    {
        public static void GetInfo(string filePath, ref List<Classroom> currentClassrooms, ref Dictionary<Guid, ScheduleItemInfo> currentAuditoriums, ref List<Period> currentPeriods)
        {
            var doc = new XmlDocument();

            // Create an exemplar of Xml document.
            XDocument xdoc = XDocument.Load(filePath);

            if (xdoc != null)
            {
                var periods = (from period in xdoc.Root.Element("periods").Elements("period")
                               select new Period
                               {
                                   Name = period.Attribute("name").Value.ToString(),
                                   Start = TimeSpan.Parse(period.Attribute("starttime").Value.ToString()),
                                   End = TimeSpan.Parse(period.Attribute("endtime").Value.ToString())
                               }).ToList();
                currentPeriods = periods;
                var days = (from day in xdoc.Root.Element("daysdefs").Elements("daysdef")
                            select new
                            {
                                ID = day.Attribute("id").Value.ToString(),
                                name = day.Attribute("name").Value.ToString(),
                                days = day.Attribute("days").Value.ToString()
                            }).ToList();
                var weeks = (from week in xdoc.Root.Element("weeksdefs").Elements("weeksdef")
                             select new
                             {
                                 ID = week.Attribute("id").Value.ToString(),
                                 name = week.Attribute("name").Value.ToString(),
                                 weeks = week.Attribute("weeks").Value.ToString()
                             }).ToList();
                var terms = (from term in xdoc.Root.Element("termsdefs").Elements("termsdef")
                             select new
                             {
                                 ID = term.Attribute("id").Value.ToString(),
                                 name = term.Attribute("name").Value.ToString(),
                                 terms = term.Attribute("terms").Value.ToString()
                             }).ToList();
                var subjects = (from subject in xdoc.Root.Element("subjects").Elements("subject")
                                select new
                                {
                                    ID = subject.Attribute("id").Value.ToString(),
                                    name = subject.Attribute("name").Value.ToString()
                                }).ToList();
                var teachers = (from teacher in xdoc.Root.Element("teachers").Elements("teacher")
                                select new
                                {
                                    ID = teacher.Attribute("id").Value.ToString(),
                                    fullName = teacher.Attribute("name").Value.ToString(),
                                    shortName = teacher.Attribute("short").Value.ToString()
                                }).ToList();
                var classrooms = (from classroom in xdoc.Root.Element("classrooms").Elements("classroom")
                                  select new Classroom
                                  {
                                      ID = classroom.Attribute("id").Value.ToString(),
                                      fullName = classroom.Attribute("name").Value.ToString()
                                  }).ToList();
                currentClassrooms = classrooms;

                var classes = (from studentClass in xdoc.Root.Element("classes").Elements("class")
                               select new
                               {
                                   ID = studentClass.Attribute("id").Value.ToString(),
                                   fullName = studentClass.Attribute("name").Value.ToString(),
                                   shortName = studentClass.Attribute("short").Value.ToString()
                               }).ToList();
                var groups = (from studentGroup in xdoc.Root.Element("groups").Elements("group")
                              select new
                              {
                                  ID = studentGroup.Attribute("id").Value.ToString(),
                                  name = studentGroup.Attribute("name").Value.ToString(),
                                  classID = studentGroup.Attribute("classid").Value.ToString()
                              }).ToList();
                var lessons = (from lesson in xdoc.Root.Element("lessons").Elements("lesson")
                               select new
                               {
                                   ID = lesson.Attribute("id").Value.ToString(),
                                   classIDs = lesson.Attribute("classids").Value.ToString(),
                                   subjectID = lesson.Attribute("subjectid").Value.ToString(),
                                   teacherIDs = lesson.Attribute("teacherids").Value.ToString(),
                                   groupIDs = lesson.Attribute("groupids").Value.ToString()
                               }).ToList();
                var cards = (from card in xdoc.Root.Element("cards").Elements("card")
                             select new
                             {
                                 lessonID = card.Attribute("lessonid").Value.ToString(),
                                 classroomIDs = card.Attribute("classroomids").Value.ToString(),
                                 period = card.Attribute("period").Value.ToString(),
                                 weeks = card.Attribute("weeks").Value.ToString(),
                                 terms = card.Attribute("terms").Value.ToString(),
                                 days = card.Attribute("days").Value.ToString()
                             }).ToList();

                foreach (var card in cards)
                {
                    var subjectName = (from lesson in (from lesson in lessons where card.lessonID == lesson.ID select lesson)
                                       from subject in subjects
                                       where lesson.subjectID == subject.ID
                                       select subject.name).FirstOrDefault();

                    var teacherName = (from lesson in (from lesson in lessons where card.lessonID == lesson.ID select lesson)
                                       from teacher in teachers
                                       where lesson.teacherIDs == teacher.ID
                                       select teacher.fullName).FirstOrDefault();

                    var groupsArray = (from lesson in lessons where card.lessonID == lesson.ID select lesson.groupIDs).FirstOrDefault().Split(',');//divided IDs of groups 
                    var sb = new StringBuilder();
                    var selectedGroups = (from group1 in groupsArray from group2 in groups where group1 == group2.ID select group2);//select groups which match selected IDs
                    var classNames = (from group1 in selectedGroups from class1 in classes where group1.classID == class1.ID select class1.fullName + " " + group1.name);
                    foreach (var str in classNames)
                    {
                        sb.Append(str);
                    }

                    var classroomArray = card.classroomIDs.Split(',');

                    var classroomNums = (from classroom1 in classrooms
                                         from classroom2 in classroomArray
                                         where classroom2 == classroom1.ID
                                         select classroom1.fullName).ToList(); //numOfRoom - capacityOfRoom

                    var start = (from period in periods where card.period == period.Name select period.Start).FirstOrDefault();

                    var end = (from period in periods where card.period == period.Name select period.End).FirstOrDefault();

                    var numOfLesson = (from period in periods where card.period == period.Name select period.Name).FirstOrDefault();

                    var week = (from curWeek in weeks where card.weeks == curWeek.weeks select curWeek.name).FirstOrDefault();

                    var day = (from curDay in days where card.days == curDay.days select curDay.name).FirstOrDefault();

                    foreach (var classroom in classroomNums)
                    {
                        currentAuditoriums.Add(Guid.NewGuid(), new ScheduleItemInfo
                        {
                            RoomNumber = classroom,
                            SubjectName = subjectName,
                            TeacherName = teacherName,
                            GroupName = sb.ToString(),
                            Week = week,
                            Day = day,
                            Start = start,
                            End = end,
                            IsScheduled = true,
                            CancelledDates = new List<DateTime>(),
                            Background = "#FF327CAD",
                            SpecifiedColors = new List<KeyValuePair<DateTime, string>>()
                        });
                    }
                }
            }
            else
                throw new Exception("Неверно указан путь к файлу!");
        }
    }
}
