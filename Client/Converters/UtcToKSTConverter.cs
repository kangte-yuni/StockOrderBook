using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Client.Converters
{
    public class UtcToKSTConverter : IValueConverter
    {
        private static readonly TimeZoneInfo KstZone = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime utc)
            {
                // UTC → KST 변환
                var kst = TimeZoneInfo.ConvertTimeFromUtc(utc, KstZone);
                return kst;
            }
            return value!;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
