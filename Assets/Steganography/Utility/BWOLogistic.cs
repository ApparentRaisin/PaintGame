using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class WhaleLogistic
{
    public float4 logisticParams;
    public float fitness;
    public float blance; //平衡因子

    public float SSIM;
    public float fitRate;

    public WhaleLogistic()
    {
        this.fitness = float.MinValue;
    }
}

internal class BWOLogistic : System.IDisposable
{
    HashSet<int2> exclusionPoints;

    NativeArray<int3> texturePixels;
    int2 textureSize;
    int needPixelCount;
    Unity.Mathematics.Random globalRandom;

    public List<WhaleLogistic> whales;
    public int bestWhaleIndex;
    public HashSet<int2> bestEmbedPoints;
    public WhaleLogistic bestWhale { get { return whales[bestWhaleIndex]; } }

    public void Dispose()
    {
        texturePixels.Dispose();
    }

    public void Init(Texture2D texture, HashSet<int2> exclusionPoints = null)
    {
        this.exclusionPoints = exclusionPoints;
        this.textureSize = new int2(texture.width, texture.height);
        this.texturePixels = new NativeArray<int3>(textureSize.x * textureSize.y, Allocator.Persistent);
        for (int y = 0; y < this.textureSize.y; y++)
        {
            for (int x = 0; x < this.textureSize.x; x++)
            {
                var c = texture.GetPixel(x, y);
                this.texturePixels[x + y * textureSize.x] = new int3(
                    (int)math.floor(c.r * 255f),
                    (int)math.floor(c.g * 255f),
                    (int)math.floor(c.b * 255f)
                    );
            }
        }
    }

    public void Run(int jobCount, float embeddingRate, int maxIteration, int needPixelCount = -1, uint seed = 0)
    {
        if (seed == 0)
            seed = (uint)(Time.realtimeSinceStartup * 10);
        globalRandom = new Unity.Mathematics.Random(seed);

        whales = new List<WhaleLogistic>(jobCount);
        this.bestWhaleIndex = -1;

        int embedPixelCount = (int)(embeddingRate * (textureSize.x * textureSize.y));
        if (needPixelCount > 0)
            embedPixelCount = math.min(needPixelCount, embedPixelCount);
        this.needPixelCount = embedPixelCount;
        //GeneratePointsJob generatePointsJob = new GeneratePointsJob();
        //generatePointsJob.textureSize = textureSize;
        for (int i = 0; i < jobCount; i++)
        {
            WhaleLogistic whale = new WhaleLogistic();
            whale.logisticParams = new float4(
                    globalRandom.NextFloat(3.5f, 4.5f),
                    globalRandom.NextFloat(0.1f, 1f),
                    globalRandom.NextFloat(0.3f, 0.7f),
                    globalRandom.NextFloat(0.3f, 0.7f)
                );
            whales.Add(whale);
            //generatePointsJob.random = new Unity.Mathematics.Random(globalRandom.NextUInt());
            //generatePointsJob.positions = whale.positions = new NativeArray<int2>(embedPixelCount, Allocator.Persistent);
            //generatePointsJob.ScheduleParallel(embedPixelCount, loopBatchCount, default).Complete();
        }

        for (int t = 0; t < maxIteration; t++)
        {
            MoveWhales(t, maxIteration);
            FallWhales(t, maxIteration);
        }

        bestEmbedPoints = Logistic(bestWhale.logisticParams, needPixelCount, textureSize.x, textureSize.y, exclusionPoints);
        bestEmbedPoints = SupplementLogistic(bestEmbedPoints, needPixelCount, textureSize.x, textureSize.y, exclusionPoints);
    }

    [BurstCompile]
    struct MoveWhaleJob : IJobFor
    {
        [ReadOnly] public int currentIteration;
        [ReadOnly] public int maxIteration;
        [ReadOnly] public int bestWhaleIndex;
        [ReadOnly] public NativeArray<float4> whaleParams;
        public NativeArray<float> blances;
        public NativeArray<float4> resultParams;
        [ReadOnly] public Unity.Mathematics.Random random;

