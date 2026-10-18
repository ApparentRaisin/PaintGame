//using System.Collections.Generic;
//using System.Linq;
//using Unity.Burst;
//using Unity.Collections;
//using Unity.Jobs;
//using Unity.Mathematics;
//using UnityEngine;

//public class Whale
//{
//    public NativeArray<int2> positions;
//    public float fitness;
//    public float blance; //平衡因子

//    public float psnr;

//    public Whale()
//    {
//        this.fitness = float.MinValue;
//    }
//}

//public class BWO
//{
//    public int loopBatchCount = 1000;

//    NativeArray<int3> texturePixels;
//    int2 textureSize;
//    uint seed;
//    Unity.Mathematics.Random globalRandom;

//    public List<Whale> whales;
//    public int bestWhaleIndex;
//    public Whale bestWhale { get { return whales[bestWhaleIndex]; } }

//    public void Init(Texture2D texture)
//    {
//        this.textureSize = new int2(texture.width, texture.height);
//        this.texturePixels = new NativeArray<int3>(textureSize.x * textureSize.y, Allocator.Persistent);
//        for (int y = 0; y < this.textureSize.y; y++)
//        {
//            for (int x = 0; x < this.textureSize.x; x++)
//            {
//                var c = texture.GetPixel(x, y);
//                this.texturePixels[x + y * textureSize.x] = new int3(
//                    (int)math.floor(c.r * 255f),
//                    (int)math.floor(c.g * 255f),
//                    (int)math.floor(c.b * 255f)
//                    );
//            }
//        }
//    }

//    //[BurstCompile]
//    struct GeneratePointsJob : IJobFor
//    {
//        [ReadOnly] public int2 textureSize;
//        public NativeArray<int2> positions;
//        [ReadOnly] public Unity.Mathematics.Random random;

//        public void Execute(int index)
//        {
//            positions[index] = new int2(random.NextInt(0, textureSize.x), random.NextInt(0, textureSize.y));
//        }
//    }

//    public HashSet<int2> Run(int jobCount, float embeddingRate, int maxIterations, int needPixelCount = -1, uint seed = 0)
//    {
//        if (seed == 0)
//            seed = (uint)(Time.realtimeSinceStartup * 10);
//        this.seed = seed;
//        globalRandom = new Unity.Mathematics.Random(seed);

//        whales = new List<Whale>(jobCount);

//        int embedPixelCount = (int)(embeddingRate * (textureSize.x * textureSize.y));
//        //if (needPixelCount > 0)
//        //    embedPixelCount = math.min(needPixelCount, embedPixelCount);
//        GeneratePointsJob generatePointsJob = new GeneratePointsJob();
//        generatePointsJob.textureSize = textureSize;
//        for (int i = 0; i < jobCount; i++)
//        {
//            Whale whale = new Whale();
//            whales.Add(whale);
//            generatePointsJob.random = new Unity.Mathematics.Random(globalRandom.NextUInt());
//            generatePointsJob.positions = whale.positions = new NativeArray<int2>(embedPixelCount, Allocator.Persistent);
//            generatePointsJob.ScheduleParallel(embedPixelCount, loopBatchCount, default).Complete();
//        }

//        for (int t = 0; t < maxIterations; t++)
//        {
//            // 更新鲸鱼位置
//            for (int i = 0; i < whales.Count; i++)
//            {
//                whales[i].blance = globalRandom.NextFloat() * (1 - t / (2f * maxIterations));
//                MoveWhale(i, t, maxIterations);
//            }

//            // 鲸落
//            float wf = 0.1f - 0.05f * (t / (float)maxIterations); // 鲸落概率
//            for (int i = 0; i < whales.Count; i++)
//            {
//                if (whales[i].blance <= wf)
//                    FallWhale(i, wf, t, maxIterations);
//            }
//        }

//        // 处理重叠节点
//        HashSet<int2> points = new HashSet<int2>();
//        var positions = whales[bestWhaleIndex].positions;
//        for (int i = 0; i < positions.Length; i++)
//            points.Add(positions[i]);

//        while(points.Count < embedPixelCount)
//        {
//            int2 point = new int2(globalRandom.NextInt(0, textureSize.x), globalRandom.NextInt(0, textureSize.y));
//            while (!points.Add(point))
//                point = new int2(globalRandom.NextInt(0, textureSize.x), globalRandom.NextInt(0, textureSize.y));
//        }
//        return points;
//    }

