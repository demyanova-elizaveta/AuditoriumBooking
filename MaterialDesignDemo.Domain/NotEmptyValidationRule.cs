using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace MaterialDesignDemo.Domain
{
    public class NotEmptyValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var rx = new Regex(@"^([0-1][0-9]|[2][0-3]):([0-5][0-9])$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (string.IsNullOrWhiteSpace((value ?? "").ToString()))
                return new ValidationResult(false, "Заполните поле.");
            else
            if (rx.IsMatch(value.ToString()))
                return new ValidationResult(false, "Неверный формат.");
            else
                return ValidationResult.ValidResult;

            //return string.IsNullOrWhiteSpace((value ?? "").ToString())
            //    ? new ValidationResult(false, "Заполните поле")
            //    : ValidationResult.ValidResult;
        }
    }
}
