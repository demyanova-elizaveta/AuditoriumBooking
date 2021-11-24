using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using ScheduleInfo;

namespace AuditoriumBooking.Helpers
{
    public class TypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var item = parameter as IAuditoriumInfo;
            var type = item as ApplicationInfo;
            if (type != null && type.IsWeeklyEvent || ((ScheduleItemInfo)item).IsScheduled)
            {
                return true;
            }
            
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}
