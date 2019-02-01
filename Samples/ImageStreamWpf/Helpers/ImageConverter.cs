using System;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media.Imaging;
using System.Windows;
using System.ComponentModel;
using System.IO;
using System.Drawing.Imaging;
using System.Drawing;

namespace ImageStreamWpf.Helpers
{
  /// <summary>
  /// Binding helper which converts <see cref="BitmapImage"/> to <see cref="Uri"/>.
  /// </summary>
  public class ImageConverter : IValueConverter
  {
    /// <summary>
    ///  Converts <see cref="BitmapImage"/> to <see cref="Uri"/>.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="targetType"></param>
    /// <param name="parameter"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value == null)
      {
        return new BitmapImage();
      }

      using (MemoryStream memory = new MemoryStream())
      {
        (value as Bitmap).Save(memory, ImageFormat.Png);
        memory.Position = 0;
        BitmapImage bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = memory;
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        return bitmapImage;
      }

      //BitmapImage image = new BitmapImage();
      //image.BeginInit();
      //image.UriSource = new Uri(value as string);
      //image.EndInit();
      //return image;

      // return new BitmapImage(new Uri(value.ToString()));
    }

    /// <summary>
    /// Not implemented.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="targetType"></param>
    /// <param name="parameter"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}
