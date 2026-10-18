using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Pixel
{
    public int x;
    public int y;
    public Texture2D texture;
}

public class EmbedPointCreator
{
    public int GetTextureShape(int dataLength, int k = 1)
    {
        var matrixShape = Mathf.CeilToInt(Mathf.Sqrt(Mathf.Ceil(dataLength / 3f)));
        matrixShape = math.max(2, matrixShape);
        return matrixShape * 2;
        //int N = DataUtility.ByteToBitSize(dataLength);
        //var matrixShape = Mathf.CeilToInt(Mathf.Sqrt(Mathf.Ceil(N / (3f * k))));
        //matrixShape = math.max(2, matrixShape);
        ////if (matrixShape % 2 == 1)
        ////    matrixShape++;
        //return matrixShape * 2;
    }

    public ColorInt[] CreateRandomColors(int dataLength, Texture2D[] textures, uint seed, out int matrixShape, int k = 1)
    {
        matrixShape = GetTextureShape(dataLength, k);
        int needPixelCount = (int)Mathf.Pow(matrixShape, 2);

        UnityEngine.Random.InitState((int)seed);
        ColorInt[] contentColors = new ColorInt[needPixelCount];
        //for (int i = 0; i < contentColors.Length; ++i)
        //{
        //    var texture = textures[UnityEngine.Random.Range(0, textures.Length)];
        //    contentColors[i] = new ColorInt(texture.GetPixel(UnityEngine.Random.Range(0, texture.width), UnityEngine.Random.Range(0, texture.height)));
        //}

        return contentColors;
    }

    public ColorInt[] LoadTagPixels(Texture2D[] textures, uint seed, out HashSet<int2> tagPoints, int k = 1)
    {
        int tagNum = 2 + textures.Length * 4;
        int n1 = Mathf.CeilToInt(DataUtility.IntToBitSize(tagNum) / (3f * k));
        //Debug.Log($"Tag Bit = {DataUtility.IntToBitSize(tagNum)}");

        if (n1 > textures[0].width * textures[0].height)
        {
            Debug.LogError($"需要的像素数量\"{n1}\"超过了Texture[0]所能提供的数量\"{textures[0].width * textures[0].height}\"");
            tagPoints = null;
            return null;
        }

        UnityEngine.Random.InitState((int)seed);
        tagPoints = new HashSet<int2>();
        ColorInt[] tagColors = new ColorInt[n1];
        var texture = textures[0];
        int index = 0;
        while (tagPoints.Count < n1)
        {
            var point = new int2(UnityEngine.Random.Range(0, texture.width), UnityEngine.Random.Range(0, texture.height));
            if (tagPoints.Add(point))
                tagColors[index++] = new ColorInt(texture.GetPixel(point.x, point.y));
        }

        return tagColors;
    }

    public ColorInt[] LoadEmbedPixels(int contentDataLength, Texture2D[] textures, float4[] logisticParams, HashSet<int2> exclusionPoints, uint seed, int k = 1)
    {
        int n2 = Mathf.CeilToInt(DataUtility.ByteToBitSize(contentDataLength) / (3f * k));

        ColorInt[] contentColors = new ColorInt[n2];
        int index = 0;
        for (int i = 0; i < textures.Length; i++)
        {
            int width = textures[i].width;
            int height = textures[i].height;
            HashSet<int2> subPoints;
            if (i == 0)
            {
                float time = Time.realtimeSinceStartup;
                subPoints = BWOLogistic.Logistic(logisticParams[i], n2, width, height, exclusionPoints);
                float deltaTime = Time.realtimeSinceStartup - time;
                Debug.LogWarning($"---- Logistic[{i}] Time: {deltaTime}");

                time = Time.realtimeSinceStartup;
                subPoints = BWOLogistic.SupplementLogistic(subPoints, n2, width, height, exclusionPoints);
                deltaTime = Time.realtimeSinceStartup - time;
                Debug.LogWarning($"---- Supplement[{i}] Time: {deltaTime}");
            }
            else
            {
                float time = Time.realtimeSinceStartup;
                subPoints = BWOLogistic.Logistic(logisticParams[i], n2, width, height, null);
                float deltaTime = Time.realtimeSinceStartup - time;
                Debug.LogWarning($"---- Logistic[{i}] Time: {deltaTime}");

                time = Time.realtimeSinceStartup;
                subPoints = BWOLogistic.SupplementLogistic(subPoints, n2, width, height, null);
                deltaTime = Time.realtimeSinceStartup - time;
                Debug.LogWarning($"---- Supplement[{i}] Time: {deltaTime}");
            }
            //Debug.LogError($"[{i}] {logisticParams[i]}\nN2 = {n2} - {subPoints.Count} = {n2 - subPoints.Count}\nSize = {width}x{width}");

            bool isOver = false;
            foreach (var item in subPoints)
            {
                contentColors[index] = new ColorInt(textures[i].GetPixel(item.x, item.y));
                index++;
                if (index >= contentColors.Length)
                {
                    isOver = true;
                    break;
                }
            }
            if (isOver)
                break;

            n2 -= subPoints.Count;
            if (n2 <= 0)
                break;
        }

        return contentColors;
    }

