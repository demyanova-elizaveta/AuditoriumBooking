using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ScheduleInfo;

namespace AuditoriumScheduler.DayScheduler
{
    /// <summary>
    /// Логика взаимодействия для FirstDayScheduler.xaml
    /// </summary>
    public partial class FirstDayScheduler : UserControl
    {
        public FirstDayScheduler()
        {
            InitializeComponent();

            XMLFileParser.GetInfo(@"C:\Users\user\Desktop\Для курсовой(копия)\расписание.xml");
            DataContext = XMLFileParser.Auditoriums;

            int number;
            Classrooms = (from intClassroom in XMLFileParser.Classrooms
                          where (int.TryParse(intClassroom.shortName, out number) && (number / 100 == 4 || number / 100 == 3))
                          || intClassroom.shortName.Contains("к/к")
                          select intClassroom).ToList();

            classesTitles.MouseWheel += new MouseWheelEventHandler(DisableMouseWheel);
            timesTitles.MouseWheel += new MouseWheelEventHandler(DisableMouseWheel);

            GenerateWindowBody();
            DateButtonsSet();
            TimesTitlesSet();
            ClassesTitlesSet();
        }

        private static string fullPath = AppDomain.CurrentDomain.BaseDirectory + "Library";

        private List<Classroom> Classrooms { get; set; }

        public DateTime CurrentDate { get; set; } = DateTime.Today;

        List<DateTime> DatesDictionary { set; get; } = new List<DateTime>();

        private DateTime CurrentDayForCalendar { set; get; } = DateTime.Today;

        public void DateButtonsSet()
        {
            for (int i = 0; i < 31; i++)
            {
                DatesDictionary.Add(CurrentDayForCalendar);
                CurrentDayForCalendar = CurrentDayForCalendar.AddDays(1);
            }
            dates.ItemsSource = DatesDictionary;
        }

        public void TimesTitlesSet()
        {
            timesTitles.Children.Clear();
            timesTitles.RowDefinitions.Clear();
            timesTitles.ColumnDefinitions.Clear();

            timesTitles.ShowGridLines = true;

            for (int i = 0; i < 24 - 1; i++) //установка 24х столбцов
            {
                var column = new ColumnDefinition() { Width = new GridLength(70) };
                timesTitles.ColumnDefinitions.Add(column);
            }

            var lastColumn = new ColumnDefinition() { Width = new GridLength(70 + 90) }; //костыль для того, чтобы последний столбец был предусмотрен под ещё 90 пикселей - это размер скролла
            timesTitles.ColumnDefinitions.Add(lastColumn);

            for (int j = 1; j <= 24; j++) //запись названий пар в таблице
            {
                var label = new Label() { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };

                label.Height = 40;
                label.Width = 70;

                label.Content = string.Format("{0}", j - 1);

                Grid.SetColumn(label, j - 1);
                Grid.SetRow(label, 0);

                timesTitles.Children.Add(label);
            }
        }

        public void ClassesTitlesSet()
        {
            classesTitles.Children.Clear();
            classesTitles.RowDefinitions.Clear();
            classesTitles.ColumnDefinitions.Clear();

            classesTitles.ShowGridLines = true;

            for (int i = 0; i < Classrooms.Count - 1; i++)
            {
                var row = new RowDefinition() { Height = new GridLength(40) };
                classesTitles.RowDefinitions.Add(row);
            }

            var lastRow = new RowDefinition() { Height = new GridLength(40 + 90) }; //костыль для того, чтобы последний столбец был предусмотрен под ещё 90 пикселей - это размер скролла
            classesTitles.RowDefinitions.Add(lastRow);

            for (int j = 1; j <= Classrooms.Count; j++) //запись названий аудиторий в таблице аудиторий
            {
                var label = new Label() { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };

                label.Height = 40;
                label.Width = 90;

                if (j == 0)
                {
                    label.Content = "";
                }
                else
                {
                    label.Content = string.Format("{0}", Classrooms[j - 1].fullName);
                }

                Grid.SetColumn(label, 0);
                Grid.SetRow(label, j - 1);

                classesTitles.Children.Add(label);
            }
        }

