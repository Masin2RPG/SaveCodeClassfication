using System.Globalization;
using System.Windows.Data;
using SaveCodeClassfication.Models;

namespace SaveCodeClassfication
{
    /// <summary>
    /// Ÿ���� ������� JobGroupInfo���� Ȯ���ϴ� ������
    /// </summary>
    public class TypeToBooleanConverter : IValueConverter
    {
        public static readonly TypeToBooleanConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is JobGroupInfo;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}