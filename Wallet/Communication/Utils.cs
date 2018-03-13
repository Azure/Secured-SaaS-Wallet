using System.IO;
using System.Runtime.Serialization;

namespace Wallet.Communication
{
    /// <summary>
    /// Provides utility helper methods
    /// </summary>
    public static class Utils
    {
        public static byte[] ToByteArray<T>(T source)
        {
            var ser = new DataContractSerializer(typeof(T));
            using (var stream = new MemoryStream())
            {
                ser.WriteObject(stream, source);
                return stream.ToArray();
            }
        }

        public static T FromByteArray<T>(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return default(T);
            }

            var ser = new DataContractSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream(data))
            {
                object obj = ser.ReadObject(ms);
                return (T)obj;
            }
        }
    }
}