        private void DisableMouseWheel(object sender, MouseEventArgs e)
        {
            MouseWheelEventArgs h = (MouseWheelEventArgs)e;
            h.Handled = true;
        }

        public void GenerateWindowBody()
        {
            eventsTable.Children.Clear();
            gridBookingTable.Children.Clear();
            gridBookingTable.ColumnDefinitions.Clear();
            gridBookingTable.RowDefinitions.Clear();

            for (int i = 0; i < 24; i++)
            {
                var column = new ColumnDefinition() { Width = new GridLength(70) };
                gridBookingTable.ColumnDefinitions.Add(column);
            }

            for (int i = 0; i < Classrooms.Count; i++)
            {
                var row = new RowDefinition() { Height = new GridLength(40) };
                gridBookingTable.RowDefinitions.Add(row);
            }

            var dayOfWeekEng = this.CurrentDate.DayOfWeek;
            string dayOfWeek = "";

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

            for (int j = 0; j < Classrooms.Count; j++)
            {
                var localApps = XMLFileParser.Auditoriums.Where(a => a.Day == dayOfWeek && a.RoomNumber == Classrooms[j].shortName && a.Week == "Знаменатель").ToList(); //fullName || shortname???

                // Выбираем заявки или пары по каждой из 6 пар 
                //var applications = localApps.Where(a => a.numberOfLesson == lessonNumber.ToString()).ToList();

                //int i = 0;

                // Если есть записи с такой парой
                if (localApps.Count > 0)
                {
                    // Проверяем каждую аудитрию
                    foreach (var todayEvent in localApps)
                    {
                        double oneHourWidth = 70;

                        //var concurrentEvents = localApps.Where(e => ((e.Start <= todayEvent.Start && e.End > todayEvent.Start) ||
                        //(e.Start > todayEvent.Start && e.Start < todayEvent.End))).OrderBy(ev => ev.Start); //не учитывает события на весь день

                        double width = oneHourWidth * (todayEvent.End.Subtract(todayEvent.Start).Hours + todayEvent.End.Subtract(todayEvent.Start).Minutes / 60.0);
                        double margLeft = oneHourWidth * (todayEvent.Start.Hours + (todayEvent.Start.Minutes / 60.0));

                        double height = 40;

                        double margTop = j * 40;

                        var label = new Label()
                        {
                            Margin = new Thickness(margLeft, margTop, 0, 0),
                            Height = height,
                            Width = width,
                            Background = Brushes.Red,
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(1),
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top
                        };

                        eventsTable.Children.Add(label);
                    }
                }
            }
        }

        private void SchedulerScrolViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            HeaderHoursScrolViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
            HeaderClassesScrolViewer.ScrollToVerticalOffset(e.VerticalOffset);
        }

