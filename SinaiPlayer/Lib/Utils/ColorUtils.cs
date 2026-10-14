using System.Runtime.InteropServices.WindowsRuntime;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SinaiPlayer.Lib.Utils;
using Windows.Graphics.Imaging; 
using Windows.UI;
using Windows.UI;

namespace SinaiPlayer.Lib.Helpers
{
    class ColorUtils
    {
        public static async Task<Color> GeneratePaletteAsync(byte[] bytes)
        {
            try
            {
                if (bytes == null || bytes.Length == 0)
                    return Colors.Transparent;

                using (var stream = await ThumbnailUtils.StreamFromBytesAsync(bytes))
                {
                    if (stream == null)
                        return Colors.Transparent;

                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                    SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync(
                        BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

                    if (softwareBitmap == null)
                        return Colors.Transparent;

                    int width = softwareBitmap.PixelWidth;
                    int height = softwareBitmap.PixelHeight;

                    if (width <= 0 || height <= 0)
                        return Colors.Transparent;

                    byte[] pixels = new byte[4 * width * height];

                    softwareBitmap.CopyToBuffer(pixels.AsBuffer());

                    long totalR = 0;
                    long totalG = 0;
                    long totalB = 0;
                    long totalWeight = 0;

                    int step = Math.Max(1, Math.Min(width, height) / 40);

                    for(int y = 0; y < height; y += step)
                    {
                        for(int x = 0; x < width; x += step)
                        {
                            int index = ((y * width) + x) * 4;

                            if (index + 3 >= pixels.Length)
                                continue;

                            byte b = pixels[index + 0];
                            byte g = pixels[index + 1];
                            byte r = pixels[index + 2];
                            byte a = pixels[index + 3];

                            if (a < 10)
                                continue;

                            int brightness = r + g + b;
                            if (brightness < 25)
                                continue;

                            int max = Math.Max(r, Math.Max(g, b));
                            int min = Math.Min(r, Math.Min(g, b));
                            int saturation = max - min;

                            int weight = Math.Max(1, saturation + (brightness / 8));

                            totalR += r * weight;
                            totalG += g * weight;
                            totalB += b * weight;
                            totalWeight += weight;
                        }
                    }

                    if(totalWeight == 0)
                    {
                        long r = 0;
                        long g = 0;
                        long b = 0;
                        long count = 0;

                        int fallbackStep = Math.Max(1, (width * height) / 2000);

                        for(int i = 0; i < pixels.Length; i += 4 * fallbackStep)
                        {
                            if (i + 3 >= pixels.Length)
                                continue;

                            b += pixels[i + 0];
                            g += pixels[i + 1];
                            r += pixels[i + 2];
                            count++;
                        }

                        if (count == 0)
                            return Colors.Transparent;

                        return Color.FromArgb(255,
                            (byte)(r / count),
                            (byte)(g / count),
                            (byte)(b / count));
                    }

                    return Color.FromArgb(255,
                        (byte)(totalR / totalWeight),
                        (byte)(totalG / totalWeight),
                        (byte)(totalB / totalWeight));
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("GeneratePaletteAsync failed: " + e);
                return Colors.Transparent;
            }
        }
    }
}
