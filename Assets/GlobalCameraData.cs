using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using System.IO;
using System.Text;
public class GlobalCameraData : MonoBehaviour
{
    Camera thisCamera;
    public RenderTexture paintTex;
    Material drawingMaterial;
    public Material other;

    public ComputeShader computeShader, encodeShader, decodeShader;

    InputAction mousePosition, mouseClick, altAction, scrollWheel;

    CommandBuffer commandBuffer;
    bool captured = false;
    public bool draw = true;

    CameraData currentCamData;
    CameraData[] cameraDataArray = new CameraData[64];
    ComputeBuffer cameraDataBuffer;
    public Texture2D test;

    public Texture2DArray textureArray;
    int currentTextureCount;

    public static float currentColorValue;
    public Texture2D paintbrushTexture;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        thisCamera = this.GetComponent<Camera>();
        paintTex = new RenderTexture(960,540, 32, RenderTextureFormat.ARGBFloat) { enableRandomWrite = true, filterMode = FilterMode.Point, isPowerOfTwo = false, };
        
        drawingMaterial = new Material(Shader.Find("Custom/DrawingSRP"));

        mousePosition = InputSystem.actions.FindAction("Point");
        mouseClick = InputSystem.actions.FindAction("Click");
        altAction = InputSystem.actions.FindAction("AltAction");
        scrollWheel = InputSystem.actions.FindAction("ScrollWheel");
        altAction.started += ToggleDraw;
        Graphics.Blit(Texture2D.blackTexture, paintTex);
        
        cameraDataBuffer = new ComputeBuffer(64, sizeof(float) * 4 * 4 * 2);
        
        textureArray = new Texture2DArray(960,540, 64, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point};
        
        LoadAllTextures();

        foreach(CameraData c in cameraDataArray)
        {
            Debug.Log(c.TransformMatrix);
        }
    }

    void OnDisable()
    {
        paintTex.Release();
    }
    int kernelHandle;

    void ToggleDraw(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            Debug.Log("toggle");
            draw = !draw;
            if (draw)
            {
                Graphics.Blit(Texture2D.blackTexture, paintTex);
                cameraDataBuffer.GetData(cameraDataArray);
                Shader.SetGlobalInt("_IsDrawing", 1);
                cameraDataArray[currentTextureCount] = new CameraData(thisCamera);
                cameraDataBuffer.SetData(cameraDataArray);
                Shader.SetGlobalBuffer("_CameraDataBuffer", cameraDataBuffer);
                Shader.SetGlobalTexture("_CurrentPaintingTexture", paintTex);
                captured = false;
                FirstPersonDrifter.locked = true;
                MouseLook.locked = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Shader.SetGlobalInt("_IsDrawing", 0);
                FirstPersonDrifter.locked = false;
                MouseLook.locked = false;
                Cursor.lockState = CursorLockMode.Locked;
                SaveTexture(paintTex, currentTextureCount);
                
            }
            

        }
    }

    float brushSize = 0.5f;
    // Update is called once per frame
    void Update()
    {
        
        //Shader.SetGlobalMatrix("_CameraTransform", thisCamera.worldToCameraMatrix);
        //Shader.SetGlobalMatrix("_FrustrumMatrix", thisCamera.projectionMatrix);
        

        if (draw)
        {
            Vector2 mPos = mousePosition.ReadValue<Vector2>();
            float click = mouseClick.ReadValue<float>();
            brushSize -= scrollWheel.ReadValue<Vector2>().y * 0.01f;
            brushSize = Mathf.Clamp01(brushSize);
            Debug.Log(brushSize);
            Vector4 mouseData = new Vector4(mPos.x,mPos.y,click,1 + (brushSize * brushSize) * 15);

            kernelHandle = computeShader.FindKernel("CSMain");
            computeShader.SetTexture(kernelHandle,"Result", paintTex);
        
            computeShader.SetVector("_MouseData", mouseData);
            computeShader.SetFloat("_Color", currentColorValue);
            computeShader.SetTexture(kernelHandle, "_BrushTexture", paintbrushTexture);

            computeShader.Dispatch(kernelHandle, paintTex.width/8,paintTex.height/8,1);
            Shader.SetGlobalTexture("_CurrentPaintingTexture", paintTex);
            Shader.SetGlobalTexture("_BrushTexture", paintbrushTexture);
            Shader.SetGlobalVector("_MouseData", mouseData);

        }
    }

    Texture2D toTexture2D(RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point , alphaIsTransparency = false};
        // ReadPixels looks at the active RenderTexture.
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        return tex;
    }

    void SaveTexture(RenderTexture rt, int currentIndex)
    {
        int kH = encodeShader.FindKernel("Encode");
        encodeShader.SetTexture(kH,"Result", rt);
        encodeShader.SetBuffer(kH, "camData", cameraDataBuffer);
        encodeShader.SetInt("index", currentIndex);
        encodeShader.Dispatch(kH,1,1,1);
        Texture2D tex = toTexture2D(rt);
        Graphics.CopyTexture(tex, 0, textureArray, currentTextureCount);
        currentTextureCount++;
        File.WriteAllBytes(Application.dataPath + "/Pictures/Pic-" + Random.Range(1000,10000) + ".png", ImageConversion.EncodeToPNG(tex));
        Shader.SetGlobalBuffer("_CameraDataBuffer", cameraDataBuffer);
        Shader.SetGlobalTexture("_TextureArray", textureArray);
        Shader.SetGlobalInt("_TextureCount", currentTextureCount);
    }

    public void LoadAllTextures()
    {
        string[] pictures = Directory.GetFiles(Application.dataPath + "/Pictures/", "*.png", SearchOption.AllDirectories);
        int i = 0;

        cameraDataBuffer.SetData(cameraDataArray);

        foreach(string p in pictures)
        {
            byte[] bytes = File.ReadAllBytes(p);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point , alphaIsTransparency = false};
            ImageConversion.LoadImage(tex, bytes);
            LoadTexture(tex, i);
            Graphics.CopyTexture(tex, 0, textureArray, i);
            i++;
        }
        
        Shader.SetGlobalBuffer("_CameraDataBuffer", cameraDataBuffer);
        Shader.SetGlobalTexture("_TextureArray", textureArray);
        currentTextureCount = pictures.Length;
        Shader.SetGlobalInt("_TextureCount", currentTextureCount);
    }

    public void LoadTexture(Texture2D tex, int index)
    {
        int kH = decodeShader.FindKernel("Decode");
        decodeShader.SetTexture(kH,"Result", tex);
        decodeShader.SetBuffer(kH, "camData", cameraDataBuffer);
        decodeShader.SetInt("index", index);
        decodeShader.Dispatch(kH,1,1,1);
        
    }

    void OnRenderObject()
    {
        
        if (!captured)
        {
            computeShader.SetTexture(kernelHandle,"_CameraDepthTexture", Shader.GetGlobalTexture("_CameraDepthTexture"));
            captured = true;
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
