using System.Globalization;
using System.Windows.Data;
using SaveCodeClassfication.Models;

namespace SaveCodeClassfication
{
    /// <summary>
    /// 타입을 기반으로 JobGroupInfo인지 확인하는 컨버터
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