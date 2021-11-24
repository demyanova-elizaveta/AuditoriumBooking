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
    /// Логика взаимодействия для SecondDayScheduler.xaml
    /// </summary>
    public partial class SecondDayScheduler : UserControl
    {
        public SecondDayScheduler()
        {
            InitializeComponent();
            FIllForm();
        }

        private void FIllForm()
        {
            EventsGrid.ColumnDefinitions.Clear();
            EventsGrid.RowDefinitions.Clear();

            int number;
            var intClassrooms = (from intClassroom in XMLFileParser.Classrooms
                                 where (int.TryParse(intClassroom.shortName, out number) && (number / 100 == 4 || number / 100 == 3))
                                 || intClassroom.shortName.Contains("к/к")
                                 select intClassroom).ToList();

            for (int j = 0; j <= intClassrooms.Count; j++) //запись названий аудиторий в таблице аудиторий
            {
                var column = new ColumnDefinition() { Width = new GridLength(90) };
                EventsGrid.ColumnDefinitions.Add(column);

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

                Grid.SetColumn(label, j);
                Grid.SetRow(label, 0);

                EventsGrid.Children.Add(label);
            }

            for (int j = 1; j <= 23; j++) //запись названий пар в таблице
            {
                var label = new Label();

                label.Height = 30;
                label.Width = 90;

                label.Content = string.Format("{0}:00", j-1);

                Grid.SetColumn(label, 0);
                Grid.SetRow(label, j);

                EventsGrid.Children.Add(label);
            }
        }
    }
}