        public void GenerateWindowBody1() //запасной
        {

            gridBookingTable.ColumnDefinitions.Clear();
            gridBookingTable.RowDefinitions.Clear();

            gridBookingTable.ShowGridLines = true;

            int number;
            var intClassrooms = (from intClassroom in XMLFileParser.Classrooms
                                 where (int.TryParse(intClassroom.shortName, out number) && (number / 100 == 4 || number / 100 == 3))
                                 || intClassroom.shortName.Contains("к/к")
                                 select intClassroom).ToList();

            for (int j = 0; j <= intClassrooms.Count; j++) //запись названий аудиторий в таблице аудиторий
            {
                var row = new RowDefinition() { Height = new GridLength(90) };
                gridBookingTable.RowDefinitions.Add(row);

                var label = new Label();

                label.Height = 30;
                label.Width = 90;

                if (j == 0)
                {
                    label.Content = "";
                }
                else
                {
                    label.Content = string.Format("{0}", intClassrooms[j - 1].fullName);
                }

                Grid.SetColumn(label, 0);
                Grid.SetRow(label, j);

                gridBookingTable.Children.Add(label);
            }

            var similarDayClasses = new List<AuditoriumInfo>();

            var dayOfWeekEng = this.CurrentDate.DayOfWeek;
            string dayOfWeek = "";

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

            var localApps = XMLFileParser.Auditoriums.Where(a => a.Day == dayOfWeek).ToList();

            var nullColumn = new ColumnDefinition() { Width = new GridLength(90) };
            gridBookingTable.ColumnDefinitions.Add(nullColumn);

            // Всего у нас 6 рядов (6 пар)
            for (int lessonNumber = 1; lessonNumber <= 6; lessonNumber++)
            {
                var column = new ColumnDefinition() { Width = new GridLength(90) };
                gridBookingTable.ColumnDefinitions.Add(column);

                // Выбираем заявки или пары по каждой из 8 пар 
                var applications = localApps.Where(a => a.NumberOfLesson == lessonNumber.ToString()).ToList();

                int i = 0;

                // Если есть записи с такой парой
                if (applications.Count > 0)
                {
                    // Проверяем каждую аудитрию
                    foreach (var classroom in intClassrooms)
                    {
                        var app = applications.FirstOrDefault(a => a.RoomNumber == classroom.shortName);

                        // Если есть заявка на текщую итерируемую аудиторию
                        if (app != null)
                        {
                            var btn = new Button()
                            {
                                Width = 90,
                                Height = 90,
                                Tag = $"{classroom.fullName}||{lessonNumber}",
                                VerticalAlignment = VerticalAlignment.Stretch,
                                // Уставнавливаем bg в зависимости от статуса и количества свободных мест
                                Background = GetBackgroundAccordingToStatus(app),
                            };

                            // Подписали метод на событие кнопки
                            //btn.Click += BtnClickCallback;


                            var label = new Label()
                            {
                                Foreground = new SolidColorBrush(Colors.White),
                                Margin = new Thickness(-20, -5, 0, 0),
                                FontSize = 15,
                            };

                            Grid.SetColumn(btn, lessonNumber);
                            Grid.SetRow(btn, i + 1);

                            gridBookingTable.Children.Add(btn);
                            //gridBookingTable.Children.Add(label);
                        }
                        else
                            SetEmptyButton(classroom.fullName, lessonNumber, i);
                        i++;
                    }
                }
                else // Если нет записей - ряд из пустых кнопкок
                    foreach (var classroom in intClassrooms)
                    {
                        SetEmptyButton(classroom.fullName, lessonNumber, i);
                        i++;
                    }
            }

        }

        public Brush GetBackgroundAccordingToStatus(AuditoriumInfo app)
        {
            Color color;

            //switch (app.Status.Type)
            //{
            //    case "Sheduled": color = Color.FromRgb(98, 0, 234); break; // purple
            //    case "Accepted": color = Color.FromRgb(100, 221, 23); break; // green
            //    case "InProgress": color = Color.FromRgb(255, 171, 0); break; // yellow

            //    default: color = Colors.White; break;
            //}
            color = Colors.PaleVioletRed; // red

            return new SolidColorBrush(color);
        }

        public void SetEmptyButton(string classroomNumber, int lessonNumber, int i)
        {
            var btn = new Button()
            {
                Width = 70,
                Height = 40,
                VerticalAlignment = VerticalAlignment.Stretch,
                Tag = $"{classroomNumber}||{lessonNumber}||none",
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
            };
            var label = new Label() { Content = "", Margin = new Thickness(-20, -5, 0, 0), FontSize = 15, };

            //btn.Click += BtnClickCallback;

            Grid.SetColumn(btn, lessonNumber);
            Grid.SetRow(btn, i + 1);

            gridBookingTable.Children.Add(btn);
        }

        private void DateChanged(object sender, RoutedEventArgs e)
        {
            // get date from button content property
            var dateStr = (sender as Button).Content.ToString();

            // set datetime variable from a string
            var newDate = DateTime.Parse(dateStr);
            this.CurrentDate = newDate;

            // update applications grid
            GenerateWindowBody();
        }
    }
}
