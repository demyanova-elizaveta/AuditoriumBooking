using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ScheduleInfo;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AuditoriumBooking.SharedResources;
using System.Windows.Data;
using System.IO;
using System.Drawing;
using AuditoriumBooking.Styles;
using System.Threading;
using System.Diagnostics;
using System.Timers;

namespace AuditoriumBooking.AuditoriumForm
{
    /// <summary>
    /// Interaction logic for EditingForm.xaml
    /// </summary>
    public partial class EditingForm : Window, INotifyPropertyChanged, IDataErrorInfo
    {
        object _sender;
        RoutedEventArgs _e;
        bool _changed = false;

        public EditingForm(object sender, RoutedEventArgs e)
        {
            InitializeComponent();

            _sender = sender;
            _e = e;

            //Set DataContext
            DataContext = this;
            freeClassroomsInfoTB.DataContext = this;
            classroomCB.DataContext = ResourcesProvider.Current.Classrooms;
            datePicker.DataContext = DatePickerDate;

            //Set calendar features
            CalendarDateRange cdr = new CalendarDateRange(DateTime.MinValue, DateTime.Today.AddDays(-1));
            datePicker.BlackoutDates.Add(cdr);
        }
        public IAuditoriumInfo EditingEvent { get; set; }

        public Guid EventGuid { get; set; }