//    //[BurstCompile]
//    struct ExploreJob : IJobFor
//    {
//        [ReadOnly] public int2 textureSize;
//        [ReadOnly] public NativeArray<int2> randomWhalePositions;
//        public NativeArray<int2> positions;
//        [ReadOnly] public Unity.Mathematics.Random random;

//        public void Execute(int index)
//        {
//            var r1 = random.NextFloat(0f, 1f);
//            var r2 = random.NextFloat(0f, 1f);

//            var offset = randomWhalePositions[index] - positions[index];
//            int2 point;
//            point.x = (int)(positions[index].x + offset.x * (1 + r1) * Mathf.Sin(2f * Mathf.PI * r2));
//            point.y = (int)(positions[index].y + offset.y * (1 + r1) * Mathf.Cos(2f * Mathf.PI * r2));
//            // 限制边界
//            point.x = math.clamp(point.x, 0, textureSize.x - 1);
//            point.y = math.clamp(point.y, 0, textureSize.y - 1);
//            positions[index] = point;
//        }
//    }

//    //[BurstCompile]
//    struct ExploitJob : IJobFor
//    {
//        [ReadOnly] public int2 textureSize;
//        [ReadOnly] public NativeArray<int2> bestWhalePositions;
//        public NativeArray<int2> positions;
//        [ReadOnly] public Unity.Mathematics.Random random;

//        public void Execute(int index)
//        {
//            var offset = bestWhalePositions[index] - positions[index];
//            var offsetFloat = new Vector2(offset.x, offset.y).normalized * random.NextFloat(0, 2);
//            var point = positions[index] + offset + new int2(random.NextInt(-1, 1) + (int)offsetFloat.x, random.NextInt(-1, 1) + (int)offsetFloat.y);
//            // 限制边界
//            point.x = math.clamp(point.x, 0, textureSize.x - 1);
//            point.y = math.clamp(point.y, 0, textureSize.y - 1);
//            positions[index] = point;
//        }
//    }

//    ExploreJob exploreJob;
//    bool isExploreJobBuild;
//    ExploitJob exploitJob;
//    bool isExploitJobBuild;

//    void MoveWhale(int i, int t, int maxIterations)
//    {
//        var whale = whales[i];
//        var positions = new NativeArray<int2>(whale.positions, Allocator.TempJob);

//        if (whale.blance > 0.5f)
//        {
//            // ---- 探索阶段 ---- //
//            // 随机选择1只白鲸
//            int r = globalRandom.NextInt(0, whales.Count);
//            if (i == r) r = (r + 1) % whales.Count;

//            if (!isExploreJobBuild)
//            {
//                isExploreJobBuild = true;
//                exploreJob = new ExploreJob();
//                exploreJob.textureSize = textureSize;
//            }
//            exploreJob.randomWhalePositions = whale.positions;
//            exploreJob.positions = positions;
//            exploreJob.random = new Unity.Mathematics.Random(globalRandom.NextUInt());
//            exploreJob.ScheduleParallel(positions.Length, loopBatchCount, default).Complete();
//        }
//        else
//        {
//            // ---- 开发阶段 ---- //
//            // 网格地图并不适合莱特飞行策略，莱特飞行策略是指数级扩大每次行走的步长，所以可能会出现穿越障碍物的情况
//            #region [ 使用莱特飞行策略 ]
//            //float r3 = Random.Range(MIN_PRECISION, 1f);
//            //float r4 = Random.Range(MIN_PRECISION, 1f);

//            //// β = beta
//            //float beta = 1.5f;
//            //// δ = delta, Γ = Gamma()
//            //float delta = Mathf.Pow((Gamma(1 + beta) * Mathf.Sin((Mathf.PI * beta) / 2)) / (Gamma((1 + beta) / 2) * beta * Mathf.Pow(2, (beta - 1) / 2)), 1 / beta);

//            //Vector2 LF;
//            //Vector2 u = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
//            //Vector2 v = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
//            //LF.x = 0.05f * ((u.x * delta) / Mathf.Pow(Mathf.Abs(v.x), 1f / beta));
//            //LF.y = 0.05f * ((u.y * delta) / Mathf.Pow(Mathf.Abs(v.y), 1f / beta));
//            //Debug.Log(LF);

