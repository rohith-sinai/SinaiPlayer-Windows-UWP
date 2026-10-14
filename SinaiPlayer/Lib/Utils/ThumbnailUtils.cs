using Windows.UI.Xaml.Media.Imaging;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage.Streams; 

namespace SinaiPlayer.Lib.Utils
{
    public static class ThumbnailUtils
    {
        public static async Task<byte[]> ReadBytesAsync(IRandomAccessStream stream)
        {
            if (stream == null)
                return null;

            stream.Seek(0);

            byte[] bytes = new byte[stream.Size];

            using (DataReader reader = new DataReader(stream.GetInputStreamAt(0)))
            {
                await reader.LoadAsync((uint)stream.Size);
                reader.ReadBytes(bytes);
            }

            return bytes;
        }

        public static async Task<BitmapImage> BitmapFromBytesAsync(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;

            BitmapImage bitmap = new BitmapImage();

            InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();

            await stream.WriteAsync(bytes.AsBuffer());
            stream.Seek(0);

            await bitmap.SetSourceAsync(stream);

            // Do NOT dispose the stream before SetSourceAsync completes.
            // After SetSourceAsync returns, BitmapImage has already consumed it.
            stream.Dispose();

            return bitmap;
        }

        public static async Task<IRandomAccessStream> StreamFromBytesAsync(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;

            InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();

            await stream.WriteAsync(bytes.AsBuffer());
            stream.Seek(0);

            return stream;
        }
    }
}
