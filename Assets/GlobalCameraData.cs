using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using Unity.Netcode;

public class GlobalCameraData : NetworkBehaviour
{
    Camera thisCamera;
    ComputeShader computeShader;
    InputAction mousePosition, mouseClick, altAction, scrollWheel;
    bool captured = false;
    bool draw = false;
    int playerNumber;
    static int playerCount;
    public static float currentColorValue;
    public Texture2D paintbrushTexture;
    float brushSize = 0.5f;
    int kernelHandle;
    static float[] playerDrawing = new float[8];

    public bool singlePlayer;
    void Start()
    {
        thisCamera = this.GetComponent<Camera>();
        
        if((!IsSpawned || !HasAuthority) && !singlePlayer)
        {
            thisCamera.enabled = false;
            this.enabled = false;
            this.GetComponentInParent<FirstPersonDrifter>().enabled = singlePlayer;
            this.GetComponentInParent<MouseLook>().enabled = singlePlayer;
            this.GetComponent<MouseLook>().enabled = singlePlayer;
            return;
        }

        

        mousePosition = InputSystem.actions.FindAction("Point");
        mouseClick = InputSystem.actions.FindAction("Click");
        altAction = InputSystem.actions.FindAction("AltAction");
        scrollWheel = InputSystem.actions.FindAction("ScrollWheel");
        altAction.started += ToggleDraw;
        
        computeShader = Resources.Load<ComputeShader>("ComputeShaders/DrawCompute");
        if(singlePlayer || NetworkManager?.LocalClientId == NetworkManager.CurrentSessionOwner)
        {
            PaintController.LoadAllTextures(ConnectionManager.sessionName,SceneManager.GetActiveScene().name);
        }
        else
        {
            PaintController.SetupNoLoad(ConnectionManager.sessionName,SceneManager.GetActiveScene().name);
        }
        

        Cursor.lockState = CursorLockMode.Locked;

        if (!singlePlayer)
        {
            playerNumber = (int)NetworkManager.LocalClientId - 1;
            NetworkManager.OnClientConnectedCallback += OnConnection;
        }
        else
        {
            playerNumber = 0;
        }

        Shader.SetGlobalInt("_PlayerCount", playerCount);
        Shader.SetGlobalInt("_Player", playerNumber);
        Shader.SetGlobalFloatArray("_PlayerDrawing", playerDrawing);

    }

    public void OnConnection(ulong clientID)
    {
        if(clientID != NetworkManager.LocalClientId)
        {
            SendCameraDataRPC(playerNumber, playerDrawing[playerNumber], new CameraData(thisCamera));
            if(NetworkManager.LocalClientId == NetworkManager.CurrentSessionOwner)
            {
                PaintController.SendAllTextures(ConnectionManager.sessionName,SceneManager.GetActiveScene().name);
            }            
        }
    }

    [Rpc(SendTo.NotMe)]
    void SendCameraDataRPC(int player, float drawing, CameraData data)
    {
        Debug.Log("got " + data.TransformMatrix + "from player " + player);
        playerDrawing[player] = drawing;
        PaintController.SetCurrentCameraData(data, player);
        Shader.SetGlobalFloatArray("_PlayerDrawing", playerDrawing);
    }
    
    void ToggleDraw(InputAction.CallbackContext context)
    {
        if((!IsSpawned || !HasAuthority) && !singlePlayer)
        {
            return;
        }

        if (context.phase == InputActionPhase.Started)
        {
            Debug.Log("toggle");
            draw = !draw;
            if (draw)
            {
                playerDrawing[playerNumber] = 1;
                PaintController.StartPainting(thisCamera, playerNumber);
                if(!singlePlayer)
                    SendCameraDataRPC(playerNumber, 1, new CameraData(thisCamera));
                
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
                if(!singlePlayer)
                    SendCameraDataRPC(playerNumber, 0, new CameraData(thisCamera));
                Shader.SetGlobalFloatArray("_PlayerDrawing", playerDrawing);
                FirstPersonDrifter.locked = false;
                MouseLook.locked = false;
                Cursor.lockState = CursorLockMode.Locked;
                PaintController.SaveTexture(ConnectionManager.sessionName,SceneManager.GetActiveScene().name, playerNumber, singlePlayer);
                Time.timeScale = 1;
            }
            

        }
    }
    
    //toggling the capture so that only one depth textures i captured - this is weird that its needed - research more
    float nT = 1.2f;
    void Update()
    {
        if((!IsSpawned || !HasAuthority) && !singlePlayer)
        {
            return;
        }

        if (draw)
        {
            captured = false;
            
        }
    }
    //Drawing effect - has to be done on render object to collect Camera Depth Texture
    void OnRenderObject()
    {
        if((!IsSpawned || !HasAuthority) && !singlePlayer)
        {
            return;
        }
        
        if (!captured)
        {
            Vector2 mPos = mousePosition.ReadValue<Vector2>();
            float click = mouseClick.ReadValue<float>();
            brushSize -= scrollWheel.ReadValue<Vector2>().y * 0.01f;
            brushSize = Mathf.Clamp01(brushSize);
            
            Debug.Log(brushSize);
            Vector4 mouseData = new Vector4(mPos.x,mPos.y,click,1f + (brushSize * brushSize) * 50);

            kernelHandle = computeShader.FindKernel("CSMain");
            computeShader.SetTexture(kernelHandle,"Result", PaintController.GetCurrentPaintTex());
        
            computeShader.SetVector("_MouseData", mouseData);
            computeShader.SetFloat("_Color", currentColorValue);
            computeShader.SetTexture(kernelHandle, "_BrushTexture", paintbrushTexture);
            computeShader.SetFloat("_TimeUnscaled", Time.unscaledTime);
            computeShader.SetInt("_Player", playerNumber);
            
            computeShader.SetTexture(kernelHandle,"_CameraDepthTexture", Shader.GetGlobalTexture("_DepthTexture"));
            computeShader.SetTexture(kernelHandle,"_LayerTexture", Shader.GetGlobalTexture("_LayerTexture"));

            computeShader.Dispatch(kernelHandle, PaintController.GetTextureWidth()/8,PaintController.GetTextureHeight()/8,1);
            Shader.SetGlobalTexture("_CurrentPaintingTexture", PaintController.GetCurrentPaintTex());
            Shader.SetGlobalTexture("_BrushTexture", paintbrushTexture);
            Shader.SetGlobalVector("_MouseData", mouseData);

            captured = true;

            if(singlePlayer)
                return;
            
            if(Time.unscaledTime+(playerNumber/8f) > nT)
            {
                Debug.Log("paint tick");
                nT = Time.unscaledTime+(playerNumber/8f) + 0.1f;
                TextureSend.instance.SendTexture(PaintController.GetCurrentPaintTex(), 960, 540, playerNumber);
            }
        }
        
    }
}