//            //// 随机选择1只白鲸
//            //int r = Random.Range(0, whales.Length);
//            //if (i == r) r = (r + 1) % whales.Length;

//            //var bestWhale = whales[bestWhaleIndex];
//            //float c1 = 2 * r4 * (1 - (t / (float)maxIterations));
//            //whale.nextPosition = r3 * bestWhale.lastPosition - r4 * whale.lastPosition + c1 * LF;
//            #endregion

//            if (!isExploitJobBuild)
//            {
//                isExploitJobBuild = true;
//                exploitJob = new ExploitJob();
//                exploitJob.textureSize = textureSize;
//            }
//            exploitJob.bestWhalePositions = whales[bestWhaleIndex].positions;
//            exploitJob.positions = positions;
//            exploitJob.random = new Unity.Mathematics.Random(globalRandom.NextUInt());
//            exploitJob.ScheduleParallel(positions.Length, loopBatchCount, default).Complete();
//        }

//        // 计算鲸鱼的适应度
//        //CountFitnessTest(positions, out var psnr2);
//        var fitness = CountFitness(positions, out var psnr);
//        if (fitness > whale.fitness)
//        {
//            //Debug.LogError($"BestChange: {bestWhaleIndex} → {i}\n{bestWhale.psnr} = {psnr}");
//            whale.psnr = psnr;
//            positions.CopyTo(whale.positions);
//            //whale.positions = positions;
//            whale.fitness = fitness;
//            if (fitness > whales[bestWhaleIndex].fitness)
//                bestWhaleIndex = i;
//        }
//        positions.Dispose();
//    }


//    //[BurstCompile]
//    struct FallWhaleJob : IJobFor
//    {
//        [ReadOnly] public int2 textureSize;
//        [ReadOnly] public int2 step;
//        [ReadOnly] public NativeArray<int2> randomWhalePositions;
//        public NativeArray<int2> positions;
//        [ReadOnly] public Unity.Mathematics.Random random;

//        public void Execute(int index)
//        {
//            var r5 = random.NextFloat(0.3f, 1f);
//            var r6 = random.NextFloat(0.3f, 1f);
//            var r7 = random.NextFloat(0.3f, 1f);
//            step.x = (int)(step.x * r7);
//            step.y = (int)(step.y * r7);
//            var point = step + new int2((int)(positions[index].x * r5 - randomWhalePositions[index].x * r6), (int)(positions[index].y * r5 - randomWhalePositions[index].y * r6));
//            // 限制边界
//            point.x = math.clamp(point.x, 0, textureSize.x - 1);
//            point.y = math.clamp(point.y, 0, textureSize.y - 1);
//            positions[index] = point;
//        }
//    }

//    FallWhaleJob fallWhaleJob;
//    bool isFallWhaleJobBuild;

//    void FallWhale(int i, float fallProbability, int t, int maxIterations)
//    {
//        var whale = whales[i];
//        var positions = new NativeArray<int2>(whale.positions, Allocator.TempJob);

//        var c2 = 2 * fallProbability * whales.Count;
//        var exp = Mathf.Exp((-c2 * t) / (float)maxIterations);
//        int2 step = new int2((int)(textureSize.x * exp), (int)(textureSize.y * exp));

//        // 随机选择1只白鲸
//        int r = globalRandom.NextInt(0, whales.Count);
//        if (i == r) r = (r + 1) % whales.Count;
//        if (!isFallWhaleJobBuild)
//        {
//            isFallWhaleJobBuild = true;
//            fallWhaleJob = new FallWhaleJob();
//            fallWhaleJob.textureSize = textureSize;
//        }
//        fallWhaleJob.step = step;
//        fallWhaleJob.randomWhalePositions = whales[r].positions;
//        exploitJob.positions = positions;
//        fallWhaleJob.random = new Unity.Mathematics.Random(globalRandom.NextUInt());

//        // 计算鲸鱼的适应度
//        var fitness = CountFitness(positions, out var psnr);
//        if (fitness > whale.fitness)
//        {
//            //Debug.LogError($"BestChange: {bestWhaleIndex} → {i}\n{bestWhale.psnr} = {psnr}");
//            whale.psnr = psnr;
//            positions.CopyTo(whale.positions);
//            //whale.positions = positions;
//            whale.fitness = fitness;
//            if (fitness > whales[bestWhaleIndex].fitness)
//                bestWhaleIndex = i;
//        }
//    }


