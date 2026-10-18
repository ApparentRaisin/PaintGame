using System.IO.Compression;
using System.IO;
using UnityEngine;
using Unity.Mathematics;
using System.Text;
using System.Linq;
using Unity.Collections;

public static class DataUtility
{
    // ------------------ 颜色运算 ------------------- //
    public static Color[] FormatColors255(Color[] colors)
    {
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i].r = DataUtility.FormatFloat255(colors[i].r);
            colors[i].g = DataUtility.FormatFloat255(colors[i].g);
            colors[i].b = DataUtility.FormatFloat255(colors[i].b);
        }
        return colors;
    }

    public static float FormatFloat255(float channel)
    {
        for (int i = 0; i < 256; i++)
        {
            float value = i / 255f;
            if (Mathf.Abs(channel - value) <= 0.000001f)
                //if (Mathf.Abs(channel - value) <= 0.003921f)
                return value;
        }
        return channel;
    }

    public static int ChannelTo255(float channel)
    {
        for (int i = 0; i < 256; i++)
        {
            float value = i / 255f;
            if (Mathf.Abs(channel - value) <= 0.000001f)
                return i;
        }
        return (int)math.round(channel * 255f);
    }
    public static char GetChannelLSB(float channel)
    {
        return (char)(ChannelTo255(channel) % 2 + '0');
    }
    //public static char GetChannelLSB(int channel)
    //{
    //    return (char)(channel % 2 + '0');
    //}
    public static int BitsToChannel255(NativeArray<char> bits)
    {
        int value255 = 0;
        for (int i = bits.Length - 1; i >= 0; i--)
        {
            if (bits[i] == '0')
                continue;
            value255 += (int)math.pow(2, 7 - i);
        }
        return value255;
    }
    public static float BitsToChannel(NativeArray<char> bits)
    {
        return FormatFloat255(BitsToChannel255(bits) / 255f);
    }
    //public static NativeArray<char> ChannelToBits(int channel)
    //{
    //    return Channel255ToBits(channel);
    //}
    public static NativeArray<char> ChannelToBits(float channel)
    {
        var channel_255 = ChannelTo255(channel);
        return Channel255ToBits(channel_255);
    }
    public static NativeArray<char> Channel255ToBits(int channel_255)
    {
        NativeArray<char> bits = new NativeArray<char>(8, Allocator.Temp);
        for (int i = 0; i < 8; i++)
        {
            char bit;
            if (channel_255 > 0)
            {
                bit = (char)((channel_255 % 2) + '0');
                channel_255 /= 2;
            }
            else
                bit = '0';
            bits[7 - i] = bit;
        }
        return bits;
    }

    public static NativeArray<char> ChannelToBits(float channel, out int channel_255)
    {
        channel_255 = ChannelTo255(channel);
        NativeArray<char> bits = new NativeArray<char>(8, Allocator.Temp);
        for (int i = 0; i < 8; i++)
        {
            char bit;
            if (channel_255 > 0)
            {
                bit = (char)((channel_255 % 2) + '0');
                channel_255 /= 10;
            }
            else
                bit = '0';
            bits[7 - i] = bit;
        }
        return bits;
    }

    // ------------------ 位运算 / 进制运算 ------------------- //

    public static char[] BytesToBits(byte[] bytes)
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
            builder.Append(System.Convert.ToString(bytes[i], 2).PadLeft(8, '0'));
        return builder.ToString().ToCharArray();
    }

    public static byte[] BitsToBytes(char[] bits)
    {
        byte[] bytes = new byte[bits.Length / 8];
        int byteIndex = 0;
        StringBuilder builder = new StringBuilder(8);
        for (int i = 0; i < bits.Length; i++)
        {
            builder.Append(bits[i]);
            if (builder.Length == 8)
            {
                bytes[byteIndex++] = System.Convert.ToByte(builder.ToString(), 2);
                builder.Clear();
            }
        }
        return bytes;
    }

    public static int BitsToInt(char[] bits)
    {
        return BytesToInt(BitsToBytes(bits));
    }

    public static int[] BitsToInts(char[] bits)
    {
        return BytesToInts(BitsToBytes(bits));
    }

    public static byte[] ToBytes(int value)
    {
        return System.BitConverter.GetBytes(value);
    }
    public static byte[] ToBytes(float value)
    {
        return System.BitConverter.GetBytes(value);
    }

    public static byte[] ToBytes(params int[] values)
    {
        byte[] bytes = new byte[IntToByteSize(values.Length)];
        for (int i = 0; i < values.Length; i++)
        {
            var itemBytes = ToBytes(values[i]);
            int index = i * 4;
            bytes[index] = itemBytes[0];
            bytes[index + 1] = itemBytes[1];
            bytes[index + 2] = itemBytes[2];
            bytes[index + 3] = itemBytes[3];
        }
        return bytes;
    }

    public static byte[] ToBytes(int[] ints, float[] floats)
    {
        byte[] bytes = new byte[IntToByteSize(ints.Length + floats.Length)];
        int index = 0;
        for (int i = 0; i < ints.Length; i++)
        {
            var itemBytes = ToBytes(ints[i]);
            bytes[index++] = itemBytes[0];
            bytes[index++] = itemBytes[1];
            bytes[index++] = itemBytes[2];
            bytes[index++] = itemBytes[3];
        }
        for (int i = 0; i < floats.Length; i++)
        {
            var itemBytes = ToBytes(floats[i]);
            bytes[index++] = itemBytes[0];
            bytes[index++] = itemBytes[1];
            bytes[index++] = itemBytes[2];
            bytes[index++] = itemBytes[3];
        }
        return bytes;
    }
    public static byte[] ToBytes(int[] ints, float4[] float4s)
    {
        byte[] bytes = new byte[IntToByteSize(ints.Length + float4s.Length * 4)];
        int index = 0;
        for (int i = 0; i < ints.Length; i++)
        {
            var itemBytes = ToBytes(ints[i]);
            bytes[index++] = itemBytes[0];
            bytes[index++] = itemBytes[1];
            bytes[index++] = itemBytes[2];
            bytes[index++] = itemBytes[3];
        }
        for (int i = 0; i < float4s.Length; i++)
        {
            var itemBytes = ToBytes(float4s[i].x);
            bytes[index++] = itemBytes[0];
            bytes[index++] = itemBytes[1];
            bytes[index++] = itemBytes[2];
            bytes[index++] = itemBytes[3];
            itemBytes = ToBytes(float4s[i].y);
            bytes[index++] = itemBytes[0];
            bytes[index++] = itemBytes[1];
            bytes[index++] = itemBytes[2];
            bytes[index++] = itemBytes[3];
            itemBytes = ToBytes(float4s[i].z);
            bytes[index++] = itemBytes[0];
            bytes[index++] = itemBytes[1];
            bytes[index++] = itemBytes[2];
            bytes[index++] = itemBytes[3];
            itemBytes = ToBytes(float4s[i].w);
            bytes[index++] = itemBytes[0];
            bytes[index++] = itemBytes[1];
            bytes[index++] = itemBytes[2];
            bytes[index++] = itemBytes[3];
        }
        return bytes;
    }

    public static int BytesToInt(byte[] bytes)
    {
        return System.BitConverter.ToInt32(bytes);
    }
    public static float BytesToFloat(byte[] bytes)
    {
        return System.BitConverter.ToSingle(bytes);
    }

    public static int[] BytesToInts(byte[] bytes)
    {
        int[] result = new int[bytes.Length / 4];
        int resultIndex = 0;
        byte[] temp = new byte[4];
        int index = 0;
        for (int i = 0; i < bytes.Length; i++)
        {
            temp[index++] = bytes[i];
            if (index == 4)
            {
                result[resultIndex++] = BytesToInt(temp);
                index = 0;
            }
        }
        return result;
    }

    public static (int[], float[]) BytesToIntsAndFloats(byte[] bytes, int intCount, int floatCount)
    {
        int[] resultInt = new int[intCount];
        float[] resultFloat = new float[floatCount];

        int resultIndex = 0;
        byte[] temp = new byte[4];
        int index = 0;
        for (int i = 0; i < bytes.Length; i++)
        {
            temp[index++] = bytes[i];
            if (index == 4)
            {
                if(resultIndex < intCount)
                    resultInt[resultIndex++] = BytesToInt(temp);
                else
                    resultFloat[resultIndex++] = BytesToFloat(temp);
                index = 0;
            }
        }
        return (resultInt, resultFloat);
    }

    public static (int[], float4[]) BytesToIntsAndFloat4s(byte[] bytes, int intCount, int floatCount)
    {
        int[] resultInt = new int[intCount];
        float4[] resultFloat4 = new float4[floatCount];

        int resultIndex = 0;
        byte[] temp = new byte[4];
        int index = 0;
        int float4Index = 0;
        for (int i = 0; i < bytes.Length; i++)
        {
            temp[index++] = bytes[i];
            if (index == 4)
            {
                if (resultIndex < intCount)
                {
                    resultInt[resultIndex++] = BytesToInt(temp);
                }
                else
                {
                    int currentIndex = resultIndex - intCount;
                    switch (float4Index)
                    {
                        case 0: resultFloat4[currentIndex].x = BytesToFloat(temp); float4Index++; break;
                        case 1: resultFloat4[currentIndex].y = BytesToFloat(temp); float4Index++; break;
                        case 2: resultFloat4[currentIndex].z = BytesToFloat(temp); float4Index++; break;
                        case 3: 
                            resultFloat4[currentIndex].w = BytesToFloat(temp);
                            float4Index = 0;
                            resultIndex++;
                            break;
                    }
                }
                index = 0;
            }
        }
        return (resultInt, resultFloat4);
    }

    /// <summary>
    /// 该函数的作用是单位转换，用于计算数个int等于几个byte（如：1 int = 4 byte， 2 int = 8 byte）
    /// </summary>
    public static int IntToByteSize(int intCount)
    {
        return intCount * 4;
    }
    /// <summary>
    /// 该函数的作用是单位转换，用于计算数个int等于几个byte（如：1 int = 4 byte， 2 int = 8 byte）
    /// </summary>
    public static int FloatToByteSize(int floatCount)
    {
        return floatCount * 4;
    }

    public static int ByteToBitSize(int byteCount)
    {
        return byteCount * 8;
    }
    public static int BitsToByteSize(int bitsLength)
    {
        return bitsLength / 8;
    }

    public static int IntToBitSize(int intCount)
    {
        return ByteToBitSize(IntToByteSize(intCount));
    }
}
