using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DataValidation
{
    public class TimeValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var rx = new Regex(@"^([0-1][0-9]|[2][0-3]):([0-5][0-9])$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (!string.IsNullOrWhiteSpace((value ?? "").ToString()) && !rx.IsMatch(value.ToString()))
                return new ValidationResult(false, "Неверный формат.");
            else
                return ValidationResult.ValidResult;
        }
    }
}
