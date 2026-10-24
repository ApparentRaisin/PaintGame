using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

using Unity.Netcode;

public class GlobalCameraData : NetworkBehaviour
{
    Camera thisCamera;
    ComputeShader computeShader;
    InputAction mousePosition, mouseClick, altAction, scrollWheel;
    bool captured = false;
    bool draw = true;
    int playerNumber;
    static int playerCount;
    public static float currentColorValue;
    public Texture2D paintbrushTexture;
    float brushSize = 0.5f;
    int kernelHandle;
    static List<float> playerDrawing = new List<float>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        thisCamera = this.GetComponent<Camera>();
        
        if(!IsSpawned || !HasAuthority)
        {
            Destroy(thisCamera);
            Destroy(this);
            return;
        }
        mousePosition = InputSystem.actions.FindAction("Point");
        mouseClick = InputSystem.actions.FindAction("Click");
        altAction = InputSystem.actions.FindAction("AltAction");
        scrollWheel = InputSystem.actions.FindAction("ScrollWheel");
        altAction.started += ToggleDraw;
        
        computeShader = Resources.Load<ComputeShader>("ComputeShaders/DrawCompute");
        
        
        PaintController.LoadAllTextures(SceneManager.GetActiveScene().name);

        Cursor.lockState = CursorLockMode.Locked;

        playerCount++;
        playerDrawing.Add(0);
        Shader.SetGlobalInt("_PlayerCount", playerCount);
        Shader.SetGlobalFloatArray("_PlayerDrawing", playerDrawing);
    }
    
    void ToggleDraw(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            Debug.Log("toggle");
            draw = !draw;
            if (draw)
            {
                
                PaintController.StartPainting(thisCamera, playerNumber);
                playerDrawing[playerNumber] = 1;
                Shader.SetGlobalFloatArray("_PlayerDrawing", playerDrawing);
                captured = false;
                FirstPersonDrifter.locked = true;
                MouseLook.locked = true;
                Cursor.lockState = CursorLockMode.None;
                Time.timeScale = 0;
            }
            else
            {
                playerDrawing[playerNumber] = 0;
                Shader.SetGlobalFloatArray("_PlayerDrawing", playerDrawing);
                FirstPersonDrifter.locked = false;
                MouseLook.locked = false;
                Cursor.lockState = CursorLockMode.Locked;
                PaintController.SaveTexture(SceneManager.GetActiveScene().name, playerNumber);
                Time.timeScale = 1;
                
            }
            

        }
    }
    
    //toggling the capture so that only one depth textures i captured - this is weird that its needed - research more
    void Update()
    {
        if (draw)
        {
            captured = false;
        }
    }
    //Drawing effect - has to be done on render object to collect Camera Depth Texture
    void OnRenderObject()
    {
        if (!captured)
        {
            Vector2 mPos = mousePosition.ReadValue<Vector2>();
            float click = mouseClick.ReadValue<float>();
            brushSize -= scrollWheel.ReadValue<Vector2>().y * 0.01f;
            brushSize = Mathf.Clamp01(brushSize);
            
            Debug.Log(brushSize);
            Vector4 mouseData = new Vector4(mPos.x,mPos.y,click,1 + (brushSize * brushSize) * 15);

            kernelHandle = computeShader.FindKernel("CSMain");
            computeShader.SetTexture(kernelHandle,"Result", PaintController.GetCurrentPaintTex());
        
            computeShader.SetVector("_MouseData", mouseData);
            computeShader.SetFloat("_Color", currentColorValue);
            computeShader.SetTexture(kernelHandle, "_BrushTexture", paintbrushTexture);
            computeShader.SetFloat("_TimeUnscaled", Time.unscaledTime);
            computeShader.SetInt("_Player", playerNumber);
            
            computeShader.SetTexture(kernelHandle,"_CameraDepthTexture", Shader.GetGlobalTexture("_DepthTexture"));

            computeShader.Dispatch(kernelHandle, PaintController.GetTextureWidth()/8,PaintController.GetTextureHeight()/8,1);
            Shader.SetGlobalTexture("_CurrentPaintingTexture", PaintController.GetCurrentPaintTex());
            Shader.SetGlobalTexture("_BrushTexture", paintbrushTexture);
            Shader.SetGlobalVector("_MouseData", mouseData);
            
            captured = true;
        }
        
    }
}

