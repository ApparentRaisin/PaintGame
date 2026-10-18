using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;

public class Steganography
{
    public static void MassEmbedTests(Texture2D[] textures, uint seed, int massEmbedBitLength, bool extractCheck = false)
    {
        var tempTextures = new Texture2D[textures.Length];
        for (int i = 0; i < tempTextures.Length; i++)
        {
            tempTextures[i] = GameObject.Instantiate(textures[i]);
            tempTextures[i].name = $"{textures[i].name}_{i}";
        }

        int charLength = massEmbedBitLength / 8; //C#中，char占用8 bit
        char[] chars = new char[charLength];
        for (int j = 0; j < charLength; j++)
            chars[j] = (char)(UnityEngine.Random.value * ('z' - '0') + '0');
        var data = System.Text.Encoding.UTF8.GetBytes(chars);
        int embedBitLength = data.Length * 8;

        float time = Time.realtimeSinceStartup;
        Embed(data, tempTextures, seed);
        float deltaTime = Time.realtimeSinceStartup - time;
        Debug.Log($"[{massEmbedBitLength}] Embed Time: {deltaTime}s");
        if (!Logger.embedTimes.TryGetValue("Mass Mix", out var list))
            Logger.embedTimes.Add("Mass Mix", list = new List<(int, int, float)>());
        list.Add((massEmbedBitLength, embedBitLength, deltaTime));

        if (extractCheck)
        {
            try
            {
                time = Time.realtimeSinceStartup;
                var testBytes = Extract(tempTextures, seed);
                deltaTime = Time.realtimeSinceStartup - time;
                Debug.Log($"[{massEmbedBitLength}] Extract Time: {deltaTime}s");
                //var test = System.Text.Encoding.UTF8.GetString(testBytes);
                //Debug.Log(test);
                //Debug.Log($"Embed & Extract Success");
                if (!Logger.extractTimes.TryGetValue("Mass Mix", out list))
                    Logger.extractTimes.Add("Mass Mix", list = new List<(int, int, float)>());
                list.Add((massEmbedBitLength, embedBitLength, deltaTime));
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"[{massEmbedBitLength}] 读取秘密信息失败\n");
                Debug.LogError(Logger.ToLog());
                return;
            }
        }