        public void Execute(int index)
        {
            blances[index] = random.NextFloat() * (1 - currentIteration / (2f * maxIteration));
            if (bestWhaleIndex == -1 || blances[index] > 0.5f)
            {
                // ---- 探索阶段 ---- //
                // 随机选择1只白鲸
                int whaleCount = whaleParams.Length;
                int r = random.NextInt(0, whaleCount);
                if (index == r) r = (r + 1) % whaleCount;

                var r1 = random.NextFloat(0f, 1f);
                var r2 = random.NextFloat(0f, 1f);

                float4 offset = whaleParams[r] - whaleParams[index];
                float4 newParams = whaleParams[index];
                newParams.x += offset.x * (1f + r1) * math.sin(2f * math.PI * r2);
                newParams.y += offset.y * (1f + r1) * math.cos(2f * math.PI * r2);
                newParams.z += offset.z * (1f + r1) * math.sin(2f * math.PI * r2);
                newParams.w += offset.w * (1f + r1) * math.cos(2f * math.PI * r2);
                resultParams[index] = newParams;
            }
            else
            {
                float4 offset = whaleParams[bestWhaleIndex] - whaleParams[index];
                Vector2 offsetR12 = new Vector2(offset.x, offset.y).normalized;
                Vector2 offsetXY = new Vector2(offset.z, offset.w).normalized;
                float4 newParams = whaleParams[index];
                newParams.x += offsetR12.x * random.NextFloat(0f, 0.2f);
                newParams.y += offsetR12.y * random.NextFloat(0f, 0.2f);
                newParams.z += offsetXY.x * random.NextFloat(0f, 0.2f);
                newParams.w += offsetXY.y * random.NextFloat(0f, 0.2f);
                resultParams[index] = newParams;
            }
        }
    }

    MoveWhaleJob moveWhaleJob;
    bool isMoveWhaleJobBuild;

    void MoveWhales(int t, int maxIterations)
    {
        if (!isMoveWhaleJobBuild)
        {
            isMoveWhaleJobBuild = true;
            moveWhaleJob = new MoveWhaleJob();
            moveWhaleJob.maxIteration = maxIterations;
        }
        moveWhaleJob.currentIteration = t;
        moveWhaleJob.bestWhaleIndex = bestWhaleIndex;
        moveWhaleJob.blances = new NativeArray<float>(whales.Count, Allocator.TempJob);
        moveWhaleJob.whaleParams = new NativeArray<float4>(whales.Count, Allocator.TempJob);
        moveWhaleJob.resultParams = new NativeArray<float4>(whales.Count, Allocator.TempJob);
        moveWhaleJob.random = new Unity.Mathematics.Random(globalRandom.NextUInt());
        for (int i = 0; i < whales.Count; i++)
            moveWhaleJob.whaleParams[i] = whales[i].logisticParams;
        moveWhaleJob.ScheduleParallel(whales.Count, 1, default).Complete();

        var blances = moveWhaleJob.blances.ToArray();
        var logisticParams = moveWhaleJob.resultParams.ToArray();
        moveWhaleJob.blances.Dispose();
        moveWhaleJob.whaleParams.Dispose();
        moveWhaleJob.resultParams.Dispose();

        for (int i = 0; i < whales.Count; i++)
        {
            whales[i].blance = blances[i];
            // 计算鲸鱼的适应度
            var fitness = CountFitness(logisticParams[i], out var points, out var SSIM);
            if (fitness > whales[i].fitness)
            {
                whales[i].SSIM = SSIM;
                whales[i].logisticParams = logisticParams[i];
                whales[i].fitness = fitness;
                whales[i].fitRate = fitness - SSIM;
                if (bestWhaleIndex < 0 || fitness > whales[bestWhaleIndex].fitness)
                {
                    bestEmbedPoints = points;
                    bestWhaleIndex = i;
                }
            }
        }
    }

    [BurstCompile]
    struct FallWhalesJob : IJobFor
    {
        [ReadOnly] public int currentIteration;
        [ReadOnly] public int maxIteration;
        [ReadOnly] public NativeArray<float4> whaleParams;
        [ReadOnly] public NativeArray<float> blances;
        public NativeArray<float4> resultParams;
        [ReadOnly] public Unity.Mathematics.Random random;

