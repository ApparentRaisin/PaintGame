using System.Collections.Generic;
using System.Text;

public static class Logger
{
    public static Dictionary<string, List<(int, int, float)>> embedTimes;
    public static Dictionary<string, List<(int, int, float)>> extractTimes;
    public static List<(int, int, int)> dataAndTextureLengths;
    public static List<(int, int)> compressDataAndTextureLengths;
    public static int usedTextureNum;

    static Logger()
    {
        Reset();
    }

    static void Reset()
    {
        embedTimes = new Dictionary<string, List<(int, int, float)>>();
        extractTimes = new Dictionary<string, List<(int, int, float)>>();
        dataAndTextureLengths = new List<(int, int, int)>();
        compressDataAndTextureLengths = new List<(int, int)>();
        usedTextureNum = 0;
    }

    public static string ToLog(bool reset = true)
    {
        StringBuilder builder = new StringBuilder("【信息】\n");
        builder.AppendLine($"使用的图片数量 = {usedTextureNum}");

        builder.AppendLine("【嵌入】");
        Dictionary<int, float> avgs = new Dictionary<int, float>();
        foreach (var itemTexture in embedTimes)
        {
            builder.AppendLine(itemTexture.Key);
            foreach (var item in itemTexture.Value)
            {
                avgs.TryAdd(item.Item2, 0f);
                avgs[item.Item2] += item.Item3;
                builder.AppendLine($"({item.Item1}){item.Item2} bit\t {item.Item3}s");
            }
        }
        builder.AppendLine("---- 平均 ----");
        foreach (var item in avgs)
            builder.AppendLine($"{item.Key} bit\t {item.Value / avgs.Count}s");

        builder.AppendLine("\n【提取】");
        avgs.Clear(); 
        foreach (var itemTexture in extractTimes)
        {
            builder.AppendLine(itemTexture.Key);
            foreach (var item in itemTexture.Value)
            {
                avgs.TryAdd(item.Item2, 0f);
                avgs[item.Item2] += item.Item3;
                builder.AppendLine($"({item.Item1}){item.Item2} bit\t {item.Item3}s");
            }
        }
        builder.AppendLine("---- 平均 ----");
        foreach (var item in avgs)
            builder.AppendLine($"{item.Key} bit\t {item.Value / avgs.Count}s");

        //builder.AppendLine("【压缩前的数据】");
        //foreach (var item in compressDataAndTextureLengths)
        //{
        //    builder.AppendLine($"Data: {item.Item1} byte\t Texture ({item.Item3}x{item.Item3}): {item.Item2} byte");
        //}
        builder.AppendLine("\n【数据大小】");
        for (int i = 0; i < dataAndTextureLengths.Count; i++)
        {
            var originItem = dataAndTextureLengths[i];
            if(i >= compressDataAndTextureLengths.Count)
            {
                builder.AppendLine($"Data: {originItem.Item1} byte\t Texture ({originItem.Item3}x{originItem.Item3}): {originItem.Item2} byte");
            }
            else
            {
                var compreItem = compressDataAndTextureLengths[i];
                builder.AppendLine($"Data: {originItem.Item1} → {compreItem.Item1} byte\t Texture ({originItem.Item3}x{originItem.Item3}): {originItem.Item2} → {compreItem.Item2} byte");
            }
        }

        if(reset)
            Reset();
        return builder.ToString();
    }
}