    public (Pixel[], ColorInt[], Pixel[], ColorInt[], float4[]) CreateEmbedPixels(int contentDataLength, Texture2D[] textures, uint seed, int k = 1)
    {
        int n2 = Mathf.CeilToInt(DataUtility.ByteToBitSize(contentDataLength) / (3f * k));

        ColorInt[] tagColors = LoadTagPixels(textures, seed, out var tagPoints);
        Pixel[] tagPixels = new Pixel[tagColors.Length];
        int index = 0;
        foreach (var item in tagPoints)
        {
            tagPixels[index++] = new Pixel() { 
                x = item.x,
                y = item.y,
                texture = textures[0],
            };
        }

        Pixel[] contentPixels = new Pixel[n2];
        ColorInt[] contentColors = new ColorInt[n2];
        float4[] textureLogisticParams = new float4[textures.Length];

        UnityEngine.Random.InitState((int)seed);
        index = 0;
        for (int i = 0; i < textures.Length; i++)
        {
            Logger.usedTextureNum++;
            BWOLogistic bwo = new BWOLogistic();
            if (i == 0)
                bwo.Init(textures[i], tagPoints);
            else
                bwo.Init(textures[i]);
            bwo.Run(5, 1f, 10, n2, (uint)UnityEngine.Random.Range(0, seed));
            textureLogisticParams[i] = bwo.bestWhale.logisticParams;
            var positions = bwo.bestEmbedPoints;
            //Debug.LogError($"[{i}] {bwo.bestWhale.logisticParams}\nN2 = {n2} - {positions.Count} = {n2 - positions.Count}\nSize = {textures[i].width}x{textures[i].width}");
            //if (i == 0)
            //{
            //    positions = BWOLogistic.Logistic(bwo.bestWhale.logisticParams, n2, textures[i].width, textures[i].height, tagPoints);
            //    positions = BWOLogistic.SupplementLogistic(positions, n2, textures[i].width, textures[i].height, tagPoints);
            //}
            //else
            //{
            //    positions = BWOLogistic.Logistic(bwo.bestWhale.logisticParams, n2, textures[i].width, textures[i].height, null);
            //    positions = BWOLogistic.SupplementLogistic(positions, n2, textures[i].width, textures[i].height, null);
            //}
            foreach (var item in positions)
            {
                contentPixels[index] = new Pixel()
                {
                    x = item.x,
                    y = item.y,
                    texture = textures[i],
                };
                contentColors[index] = new ColorInt(textures[i].GetPixel(item.x, item.y));
                //Debug.Log($"{index}/{contentColors.Length} | {needPixelCount}");
                index++;
                if (index >= contentPixels.Length)
                    break;
            }
            bwo.Dispose();

            n2 -= positions.Count;
            if (n2 <= 0)
                break;
        }

        return (tagPixels, tagColors, contentPixels, contentColors, textureLogisticParams);
    }
}