        public void Execute(int index)
        {
            float fallProbability = 0.1f - 0.05f * (currentIteration / (float)maxIteration); // 鲸落概率
            if (blances[index] > fallProbability)
                return;

            // 随机选择1只白鲸
            int whaleCount = whaleParams.Length;
            int r = random.NextInt(0, whaleCount);
            if (index == r) r = (r + 1) % whaleCount;

            var c2 = 2 * fallProbability * whaleCount;
            var exp = math.exp((-c2 * currentIteration) / (float)maxIteration);
            float4 step = new float4(exp, exp, exp, exp) * random.NextFloat(0.3f, 1f);

            float4 newParams = whaleParams[index];
            newParams.x = step.x + whaleParams[index].x * random.NextFloat(0.1f, 0.5f) - whaleParams[r].x * random.NextFloat(0.1f, 0.5f);
            newParams.y = step.y + whaleParams[index].y * random.NextFloat(0.1f, 0.5f) - whaleParams[r].y * random.NextFloat(0.1f, 0.5f);
            newParams.z = step.z + whaleParams[index].z * random.NextFloat(0.1f, 0.5f) - whaleParams[r].z * random.NextFloat(0.1f, 0.5f);
            newParams.w = step.w + whaleParams[index].w * random.NextFloat(0.1f, 0.5f) - whaleParams[r].w * random.NextFloat(0.1f, 0.5f);
            resultParams[index] = newParams;
        }
    }

    FallWhalesJob fallWhalesJob;
    bool isFallWhalesJobBuild;

    void FallWhales(int t, int maxIterations)
    {
        if (!isFallWhalesJobBuild)
        {
            isFallWhalesJobBuild = true;
            fallWhalesJob = new FallWhalesJob();
            fallWhalesJob.maxIteration = maxIterations;
        }
        fallWhalesJob.currentIteration = t;
        fallWhalesJob.blances = new NativeArray<float>(whales.Count, Allocator.TempJob);
        fallWhalesJob.whaleParams = new NativeArray<float4>(whales.Count, Allocator.TempJob);
        fallWhalesJob.resultParams = new NativeArray<float4>(whales.Count, Allocator.TempJob);
        fallWhalesJob.random = new Unity.Mathematics.Random(globalRandom.NextUInt());
        for (int i = 0; i < whales.Count; i++)
        {
            fallWhalesJob.blances[i] = whales[i].blance;
            fallWhalesJob.whaleParams[i] = whales[i].logisticParams;
        }
        fallWhalesJob.ScheduleParallel(whales.Count, 1, default).Complete();

        var logisticParams = fallWhalesJob.resultParams.ToArray();
        fallWhalesJob.blances.Dispose();
        fallWhalesJob.whaleParams.Dispose();
        fallWhalesJob.resultParams.Dispose();

        for (int i = 0; i < whales.Count; i++)
        {
            // 计算鲸鱼的适应度
            var fitness = CountFitness(logisticParams[i], out var points, out var SSIM);
            if (fitness > whales[i].fitness)
            {
                whales[i].SSIM = SSIM;
                whales[i].logisticParams = logisticParams[i];
                whales[i].fitness = fitness;
                whales[i].fitRate = fitness - SSIM;
                if (bestWhaleIndex < 0 || fitness > whales[bestWhaleIndex].fitness)
                {
                    bestEmbedPoints = points;
                    bestWhaleIndex = i;
                }
            }
        }
    }


    public float CountFitness(float4 logisticParams, out HashSet<int2> points, out float SSIM)
    {
        points = Logistic(logisticParams, needPixelCount, textureSize.x, textureSize.y, exclusionPoints);
        if (points.Count < 2)
        {
            SSIM = 0f;
            return 0f;
        }
        SSIM = CalculateSSIM(points);
        return points.Count / (float)needPixelCount + SSIM;
    }

    [BurstCompile]
    struct SSIMJob : IJobFor
    {
        [ReadOnly] public int2 textureSize;
        [ReadOnly] public NativeArray<int3> texturePixels;
        [ReadOnly] public NativeArray<int2> points;
        public NativeArray<float> ssim;

        // SSIM计算常量
        const float K1 = 0.01f;
        const float K2 = 0.03f;
        const float L = 255f;  // 假设图像像素值在[0, 255]