        var root = $"{Application.dataPath}/../_JobLSB/_MassEmbed/";
        if (!Directory.Exists(root))
            Directory.CreateDirectory(root);
        //File.WriteAllBytesAsync($"{root}{embedBitLengths[i]}.png", tempTextures[0].EncodeToPNG());
        foreach (var texture in tempTextures)
        {
            var path = $"{root}/{texture.name}.png";
            File.WriteAllBytesAsync(path, texture.EncodeToPNG());
        }
        var log = Logger.ToLog();
        var logPath = $"{root}/{System.DateTime.Now.ToString().Replace('/', '_').Replace(':', '_').Replace(' ', '_')}_数据.txt";
        File.WriteAllBytesAsync(logPath, System.Text.Encoding.UTF8.GetBytes(log));
    }

    public static void SplitEmbed(Texture2D[] textures, uint seed, int[] embedBitLengths, bool extractCheck = false)
    {
        for (int i = 0; i < embedBitLengths.Length; i++)
        {
            int charLength = embedBitLengths[i] / 8; //C#中，char占用8 bit
            char[] chars = new char[charLength];
            for (int j = 0; j < charLength; j++)
                chars[j] = (char)(UnityEngine.Random.value * ('z' - '0') + '0');
            var data = System.Text.Encoding.UTF8.GetBytes(chars);
            int embedBitLength = data.Length * 8;
            //Debug.LogWarning($"Embed Length = {embedBitLength}");
            foreach (var texture in textures)
            {
                var tempTextures = new Texture2D[] { GameObject.Instantiate(texture) };
                tempTextures[0].name = texture.name;

                float time = Time.realtimeSinceStartup;
                Embed(data, tempTextures, seed);
                float deltaTime = Time.realtimeSinceStartup - time;
                Debug.Log($"[{texture.name} | {embedBitLengths[i]}] Embed Time: {deltaTime}s");
                if(!Logger.embedTimes.TryGetValue(texture.name, out var list))
                    Logger.embedTimes.Add(texture.name, list = new List<(int, int, float)>());
                list.Add((embedBitLengths[i], embedBitLength, deltaTime));

                if (extractCheck)
                {
                    try
                    {
                        time = Time.realtimeSinceStartup;
                        var testBytes = Extract(tempTextures, seed);
                        deltaTime = Time.realtimeSinceStartup - time;
                        Debug.Log($"[{texture.name} | {embedBitLengths[i]}] Extract Time: {deltaTime}s");
                        //var test = System.Text.Encoding.UTF8.GetString(testBytes);
                        //Debug.Log(test);
                        //Debug.Log($"Embed & Extract Success");
                        if (!Logger.extractTimes.TryGetValue(texture.name, out list))
                            Logger.extractTimes.Add(texture.name, list = new List<(int, int, float)>());
                        list.Add((embedBitLengths[i], embedBitLength, deltaTime));
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                        Debug.LogError($"[{texture.name} | {embedBitLengths[i]}] 读取秘密信息失败\n");
                        Debug.LogError(Logger.ToLog(false));
                        //return;
                    }
                }

                var root = $"{Application.dataPath}/../_JobLSB/{tempTextures[0].name}/1/";
                if (!Directory.Exists(root))
                    Directory.CreateDirectory(root);
                File.WriteAllBytesAsync($"{root}{embedBitLengths[i]}.png", tempTextures[0].EncodeToPNG());
            }

            foreach (var texture in textures)
            {
                var path = $"{Application.dataPath}/../_JobLSB/{texture.name}/source.png";
                if (!File.Exists(path))
                    File.WriteAllBytesAsync(path, texture.EncodeToPNG());
            }
        }

        var log = Logger.ToLog();
        var logPath = $"{Application.dataPath}/../_JobLSB/{System.DateTime.Now.ToString().Replace('/', '_').Replace(':', '_').Replace(' ', '_')}_数据.txt";
        File.WriteAllBytesAsync(logPath, System.Text.Encoding.UTF8.GetBytes(log));
    }

    public static void Embed(byte[] data, Texture2D[] textures, uint seed)
    {
        var textureData = EmbedFirst(data, textures, seed, out var compressDataLength);
        EmbedSecond(compressDataLength, textureData, textures, seed);
    }

    // 将Data嵌入Texture，并生成TextureData
    static byte[] EmbedFirst(byte[] data, Texture2D[] textures, uint seed, out int compressDataLength)
    {
        int originDataLength = data.Length;
        // ---- 预处理 ---- //
        DataPreprocessor dataPreprocessor = new DataPreprocessor();
        var compressData = dataPreprocessor.Compress(data, seed);
        compressDataLength = compressData.Length;
        //Debug.Log(compressData, "Data = ");

        // ---- 嵌入位置获取 ---- //
        EmbedPointCreator pointCreator = new EmbedPointCreator();
        var textureColors = pointCreator.CreateRandomColors(compressData.Length, textures, seed, out var matrixShape, 8);
        // 转频域Color[]
        IWT.IWT53(textureColors, out var highFrequencyBands, out var lowFrequencyBands);

        // ---- 嵌入 ---- // 
        // Data嵌入频域Color[]
        #region [ Legacy ]
        //highFrequencyBands = JobLSB.Embed(highFrequencyBands, compressData);
        //// 【优化】
        //// compressData每个元素直接赋值给ColorInt的RGB
        #endregion
        for (int i = 0; i < highFrequencyBands.Length; i++)
        {
            int j = i * 3;
            try { highFrequencyBands[i].r = compressData[j]; } catch { break; }
            try { highFrequencyBands[i].g = compressData[j + 1]; } catch { break; }
            try { highFrequencyBands[i].b = compressData[j + 2]; } catch { break; }
        }
        //Debug.Log(highFrequencyBands, "S = ");

        // ---- 后处理 ---- //
        // 频域Color[]转为空域Color[]
        textureColors = IWT.InverseIWT53(highFrequencyBands, lowFrequencyBands);
        //Debug.Log(textureColors, "Texture = ");

        // 创建Texture，并应用空域Color[]的色值
        Texture2D texture = new Texture2D(matrixShape, matrixShape, TextureFormat.RGBA32, false, false);
        texture.filterMode = FilterMode.Point;
        for (int y = 0; y < matrixShape; y++)
        {
            for (int x = 0; x < matrixShape; x++)
            {
                texture.SetPixel(x, y, textureColors[x + y * matrixShape].ToColor());
            }
        }
        texture.Apply();
        var textureData = texture.EncodeToPNG();

        Logger.dataAndTextureLengths.Add((originDataLength, textureData.Length, matrixShape));
        return textureData;
    }

    // 将TagData和TextureData嵌入载体
    static void EmbedSecond(int dataLength, byte[] textureData, Texture2D[] textures, uint seed)
    {
        // ---- 预处理 ---- //
        DataPreprocessor dataPreprocessor = new DataPreprocessor();
        var compressTextureData = dataPreprocessor.Compress(textureData, seed);
        Logger.compressDataAndTextureLengths.Add((dataLength, compressTextureData.Length));

        // ---- 嵌入位置获取 ---- //
        EmbedPointCreator pointCreator = new EmbedPointCreator();
        var result = pointCreator.CreateEmbedPixels(compressTextureData.Length, textures, seed);
        Pixel[] tagPixels = result.Item1;
        ColorInt[] tagColors = result.Item2;
        Pixel[] contentPixels = result.Item3;
        ColorInt[] contentColors = result.Item4;
        float4[] textureLogisticParams = result.Item5;
        byte[] tagData = DataUtility.ToBytes(new int[] { dataLength, compressTextureData.Length }, textureLogisticParams);
        #region [ Log 标记位 ]
        //string str = $"Data = {dataLength}\nTextureData = {compressTextureData.Length}\n";
        //foreach (var item in textureLogisticParams)
        //    str += $"{item}\n";
        //Debug.Log(str);
        #endregion

        // ---- 嵌入 ---- //
        // 将TagData和TextureData嵌入Color[]
        tagColors = JobLSB.Embed(tagColors, tagData);
        contentColors = JobLSB.Embed(contentColors, compressTextureData);
        //Debug.Log(contentColors, "Embed = ");

        // 令载体使用Color[]的颜色
        for (int i = 0; i < tagPixels.Length; i++)
            tagPixels[i].texture.SetPixel(tagPixels[i].x, tagPixels[i].y, tagColors[i].ToColor());
        for (int i = 0; i < contentPixels.Length; i++)
        {
            try
            {
                contentPixels[i].texture.SetPixel(contentPixels[i].x, contentPixels[i].y, contentColors[i].ToColor());
            }
            catch(System.Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"contentPixels[{i}/{contentPixels.Length}] = {contentPixels[i]}\ncontentColors[{i}/{contentColors.Length}] = {contentColors[i]}");
                break;
            }
        }
        foreach (var item in textures)
            item.Apply();
    }

    public static byte[] Extract(Texture2D[] textures, uint seed)
    {
        float time;
        float deltaTime;

        // ---- 提取标记位 ---- //
        time = Time.realtimeSinceStartup;
        EmbedPointCreator pointCreator = new EmbedPointCreator();
        var tagColors = pointCreator.LoadTagPixels(textures, seed, out var tagPoints);
        deltaTime = Time.realtimeSinceStartup - time;
        Debug.Log($"Load TagColor Time: {deltaTime}");

        time = Time.realtimeSinceStartup;
        int tagIntCount = 2;
        int tagFloat4Count = textures.Length;
        var tagData = JobLSB.Extract(tagColors, DataUtility.IntToByteSize(tagIntCount + tagFloat4Count * 4));
        deltaTime = Time.realtimeSinceStartup - time;
        Debug.Log($"Load TagData Time: {deltaTime}");

        var result = DataUtility.BytesToIntsAndFloat4s(tagData, 2, tagFloat4Count);
        int dataLength = result.Item1[0];
        int textureDataLength = result.Item1[1];
        float4[] textureLogisticParams = result.Item2;
        #region [ Log 标记位 ]
        //string str = $"Data = {dataLength}\nTextureData = {textureDataLength}\n";
        //foreach (var item in textureLogisticParams)
        //    str += $"{item}\n";
        //Debug.Log(str);
        #endregion

        // ---- 提取TextureData ---- //
        time = Time.realtimeSinceStartup;
        var textureColors = pointCreator.LoadEmbedPixels(textureDataLength, textures, textureLogisticParams, tagPoints, seed);
        deltaTime = Time.realtimeSinceStartup - time;
        Debug.Log($"Load EmbedPixels Time: {deltaTime}");
        //Debug.LogWarning(textureColors, "Embed = ");
        time = Time.realtimeSinceStartup;
        var textureData = JobLSB.Extract(textureColors, textureDataLength);
        deltaTime = Time.realtimeSinceStartup - time;
        Debug.Log($"Load TextureData Time: {deltaTime}");

        DataPreprocessor dataPreprocessor = new DataPreprocessor();
        textureData = dataPreprocessor.Decompress(textureData, seed);

        // ---- TextureData ---- //
        time = Time.realtimeSinceStartup;
        var matrixShape = pointCreator.GetTextureShape(dataLength);
        Texture2D texture = new Texture2D(matrixShape, matrixShape, TextureFormat.RGBA32, false, false);
        texture.filterMode = FilterMode.Point;
        texture.LoadImage(textureData);
        var contentColors = new ColorInt[matrixShape * matrixShape];
        for (int y = 0; y < matrixShape; y++)
        {
            for (int x = 0; x < matrixShape; x++)
            {
                contentColors[x + y * matrixShape] = new ColorInt(texture.GetPixel(x, y));
            }
        }
        Debug.Log($"LoadFrom Texture Time: {deltaTime}");

        time = Time.realtimeSinceStartup;
        IWT.IWT53(contentColors, out var highFrequencyBands, out var lowFrequencyBands);
        deltaTime = Time.realtimeSinceStartup - time;
        Debug.Log($"IWT Time: {deltaTime}");
        //Debug.LogWarning(highFrequencyBands, "S = ");

        #region [ Legacy ]
        //var compressData = JobLSB.Extract(highFrequencyBands, dataLength);
        #endregion
        time = Time.realtimeSinceStartup;
        byte[] compressData = new byte[dataLength];
        for (int i = 0; i < highFrequencyBands.Length; i++)
        {
            int j = i * 3;
            try { compressData[j] = (byte)highFrequencyBands[i].r; } catch { break; }
            try { compressData[j + 1] = (byte)highFrequencyBands[i].g; } catch { break; }
            try { compressData[j + 2] = (byte)highFrequencyBands[i].b; } catch { break; }
        }
        deltaTime = Time.realtimeSinceStartup - time;
        Debug.Log($"Data Time: {deltaTime}");
        //Debug.Log(compressData, "Data = ");

        return dataPreprocessor.Decompress(compressData, seed);
    }
}