//    //[BurstCompile]
//    struct MSEPowJob : IJobFor
//    {
//        [ReadOnly] public int2 textureSize;
//        [ReadOnly] public NativeArray<int3> texturePixels;
//        [ReadOnly] public NativeArray<int2> positions;
//        public NativeArray<float> powArray;

//        public void Execute(int index)
//        {
//            var pixel = texturePixels[positions[index].x + positions[index].y * textureSize.x];
//            int r1 = TestEmbed(pixel.x);
//            int g1 = TestEmbed(pixel.y);
//            int b1 = TestEmbed(pixel.z);
//            var pow = math.pow((pixel.x - r1), 2) +
//                      math.pow((pixel.y - g1), 2) +
//                      math.pow((pixel.z - b1), 2);
//            powArray[index] = pow;
//        }

//        int TestEmbed(int channel)
//        {
//            if (channel == 0)
//                return 1;
//            return channel - 1;
//        }
//    }

//    //[BurstCompile]
//    struct PSNRJob : IJob
//    {
//        [ReadOnly] public int2 textureSize;
//        [ReadOnly] public NativeArray<float> powArray;
//        public NativeArray<float> result;

//        public void Execute()
//        {
//            float mse = 0f;
//            for (int i = 0; i < powArray.Length; i++)
//                mse += powArray[i];
//            mse /= textureSize.x * textureSize.y * 3;

//            if (mse == 0)
//                result[0] = float.MinValue;
//            else
//                //result[0] = 20f * math.log10(255f / math.sqrt(mse));  // PSNR 公式
//                result[0] = 10 * Mathf.Log10((255 * 255) / mse);
//        }
//    }

//    MSEPowJob msePowJob;
//    bool isMSEPowJobBuild;

//    float CountFitness(NativeArray<int2> positions, out float psnr)
//    {
//        HashSet<int2> repeatPoint = new HashSet<int2>();
//        for (int i = 0; i < positions.Length; i++)
//            repeatPoint.Add(positions[i]);
//        positions = new NativeArray<int2>(repeatPoint.ToArray(), Allocator.TempJob);

//        if (!isMSEPowJobBuild)
//        {
//            isMSEPowJobBuild = true;
//            msePowJob = new MSEPowJob();
//            msePowJob.textureSize = textureSize;
//            msePowJob.texturePixels = texturePixels;
//        }
//        msePowJob.positions = positions;
//        msePowJob.powArray = new NativeArray<float>(positions.Length, Allocator.TempJob);
//        msePowJob.ScheduleParallel(positions.Length, loopBatchCount, default).Complete();

//        PSNRJob job = new PSNRJob();
//        job.textureSize = textureSize;
//        job.powArray = msePowJob.powArray;
//        job.result = new NativeArray<float>(1, Allocator.TempJob);
//        job.Schedule().Complete();
//        job.powArray.Dispose();

//        psnr = job.result[0];
//        job.result.Dispose();
//        return psnr + (repeatPoint.Count / positions.Length) * 100;
//    }

//    //float CountFitnessTest(NativeArray<int2> positions, out float psnr)
//    //{
//    //    HashSet<int2> repeatPoint = new HashSet<int2>();
//    //    for (int i = 0; i < positions.Length; i++)
//    //        repeatPoint.Add(positions[i]);

//    //    int TestEmbed(int channel)
//    //    {
//    //        if (channel == 0)
//    //            return 1;
//    //        return channel - 1;
//    //    }

//    //    float mse = 0f;
//    //    foreach (var item in repeatPoint)
//    //    {
//    //        var pixel = texturePixels[item.x + item.y * textureSize.x];
//    //        int r1 = TestEmbed(pixel.x);
//    //        int g1 = TestEmbed(pixel.y);
//    //        int b1 = TestEmbed(pixel.z);
//    //        mse += math.pow((pixel.x - r1), 2) +
//    //                  math.pow((pixel.y - g1), 2) +
//    //                  math.pow((pixel.z - b1), 2);
//    //    }

//    //    mse /= textureSize.x * textureSize.y * 3;
//    //    if (mse == 0) return psnr = float.MinValue;

//    //    psnr = 10 * Mathf.Log10((255 * 255) / mse);  // PSNR 公式
//    //    //Debug.LogError($"psnr = {psnr}");
//    //    return psnr + (repeatPoint.Count / positions.Length) * 100;
//    //}
//}
