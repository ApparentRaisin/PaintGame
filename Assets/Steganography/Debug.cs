using System.Text;
using UnityEngine;

public class Debug : UnityEngine.Debug
{
    public static void Log(ColorInt[] colors, string prefix = "")
    {
        StringBuilder builder = new StringBuilder(prefix);
        var length = Mathf.Min(200, colors.Length);
        for (int i = 0; i < length; i++)
        {
            builder.AppendLine($"[{i}] {colors[i]}");
        }
        Debug.Log(builder.ToString());
    }
    public static void LogWarning(ColorInt[] colors, string prefix = "")
    {
        StringBuilder builder = new StringBuilder(prefix);
        var length = Mathf.Min(200, colors.Length);
        for (int i = 0; i < length; i++)
        {
            builder.AppendLine($"[{i}] {colors[i]}");
        }
        Debug.LogWarning(builder.ToString());
    }

    public static void Log(byte[] bytes, string prefix = "")
    {
        StringBuilder builder = new StringBuilder(prefix);
        var length = Mathf.Min(200, bytes.Length);
        int count = 0;
        for (int i = 0; i < length; i++)
        {
            builder.Append(bytes[i]).Append(' ');
            count++;
            if (count >= 5)
            {
                builder.AppendLine();
                count = 0;
            }
        }
        Debug.LogWarning(builder.ToString());
    }

    public static void Log(Color[] colors)
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < colors.Length; i++)
        {
            builder.AppendLine($"[{i}] {colors[i].r:f7}, {colors[i].g:f7}, {colors[i].b:f7}");
        }
        Debug.Log(builder.ToString());
    }

    public static void Log(Color[] colors, int width, int height)
    {
        StringBuilder builder = new StringBuilder();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                builder.AppendLine($"[{y}, {x}] {colors[y * width + x].r:f7}, {colors[y * width + x].g:f7}, {colors[y * width + x].b:f7}");
            }
        }
        Debug.Log(builder.ToString());
    }

    public static int ColorFloatToInt(float channel)
    {
        return Mathf.RoundToInt(channel * 255f);
    }

    public static string ChannelToBinary(float rgbChannel, out int rgbChannel_255)
    {
        rgbChannel_255 = ColorFloatToInt(rgbChannel);
        return System.Convert.ToString(rgbChannel_255, 2).PadLeft(8, '0');
    }
    public static string ChannelToBinary(float rgbChannel)
    {
        return System.Convert.ToString(ColorFloatToInt(rgbChannel), 2).PadLeft(8, '0');
    }
    public static string ChannelToBinary(int rgbChannel)
    {
        return System.Convert.ToString(rgbChannel, 2).PadLeft(8, '0');
    }

    public static string BytesToBinaryString(byte[] bytes, bool ignoreBinaryLength = false)
    {
        if (ignoreBinaryLength)
            return DoBytesToBinaryString(bytes);
        return DoBytesToBinaryString(System.BitConverter.GetBytes(bytes.Length)) + DoBytesToBinaryString(bytes);
    }
    public static string DoBytesToBinaryString(byte[] bytes)
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
            builder.Append(System.Convert.ToString(bytes[i], 2).PadLeft(8, '0'));

        return builder.ToString();
    }

}
