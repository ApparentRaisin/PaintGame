using System.IO.Compression;
using System.IO;

public class DataPreprocessor
{
    // ---- ZIP ---- //
    public byte[] Compress(byte[] bytes, uint seed)
    {
        using (MemoryStream compressStream = new MemoryStream())
        {
            using (var zipStream = new GZipStream(compressStream, CompressionMode.Compress))
                zipStream.Write(bytes, 0, bytes.Length);
            bytes = compressStream.ToArray();
            UnityEngine.Random.InitState((int)seed);
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)(bytes[i] ^ UnityEngine.Random.Range(0, 255));
            return bytes;
        }
    }
    public byte[] Decompress(byte[] bytes, uint seed)
    {
        UnityEngine.Random.InitState((int)seed);
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = (byte)(bytes[i] ^ UnityEngine.Random.Range(0, 255));
        using (var compressStream = new MemoryStream(bytes))
        {
            using (var zipStream = new GZipStream(compressStream, CompressionMode.Decompress))
            {
                using (var resultStream = new MemoryStream())
                {
                    zipStream.CopyTo(resultStream);
                    return resultStream.ToArray();
                }
            }
        }
    }
    // -------- //
}