        public void Execute(int index)
        {
            var item = points[index];
            // 获取两个像素点的亮度（灰度值）
            int3 pixel = texturePixels[item.x + item.y * textureSize.x];
            float lum1 = GetLuminance(pixel);
            float lum2 = GetLuminance(new int3(TestEmbed(pixel.x), TestEmbed(pixel.y), TestEmbed(pixel.z)));

            // 在局部计算亮度均值、方差和协方差
            float meanX = lum1;
            float meanY = lum2;
            float varianceX = 0f;
            float varianceY = 0f;
            float covariance = 0f;

            // 计算亮度、方差和协方差
            for (int j = 0; j < points.Length; j++)
            {
                varianceX += math.pow(lum1 - meanX, 2);
                varianceY += math.pow(lum2 - meanY, 2);
                covariance += (lum1 - meanX) * (lum2 - meanY);
            }

            // 计算亮度项、对比度项、结构项
            float c1 = Mathf.Pow(K1 * L, 2);
            float c2 = Mathf.Pow(K2 * L, 2);

            float luminance = (2 * meanX * meanY + c1) / (meanX * meanX + meanY * meanY + c1);
            float contrast = (2 * Mathf.Sqrt(varianceX) * Mathf.Sqrt(varianceY) + c2) / (varianceX + varianceY + c2);
            float structure = (covariance + c2 / 2) / (Mathf.Sqrt(varianceX) * Mathf.Sqrt(varianceY) + c2 / 2);

            // SSIM值累加
            ssim[index] = luminance * contrast * structure;
        }

        int TestEmbed(int channel)
        {
            if (channel == 0)
                return 1;
            return channel - 1;
        }

        // 获取像素的亮度值（灰度值）
        float GetLuminance(int3 color)
        {
            // 使用Y = 0.299R + 0.587G + 0.114B计算亮度（灰度值）
            return (0.299f * color.x + 0.587f * color.y + 0.114f * color.z);
        }
    }

    [BurstCompile]
    struct SSIMSumJob : IJob
    {
        public NativeArray<float> ssim;

        public void Execute()
        {
            float sum = 0f;
            for (int i = 0; i < ssim.Length; i++)
                sum += ssim[i];
            ssim[0] = sum / ssim.Length;
        }
    }

    SSIMJob ssimJob;
    bool isSSIMJobBuild;

    // 计算两张图片的SSIM
    float CalculateSSIM(HashSet<int2> points)
    {
        // 初始化SSIM值
        if (!isSSIMJobBuild)
        {
            isSSIMJobBuild = true;
            ssimJob = new SSIMJob();
            ssimJob.textureSize = textureSize;
            ssimJob.texturePixels = texturePixels;
        }
        ssimJob.points = new NativeArray<int2>(points.ToArray(), Allocator.TempJob);
        ssimJob.ssim = new NativeArray<float>(points.Count, Allocator.TempJob);
        ssimJob.ScheduleParallel(points.Count, 250, default).Complete();
        ssimJob.points.Dispose();

        SSIMSumJob job = new SSIMSumJob();
        job.ssim = ssimJob.ssim;
        job.Schedule().Complete();
        float ssim = job.ssim[0];
        job.ssim.Dispose();

        // 返回平均SSIM值
        return ssim;
    }

    //public HashSet<int2> CreatePoints(float r1, float r2, float x0, float y0, int pixelCount, int width, int height)
    //{
    //    return Logistic(r1, r2, x0, y0, pixelCount, width, height, null);
    //}

    //public int2[] CreatePoints(float r1, float r2, float x0, float y0, int pixelCount, int width, int height, out float fitRate)
    //{
    //    HashSet<int2> alternate = new HashSet<int2>();
    //    for (int y = 0; y < height; y++)
    //    {
    //        for (int x = 0; x < width; x++) 
    //        {
    //            alternate.Add(new int2(x, y));
    //        }
    //    }

    //    var points = Logistic(r1, r2, x0, y0, pixelCount, width, height, alternate);
    //    fitRate = points.Count / (float)pixelCount;
    //    if (points.Count < pixelCount)
    //    {
    //        var alternateArray = alternate.ToArray();
    //        var delta = pixelCount - points.Count;
    //        var step = alternateArray.Length / delta;
    //        for (int i = 0; i < delta; i++)
    //            points.Add(alternateArray[i * step]);

    //        #region [ 高速版本 ]
    //        //for (int y = 0; y < height; y++) 
    //        //{
    //        //    for (int x = 0; x < width; x++) 
    //        //    {
    //        //        //if(points.Add(new int2(x, y)))
    //        //        //{
    //        //        //    delta--;
    //        //        //    if (delta <= 0)
    //        //        //        break;
    //        //        //}
    //        //    }
    //        //    if (delta <= 0)
    //        //        break;
    //        //}
    //        #endregion
    //    }
    //    //Debug.Log($"{points.Count} / {pixelCount}");
    //    return points.ToArray();
    //}

