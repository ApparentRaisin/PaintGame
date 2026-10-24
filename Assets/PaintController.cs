using UnityEngine;
using System.IO;
using Unity.Netcode.Components;
public class PaintController : MonoBehaviour
{
    static ComputeShader encodeShader, decodeShader;
    static ComputeBuffer cameraDataBuffer, movingTransformBuffer;
    static Texture2DArray textureArray;
    static int currentTextureCount = 0;
    static CameraData[] cameraDataArray = new CameraData[128];
    static Matrix4x4[] movingObjectTransforms = new Matrix4x4[128 * 60];
    static bool primed;
    static RenderTexture paintTex;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    static void Prime()
    {
        if(primed == false)
        {
            encodeShader = Resources.Load<ComputeShader>("ComputeShaders/Encode");
            decodeShader = Resources.Load<ComputeShader>("ComputeShaders/Decode");

            cameraDataBuffer = new ComputeBuffer(128, sizeof(float) * 4 * 4 * 2);
            movingTransformBuffer = new ComputeBuffer(128 * 60, sizeof(float) * 4 * 4);

            textureArray = new Texture2DArray(960,540, 128, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point, };

            paintTex = new RenderTexture(960,540, 32, RenderTextureFormat.ARGBFloat) { enableRandomWrite = true, filterMode = FilterMode.Point, isPowerOfTwo = false, dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray};
            Graphics.Blit(Texture2D.blackTexture, paintTex);
            primed = true;
        }
    }

    void OnDisable()
    {
        paintTex.Release();
    }
    static Texture2D toTexture2D(RenderTexture rTex, out byte[] png)
    {
        Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point};
        // ReadPixels looks at the active RenderTexture.
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        png = ImageConversion.EncodeToPNG(tex);
        return tex;
    }

    public static Texture2D PngPrep(RenderTexture rt, int player, out byte[] bytes)
    {
        RenderTexture tempTex = RenderTexture.GetTemporary(rt.width,rt.height, 32, RenderTextureFormat.ARGBFloat);
        tempTex.enableRandomWrite = true;
        Graphics.CopyTexture(rt, player, tempTex, 0);
        int kH = encodeShader.FindKernel("Encode");
        encodeShader.SetTexture(kH,"Result", tempTex);
        encodeShader.SetBuffer(kH, "camData", cameraDataBuffer);
        encodeShader.SetBuffer(kH, "movingTransforms", movingTransformBuffer);
        encodeShader.SetInt("index", currentTextureCount);
        encodeShader.Dispatch(kH,1,1,1);
        
        Texture2D tex = toTexture2D(tempTex, out bytes);
        RenderTexture.ReleaseTemporary(tempTex);
        return tex;
    }
    public static void SaveTexture(string sceneName, int player)
    {
        Prime();
        byte[] bytes;
        Graphics.CopyTexture(PngPrep(paintTex, player, out bytes), 0, textureArray, currentTextureCount);
        currentTextureCount++;
        File.WriteAllBytes(Application.dataPath + "/Pictures/"+sceneName+"/Pic-" + UnityEngine.Random.Range(1000,10000) + ".png", bytes);
        Shader.SetGlobalBuffer("_CameraDataBuffer", cameraDataBuffer);
        Shader.SetGlobalBuffer("_MovingTransfromBuffer", movingTransformBuffer);
        Shader.SetGlobalTexture("_TextureArray", textureArray);
        Shader.SetGlobalInt("_TextureCount", currentTextureCount);
    }

    public static RenderTexture GetCurrentPaintTex()
    {
        return paintTex;
    }

    public static int GetTextureWidth()
    {
        return paintTex.width;
    }

    public static int GetTextureHeight()
    {
        return paintTex.height;
    }
    public static void LoadAllTextures(string sceneName)
    {
        Prime();
        Directory.CreateDirectory(Application.dataPath + "/Pictures/"+sceneName+"/");
        string[] pictures = Directory.GetFiles(Application.dataPath + "/Pictures/"+sceneName+"/", "*.png", SearchOption.AllDirectories);
        int i = 0;

        cameraDataBuffer.SetData(cameraDataArray);
        movingTransformBuffer.SetData(movingObjectTransforms);

        foreach(string p in pictures)
        {
            byte[] bytes = File.ReadAllBytes(p);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point};
            ImageConversion.LoadImage(tex, bytes);
            AddTextureToArray(tex, textureArray, i);
            i++;
        }

        currentTextureCount = i;

        Shader.SetGlobalBuffer("_CameraDataBuffer", cameraDataBuffer);
        Shader.SetGlobalBuffer("_MovingTransforms", movingTransformBuffer);
        Shader.SetGlobalTexture("_TextureArray", textureArray);
        Shader.SetGlobalInt("_TextureCount", currentTextureCount);

        cameraDataBuffer.GetData(cameraDataArray);
        Debug.Log(cameraDataArray[0].TransformMatrix);
        Debug.Log(cameraDataArray[1].TransformMatrix);
        
    }   
    static void AddTextureToArray(Texture2D tex, Texture2DArray array, int index)
    {
        LoadTexture(tex, index);
        Graphics.CopyTexture(tex, 0, textureArray, index);
    }
    static void LoadTexture(Texture2D tex, int index)
    {
        int kH = decodeShader.FindKernel("Decode");
        decodeShader.SetTexture(kH,"Result", tex);
        decodeShader.SetBuffer(kH, "camData", cameraDataBuffer);
        decodeShader.SetBuffer(kH, "movingTransforms", movingTransformBuffer);
        decodeShader.SetInt("index", index);
        decodeShader.Dispatch(kH,1,1,1);
        
    }
    public static void StartPainting(Camera camera, int playerNumber)
    {
        Graphics.Blit(Texture2D.blackTexture, paintTex, 0, playerNumber);
        cameraDataBuffer.GetData(cameraDataArray);
        movingTransformBuffer.GetData(movingObjectTransforms);
        cameraDataArray[currentTextureCount + playerNumber] = new CameraData(camera);
        CollectTransforms();
        cameraDataBuffer.SetData(cameraDataArray);
        movingTransformBuffer.SetData(movingObjectTransforms);
        Shader.SetGlobalBuffer("_CameraDataBuffer", cameraDataBuffer);
        Shader.SetGlobalBuffer("_MovingTransforms", movingTransformBuffer);
        Shader.SetGlobalTexture("_CurrentPaintingTexture", paintTex);
    }
    static void CollectTransforms()
    {
        int start = currentTextureCount * 60;
        Matrix4x4[] mats = MovingTransformsController.instance.GetObjectTransforms();
        for(int i = 0; i < mats.Length; i++)
        {
            movingObjectTransforms[start + i] = mats[i];
        }
    }
}
public struct CameraData
{
    public Matrix4x4 TransformMatrix;
    public Matrix4x4 ProjectionMatrix;
    public CameraData(Camera camera)
    {
        this.TransformMatrix = camera.worldToCameraMatrix;
        this.ProjectionMatrix = camera.projectionMatrix;
    }
}