        //Properties
        private DateTime _datePicker = ResourcesProvider.Current.ChosenDate;
        public DateTime DatePickerDate
        {
            get { return _datePicker; }
            set
            {
                if (_datePicker != value)
                {
                    _datePicker = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string _teacherName;
        public string TeacherName
        {
            get { return _teacherName; }
            set
            {
                if (_teacherName != value)
                {
                    _teacherName = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string _busyClassrooms;
        public string BusyClassrooms
        {
            get { return _busyClassrooms; }
            set
            {
                if (_busyClassrooms != value)
                {
                    _busyClassrooms = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string _subjectName;
        public string SubjectName
        {
            get { return _subjectName; }
            set
            {
                if (_subjectName != value)
                {
                    _subjectName = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string _groupName;
        public string GroupName
        {
            get { return _groupName; }
            set
            {
                if (_groupName != value)
                {
                    _groupName = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string _startTimeFormat;
        public string StartTimeFormat
        {
            get { return _startTimeFormat; }
            set
            {
                if (_startTimeFormat != value)
                {
                    _startTimeFormat = value;
                    NotifyPropertyChanged("StartTimeFormat");
                    NotifyPropertyChanged("EndTimeFormat");
                    SearchForFreeClassrooms();
                }
            }
        }

        private string _endTimeFormat;

        public string EndTimeFormat
        {
            get { return _endTimeFormat; }
            set
            {
                if (_endTimeFormat != value)
                {
                    _endTimeFormat = value;
                    NotifyPropertyChanged("StartTimeFormat");
                    NotifyPropertyChanged("EndTimeFormat");
                    SearchForFreeClassrooms();
                }
            }
        }

        //Create link or just comment after checking if there is busy classroom at chosen time
        private void SearchForFreeClassrooms()
        {
            if (IsInitialized)
            {
                mainClassroomsInfoTB.Inlines.Clear();
            }
            if (classroomCB.SelectedItem != null)
            {
                TimeSpan time1;
                TimeSpan time2;
                if (TimeSpan.TryParse(StartTimeFormat, out time1) && TimeSpan.TryParse(EndTimeFormat, out time2) && time1 < time2)
                {
                    var eventsWithBusyClassrooms = new Dictionary<Guid, IAuditoriumInfo>();
                    if (((ListBoxItem)RadioButtonGroupWeekChoice.SelectedItem).Content.ToString() == "Все недели")
                    {
                        var appropriateApplications = ResourcesProvider.Current.Applications.Where(currentEvent => currentEvent.Value.RoomNumber == ((Classroom)classroomCB.SelectedItem).fullName &&
                            ((isWeeklyChB.IsChecked.Value &&
                            ((!currentEvent.Value.IsWeeklyEvent && currentEvent.Value.ParticularDate >= DatePickerDate.Date && currentEvent.Value.ParticularDate.Subtract(DatePickerDate.Date).TotalHours % 7 * 24 == 0)
                            || (currentEvent.Value.IsWeeklyEvent && currentEvent.Value.ParticularDate.Subtract(DatePickerDate.Date).TotalHours % 7 * 24 == 0)))
                            ||
                            (!isWeeklyChB.IsChecked.Value &&
                            ((!currentEvent.Value.IsWeeklyEvent && currentEvent.Value.ParticularDate == DatePickerDate.Date)
                            || (currentEvent.Value.IsWeeklyEvent && !currentEvent.Value.CancelledDates.Contains(DatePickerDate.Date) && currentEvent.Value.ParticularDate <= DatePickerDate.Date && currentEvent.Value.ParticularDate.Subtract(DatePickerDate.Date).TotalHours % 7 * 24 == 0)))))
                            .Where(e =>
                                     (e.Value.Start <= time1 && e.Value.End >= time2) ||
                                     (e.Value.Start >= time1 && e.Value.End <= time2) ||
                                     (e.Value.Start <= time1 && e.Value.End <= time2 && e.Value.End > time1) ||
                                     (e.Value.Start >= time1 && e.Value.End >= time2 && e.Value.Start < time2)).ToDictionary(a => a.Key, a => a.Value);
                        var appropriateScheduleItems = ResourcesProvider.Current.Auditoriums.Where(currentEvent => (currentEvent.Value.RoomNumber == ((Classroom)(classroomCB.SelectedItem)).fullName) && (currentEvent.Value).Day == GetWeekDayOfChosenDay(DatePickerDate.DayOfWeek)
                        && !currentEvent.Value.CancelledDates.Contains(DatePickerDate.Date)).Where(e =>
                                      (e.Value.Start <= time1 && e.Value.End >= time2) ||
                                      (e.Value.Start >= time1 && e.Value.End <= time2) ||
                                      (e.Value.Start <= time1 && e.Value.End <= time2 && e.Value.End > time1) ||
                                      (e.Value.Start >= time1 && e.Value.End >= time2 && e.Value.Start < time2)).ToDictionary(a => a.Key, a => a.Value);
                        eventsWithBusyClassrooms = appropriateScheduleItems.ToDictionary(a => a.Key, a => a.Value as IAuditoriumInfo).Union(appropriateApplications.ToDictionary(a => a.Key, a => a.Value as IAuditoriumInfo)).ToDictionary(a => a.Key, a => a.Value);
                    }
                    else
                    {
                        var appropriateApplications = ResourcesProvider.Current.Applications.Where(currentEvent => currentEvent.Value.RoomNumber == ((Classroom)classroomCB.SelectedItem).fullName && ((isWeeklyChB.IsChecked.Value &&
                            ((!currentEvent.Value.IsWeeklyEvent && currentEvent.Value.ParticularDate >= DatePickerDate.Date && currentEvent.Value.ParticularDate.Subtract(DatePickerDate.Date).TotalHours % 7 * 24 == 0)
                            || (currentEvent.Value.IsWeeklyEvent && currentEvent.Value.ParticularDate.Subtract(DatePickerDate.Date).TotalHours % 7 * 24 == 0)))
                            ||
                            (!isWeeklyChB.IsChecked.Value &&
                            ((!currentEvent.Value.IsWeeklyEvent && currentEvent.Value.ParticularDate == DatePickerDate.Date)
                            || (currentEvent.Value.IsWeeklyEvent && !currentEvent.Value.CancelledDates.Contains(DatePickerDate.Date) && currentEvent.Value.ParticularDate <= DatePickerDate.Date && currentEvent.Value.ParticularDate.Subtract(DatePickerDate.Date).TotalHours % 7 * 24 == 0))))
                            && (currentEvent.Value.Week == "Все недели" || currentEvent.Value.Week == ((ListBoxItem)RadioButtonGroupWeekChoice.SelectedItem).Content.ToString())).Where(e =>
                                     (e.Value.Start <= time1 && e.Value.End >= time2) ||
                                     (e.Value.Start >= time1 && e.Value.End <= time2) ||
                                     (e.Value.Start <= time1 && e.Value.End <= time2 && e.Value.End > time1) ||
                                     (e.Value.Start >= time1 && e.Value.End >= time2 && e.Value.Start < time2)).ToDictionary(a => a.Key, a => a.Value);
                        var appropriateScheduleItems = ResourcesProvider.Current.Auditoriums.Where(currentEvent => (currentEvent.Value.RoomNumber == ((Classroom)(classroomCB.SelectedItem)).fullName) && (currentEvent.Value).Day == GetWeekDayOfChosenDay(DatePickerDate.DayOfWeek)
                        && !currentEvent.Value.CancelledDates.Contains(DatePickerDate.Date) && (currentEvent.Value.Week == "Все недели" || currentEvent.Value.Week == ((ListBoxItem)RadioButtonGroupWeekChoice.SelectedItem).Content.ToString())).Where(e =>
                                      (e.Value.Start <= time1 && e.Value.End >= time2) ||
                                      (e.Value.Start >= time1 && e.Value.End <= time2) ||
                                      (e.Value.Start <= time1 && e.Value.End <= time2 && e.Value.End > time1) ||
                                      (e.Value.Start >= time1 && e.Value.End >= time2 && e.Value.Start < time2)).ToDictionary(a => a.Key, a => a.Value);
                        eventsWithBusyClassrooms = appropriateScheduleItems.ToDictionary(a => a.Key, a => a.Value as IAuditoriumInfo).Union(appropriateApplications.ToDictionary(a => a.Key, a => a.Value as IAuditoriumInfo)).ToDictionary(a => a.Key, a => a.Value);
                    }

                    if (eventsWithBusyClassrooms.Count > 0)
                    {
                        hiddenPanelForBusyInfo.Visibility = Visibility.Visible;
                        var link = new Hyperlink() { Command = DrawerHost.OpenDrawerCommand, CommandParameter = Dock.Bottom, Foreground = (System.Windows.Media.Brush)(new BrushConverter()).ConvertFrom("#7F0000"), Cursor = Cursors.Hand, TextDecorations = null };
                        link.Inlines.Add("❗ Количество пересекающихся событий в выбранное время: " + eventsWithBusyClassrooms.Count);

                        mainClassroomsInfoTB.Inlines.Add(link);

                        var sb = new StringBuilder();
                        foreach (var currentEvent in eventsWithBusyClassrooms)
                        {
                            if (currentEvent.Value.IsScheduled)
                            {
                                sb.Append(string.Format("• {0}, «{1}», {2}, {3}, {4}, по расписанию \n", currentEvent.Value.TeacherName, currentEvent.Value.SubjectName, currentEvent.Value.GroupName, currentEvent.Value.Start + " — " + currentEvent.Value.End, currentEvent.Value.Week));
                            }
                            else
                            if (!((ApplicationInfo)currentEvent.Value).IsWeeklyEvent)
                            {
                                sb.Append(string.Format("• {0}, «{1}», {2}, {3}, {4}, запланированное одиночное на: {5} \n", currentEvent.Value.TeacherName, currentEvent.Value.SubjectName, currentEvent.Value.GroupName, currentEvent.Value.Start + " — " + currentEvent.Value.End, currentEvent.Value.Week, ((ApplicationInfo)currentEvent.Value).ParticularDate));
                            }
                            else
                            if (((ApplicationInfo)currentEvent.Value).IsWeeklyEvent)
                            {
                                sb.Append(string.Format("• {0}, «{1}», {2}, {3}, {4}, запланированное повторяющееся с: {5}, каждую неделю\n", currentEvent.Value.TeacherName, currentEvent.Value.SubjectName, currentEvent.Value.GroupName, currentEvent.Value.Start + " — " + currentEvent.Value.End, currentEvent.Value.Week, ((ApplicationInfo)currentEvent.Value).ParticularDate/*, endDate*/));
                            }
                        }

                        BusyClassrooms = sb.ToString();
                        addBtn.IsEnabled = false;
                    }
                    else
                    {
                        hiddenPanelForBusyInfo.Visibility = Visibility.Visible;
                        mainClassroomsInfoTB.Text = "✔ Нет пересекающихся событий в выбранное время";
                        BusyClassrooms = "";
                        mainClassroomsInfoTB.Foreground = (System.Windows.Media.Brush)(new BrushConverter()).ConvertFrom("#008632"); //green
                        addBtn.IsEnabled = true;
                    }
                }
                else
                {
                    hiddenPanelForBusyInfo.Visibility = Visibility.Hidden;
                    mainClassroomsInfoTB.Text = "";
                    BusyClassrooms = "";
                    addBtn.IsEnabled = true;
                }
            }
        }

        //Interface INotifyPropertyChanged requirement

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] string property = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        //Interface IDataErrorInfo requirement
        public string Error => string.Empty;

        public string this[string columnName]
        {
            get
            {
                return Validate(columnName);
            }
        }

        private string Validate(string propertyName)
        {
            string validationMessage = string.Empty;
            switch (propertyName)
            {
                case ("StartTimeFormat"):
                    {
                        if (TimeSpan.TryParse(StartTimeFormat, out TimeSpan time1) && TimeSpan.TryParse(EndTimeFormat, out TimeSpan time2))
                            if (time1 >= time2)
                                validationMessage = "Время окончания не может быть больше времени начала.";
                        break;
                    }
                case ("EndTimeFormat"):
                    {
                        if (TimeSpan.TryParse(StartTimeFormat, out TimeSpan time1) && TimeSpan.TryParse(EndTimeFormat, out TimeSpan time2))
                            if (time1 >= time2)
                                validationMessage = "Время окончания не может быть больше времени начала.";
                        break;
                    }
            }

            return validationMessage;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            TimeSpan isCorrectStartTime;
            TimeSpan isCorrectEndTime;
            if (string.IsNullOrWhiteSpace((teacherNameTB.Text ?? "").ToString()) ||
                string.IsNullOrWhiteSpace((subjectNameTB.Text ?? "").ToString()) ||
                string.IsNullOrWhiteSpace((groupNameTB.Text ?? "").ToString()) ||
                classroomCB.SelectedItem == null ||
                !TimeSpan.TryParse(startTimeTB.Text, out isCorrectStartTime) ||
                !TimeSpan.TryParse(endTimeTB.Text, out isCorrectEndTime))
            {
                MessageBox.Show("Пожалуйста, введите все запрашиваемые данные корректно!");
            }
            else
            {
                DeletePreviousEvent();

                this.IsEnabled = true;

                var teacherName = teacherNameTB.Text;
                var subjectName = subjectNameTB.Text;
                var groupName = groupNameTB.Text;
                var className = ((Classroom)(classroomCB.SelectedItem)).fullName;
                var startTime = isCorrectStartTime;
                var endTime = isCorrectEndTime;
                var date = DatePickerDate;
                var week = ((ListBoxItem)RadioButtonGroupWeekChoice.SelectedItem).Content.ToString();

                var guid = EventGuid;
                ResourcesProvider.Current.Applications.Add(guid, new ApplicationInfo()
                {
                    TeacherName = teacherName,
                    SubjectName = subjectName,
                    GroupName = groupName,
                    RoomNumber = className,
                    End = endTime,
                    Start = startTime,
                    ParticularDate = date,
                    Week = week,
                    IsScheduled = false,
                    CancelledDates = new List<DateTime>(),
                    IsWeeklyEvent = isWeeklyChB.IsChecked.Value,
                    Background = EditingEvent.Background,
                    SpecifiedColors = EditingEvent.SpecifiedColors
                });
                ResourcesProvider.Current.Applications = new Dictionary<Guid, ApplicationInfo>(ResourcesProvider.Current.Applications);

                var myAddingMessage = new SnackbarMessageQueue(TimeSpan.FromMilliseconds(500));
                Snackbar.MessageQueue = myAddingMessage;
                Snackbar.MessageQueue.Enqueue("Добавление изменений...");
                Snackbar.MessageQueue.Enqueue("Изменение внесено!", "Отменить", () => UndoAdding(guid));


                if ((Application.Current.MainWindow as DayScheduler.DayScheduler).IsInitialized)
                {
                    (Application.Current.MainWindow as DayScheduler.DayScheduler).GenerateWindowBody();
                }

                SearchForFreeClassrooms();

                //System.Timers.Timer t = new System.Timers.Timer();
                //t.Interval = 2500;
                //t.Elapsed += new ElapsedEventHandler(t_Elapsed);
                //t.Start();
            }
        }

        //private void t_Elapsed(object sender, ElapsedEventArgs e)
        //{
        //    this.Dispatcher.Invoke(new Action(() =>
        //    {
        //        this.Close();
        //    }), null);
        //}

        private void DeletePreviousEvent()
        {
            this.IsEnabled = false;
            if ((Application.Current.MainWindow as DayScheduler.DayScheduler).IsInitialized)
            {
                var wnd = Application.Current.MainWindow as DayScheduler.DayScheduler;
                wnd.deleteEditedEventBtn_Click(_sender, _e);
            }
        }

        private void UndoAdding(Guid guid)
        {
            ResourcesProvider.Current.Applications.Remove(guid);
            ResourcesProvider.Current.Applications = new Dictionary<Guid, ApplicationInfo>(ResourcesProvider.Current.Applications);
            if ((Application.Current.MainWindow as DayScheduler.DayScheduler).IsInitialized)
            {
                (Application.Current.MainWindow as DayScheduler.DayScheduler).GenerateWindowBody();
            }

            SearchForFreeClassrooms();
        }

        private string GetWeekDayOfChosenDay(DayOfWeek dayOfWeekEng)
        {
            string dayOfWeek;
            switch (dayOfWeekEng)
            {
                case DayOfWeek.Monday:
                    dayOfWeek = "Понедельник";
                    break;
                case DayOfWeek.Tuesday:
                    dayOfWeek = "Вторник";
                    break;
                case DayOfWeek.Wednesday:
                    dayOfWeek = "Среда";
                    break;
                case DayOfWeek.Thursday:
                    dayOfWeek = "Четверг";
                    break;
                case DayOfWeek.Friday:
                    dayOfWeek = "Пятница";
                    break;
                case DayOfWeek.Saturday:
                    dayOfWeek = "Суббота";
                    break;
                case DayOfWeek.Sunday:
                    dayOfWeek = "Воскресенье";
                    break;
                default:
                    dayOfWeek = "Каждый день";
                    break;
            }
            return dayOfWeek;
        }

        //XAML elements handlers(обработчики действий)

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void startTimeTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StartTimeFormat = ((TextBlock)((ListBoxItem)((ListBox)sender).SelectedItem).Content).Text;
        }

        private void endTimeTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EndTimeFormat = ((TextBlock)((ListBoxItem)((ListBox)sender).SelectedItem).Content).Text;
        }

        private void ClassroomCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SearchForFreeClassrooms();
        }

        private void RadioButtonGroupWeekChoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SearchForFreeClassrooms();
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            DatePickerDate = datePicker.SelectedDate.Value.Date;
            SearchForFreeClassrooms();
        }

        private void isWeeklyChB_Click(object sender, RoutedEventArgs e)
        {
            SearchForFreeClassrooms();
        }
    }
}