    public static HashSet<int2> Logistic(float4 logisticParams, int needPixelCount, int width, int height, HashSet<int2> exclusionPoints)
    {
        return Logistic(logisticParams.x, logisticParams.y, logisticParams.z, logisticParams.w, needPixelCount, width, height, exclusionPoints);
    }

    static float FormatFloat(float value)
    {
        return ((int)(value * 10000f)) / 10000f;
    }

    //[BurstCompile]
    //struct LogisticJob : IJob
    //{
    //    public NativeArray<int2> points;
    //    [ReadOnly] public int width;
    //    [ReadOnly] public int height;
    //    [ReadOnly] public float4 logisticParams;
    //    [ReadOnly] public int needPixelCount;

    //    public void Execute()
    //    {
    //        var r1 = logisticParams.x;
    //        var r2 = logisticParams.y;
    //        var x = logisticParams.z;
    //        var y = logisticParams.w;

    //        for (int i = 0; i < needPixelCount; i++)
    //        {
    //            // Logistic映射公式
    //            x = r1 * x * (1 - x);

    //            // Sine映射公式
    //            y = r2 * math.sin(math.PI * y);

    //            // 将x和y映射到图像的二维整型坐标
    //            int coordX = (int)math.floor(x * width);
    //            int coordY = (int)math.floor(y * height);

    //            // 确保坐标在图像范围内
    //            coordX = Mathf.Clamp(coordX, 0, width - 1);
    //            coordY = Mathf.Clamp(coordY, 0, height - 1);

    //            // 添加到随机坐标列表
    //            var point = new int2(coordX, coordY);
    //            points[i] = point;
    //        }
    //    }
    //}

    public static HashSet<int2> Logistic(float r1, float r2, float x, float y, int needPixelCount, int width, int height, HashSet<int2> exclusionPoints)
    {
        FormatFloat(r1);
        FormatFloat(r2);
        FormatFloat(x);
        FormatFloat(y);

        //LogisticJob job = new LogisticJob();
        //job.points = new NativeArray<int2>(needPixelCount, Allocator.TempJob);
        //job.width = width;
        //job.height = height;
        //job.logisticParams = new float4(r1, r2, x, y);
        //job.needPixelCount = needPixelCount;
        //job.Schedule().Complete();

        //for (int i = 0; i < job.points.Length; i++)
        //    points.Add(job.points[i]);

        //job.points.Dispose();

        HashSet<int2> points = new HashSet<int2>(needPixelCount);

        //// 控制参数
        //float r1 = 3.9f;  // Logistic映射参数
        //float r2 = 0.9f;  // Sine映射参数

        //// 初始值
        //float x = 0.5f;  // Logistic映射初始值
        //float y = 0.5f;  // Sine映射初始值

        for (int i = 0; i < needPixelCount; i++)
        {
            // Logistic映射公式
            x = r1 * x * (1 - x);

            // Sine映射公式
            y = r2 * math.sin(math.PI * y);

            // 将x和y映射到图像的二维整型坐标
            int coordX = (int)math.floor(x * width);
            int coordY = (int)math.floor(y * height);

            // 确保坐标在图像范围内
            coordX = math.clamp(coordX, 0, width - 1);
            coordY = math.clamp(coordY, 0, height - 1);

            // 添加到随机坐标列表
            var point = new int2(coordX, coordY);
            if (exclusionPoints == null || !exclusionPoints.Contains(point))
                points.Add(new int2(point));
        }
        return points;
    }

    public static HashSet<int2> SupplementLogistic(HashSet<int2> points, int needPixelCount, int width, int height, HashSet<int2> exclusionPoints)
    {
        if (points.Count < needPixelCount)
        {
            bool isOver = false;
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    var point = new int2(x, y);
                    if ((exclusionPoints == null || !exclusionPoints.Contains(point)) && points.Add(point))
                    {
                        if (points.Count >= needPixelCount)
                        {
                            isOver = true;
                            break;
                        }
                    }
                }
                if (isOver)
                    break;
            }
        }
        return points;
    }
}
