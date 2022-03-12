using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace RenamingTool.Converters
{
    internal class StringToImageConverter : IValueConverter
    {

        /// <summary>
        /// GUID of the component registration group for WIC decoders
        /// </summary>
        private const string WICDecoderCategory = "{7ED96837-96F0-4812-B211-F13C24117ED3}";

        /// <summary>
        /// Represents information about a WIC decoder
        /// </summary>
        public struct DecoderInfo
        {
            public string FriendlyName;
            public string FileExtensions;
        }

        /// <summary>
        /// Gets a list of additionally registered WIC decoders
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<DecoderInfo> GetAdditionalDecoders()
        {
            var result = new List<DecoderInfo>();

            string baseKeyPath;

            // If we are a 32 bit process running on a 64 bit operating system, 
            // we find our config in Wow6432Node subkey
            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
            {
                baseKeyPath = "Wow6432Node\\CLSID";
            }
            else
            {
                baseKeyPath = "CLSID";
            }

            RegistryKey baseKey = Registry.ClassesRoot.OpenSubKey(baseKeyPath, false);
            if (baseKey != null)
            {
                var categoryKey = baseKey.OpenSubKey(WICDecoderCategory + "\\instance", false);
                if (categoryKey != null)
                {
                    // Read the guids of the registered decoders
                    var codecGuids = categoryKey.GetSubKeyNames();

                    foreach (var codecGuid in codecGuids)
                    {
                        // Read the properties of the single registered decoder
                        var codecKey = baseKey.OpenSubKey(codecGuid);
                        if (codecKey != null)
                        {
                            DecoderInfo decoderInfo = new DecoderInfo();
                            decoderInfo.FriendlyName = System.Convert.ToString(codecKey.GetValue("FriendlyName", ""));
                            decoderInfo.FileExtensions = System.Convert.ToString(codecKey.GetValue("FileExtensions", ""));
                            result.Add(decoderInfo);
                        }
                    }
                }
            }

            return result;
        }

        private static HashSet<string> SupportedExtensions;

        private static void CreateSupportedExtensions()
        {
            SupportedExtensions = new();

            // https://docs.microsoft.com/ja-jp/windows/win32/wic/-wic-about-windows-imaging-codec?redirectedfrom=MSDN
            const string nativeSupportExtensions =
                ".png,.jpg,.jpeg,.jpe,.jfif,.exif,.HEIC,.bmp,.dib,.rle,.tiff,.tif,.gif,.ico,.hdp,.wdp,.jxr,.dds";

            foreach (var extension in nativeSupportExtensions.Split(",", StringSplitOptions.RemoveEmptyEntries))
            {
                SupportedExtensions.Add(extension.ToUpperInvariant());
            }

            // https://stackoverflow.com/questions/36390013/get-supported-image-formats-from-bitmapimage/36391517#36391517
            foreach (var decoderInfo in GetAdditionalDecoders())
            {
                foreach (var extension in decoderInfo.FileExtensions.Split(",", StringSplitOptions.RemoveEmptyEntries))
                {
                    SupportedExtensions.Add(extension.ToUpperInvariant());
                }
            }
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var filePath = value as string;
            if (filePath == null)
                return null;

            if (SupportedExtensions == null)
                CreateSupportedExtensions();

            var extension = Path.GetExtension(filePath).ToUpperInvariant();
            if (!SupportedExtensions.Contains(extension))
                return null;

            try
            {
                var height = 96;
                if (int.TryParse(parameter as string, out var result))
                {
                    height = result;
                }

                var bmpImage = new BitmapImage();
                using var stream = File.OpenRead(filePath);
                bmpImage.BeginInit();
                bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                bmpImage.DecodePixelHeight = height;
                bmpImage.StreamSource = stream;
                bmpImage.EndInit();
                stream.Close();
                return bmpImage;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
