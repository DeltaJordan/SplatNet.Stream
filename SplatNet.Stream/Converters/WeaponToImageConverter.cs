using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace SplatNet.Stream.Converters
{
	public class WeaponToImageConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			BitmapImage bi;
			if (Uri.TryCreate(value?.ToString(), UriKind.Absolute, out Uri uri))
			{
				bi = new BitmapImage(uri)
				{
					DecodePixelWidth = 32
				};
			}
			else
			{
				bi = new BitmapImage();
			}
			return bi;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
