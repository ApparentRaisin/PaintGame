using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

public static class JobLSB
{
    [BurstCompile]
    struct EmbedBitJob : IJobParallelFor
    {
        public NativeArray<ColorInt> colors;
        [NativeDisableParallelForRestriction]
        public NativeArray<char> dataBits;

        public void Execute(int colorIndex)
        {
            var c = colors[colorIndex];
            var bitIndex = colorIndex * 3;
            //Debug.Log($"[{index}] {c}");
            c.r = EmbeddedBit(c.r, bitIndex);
            c.g = EmbeddedBit(c.g, bitIndex + 1);
            c.b = EmbeddedBit(c.b, bitIndex + 2);
            //Debug.LogWarning($"[{index}] {c}");
            colors[colorIndex] = c;
        }

        int EmbeddedBit(int channel, int index)
        {
            if (index >= dataBits.Length)
                return channel;

            if (dataBits[index] == '0')
            {
                if(channel % 2 == 1)
                    return channel - 1;
            }
            else
            {
                if (channel % 2 == 0)
                    return channel + 1;
            }
            return channel;
        }
    }

    [BurstCompile]
    struct ExtractBitJob : IJobParallelFor
    {
        public NativeArray<ColorInt> colors;
        [NativeDisableParallelForRestriction]
        public NativeArray<char> dataBits;

        public void Execute(int index)
        {
            int colorIndex = index;
            var c = colors[index];
            index = colorIndex * 3;
            ExtractBit(c.r, index);
            ExtractBit(c.g, index + 1);
            ExtractBit(c.b, index + 2);
        }

        void ExtractBit(int channel, int index)
        {
            if (index >= dataBits.Length)
                return;
            dataBits[index] = channel % 2 == 0 ? '0' : '1';
            //dataBits[index] = DataUtility.GetChannelLSB(channel);
            //dataBits[index] = Debug.ChannelToBinary(channel)[7];
        }
    }

    static bool isEmbedJobBuild;
    static EmbedBitJob embedJob;
    public static ColorInt[] Embed(ColorInt[] colors, byte[] bytes, bool log = false)
    {
        var bits = DataUtility.BytesToBits(bytes);
        if (!isEmbedJobBuild)
        {
            isEmbedJobBuild = true;
            embedJob = new EmbedBitJob();
        }
        var colorArray = new NativeArray<ColorInt>(colors, Allocator.TempJob);
        var bitArray = new NativeArray<char>(bits, Allocator.TempJob);
        embedJob.colors = colorArray;
        embedJob.dataBits = bitArray;
        //embedJob.random = new Unity.Mathematics.Random((uint)seed);

        embedJob.Schedule(colors.Length, 1000).Complete();

        if (log)
            Debug.Log(string.Join("", bitArray));

        var result = colorArray.ToArray();
        colorArray.Dispose();
        bitArray.Dispose();

        return result;
    }

    static bool isExtractJobBuild;
    static ExtractBitJob extractJob;
    public static byte[] Extract(ColorInt[] colors, int bytesLength, bool log = false)
    {
        if (!isExtractJobBuild)
        {
            isExtractJobBuild = true;
            extractJob = new ExtractBitJob();
        }
        var colorArray = new NativeArray<ColorInt>(colors, Allocator.TempJob);
        var bitArray = new NativeArray<char>(DataUtility.ByteToBitSize(bytesLength), Allocator.TempJob);
        extractJob.colors = colorArray;
        extractJob.dataBits = bitArray;

        extractJob.Schedule(colors.Length, 1000).Complete();

        if (log)
            Debug.Log(string.Join("", bitArray));

        var bits = bitArray.ToArray();
        colorArray.Dispose();
        bitArray.Dispose();

        return DataUtility.BitsToBytes(bits);
    }
}
