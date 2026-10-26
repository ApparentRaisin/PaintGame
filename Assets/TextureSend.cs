using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.Runtime.InteropServices;
using System;
public class TextureSend : NetworkBehaviour
{
    private byte[] data; // Byte array to hold the texture data
    private int dataSize
    {
        get
        {
            return data.Length;
        }
    }

    private void Start()
    {
        // Bind the handler after NetworkManager initializes
        NetworkManager.Singleton.OnClientConnectedCallback += OnNetworkReady;
        instance = this;
    }

    private void OnNetworkReady(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            // Register your global callback for receiving unnamed messages
            NetworkManager.Singleton.CustomMessagingManager.OnUnnamedMessage += OnUnnamedMessage;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null)
        {
            NetworkManager.Singleton.CustomMessagingManager.OnUnnamedMessage -= OnUnnamedMessage;
        }
    }

    public static TextureSend instance;

    AsyncGPUReadbackRequest req;
    public async void SendTexture(RenderTexture tex, int width, int height, int player)
    {
        if(!req.done)
            return;
        
        var buffer = new NativeArray<byte>(tex.width * tex.height * 4, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        req = await AsyncGPUReadback.RequestIntoNativeArrayAsync(ref buffer, tex, 0, 0, tex.width, 0, tex.height, player, 1, TextureFormat.RGBA32);
        if (req.hasError)
        {
            Debug.Log("GPU readback error detected.");
        }
        await Awaitable.BackgroundThreadAsync();
            using var encoded = ImageConversion.EncodeNativeArrayToPNG(buffer, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, (uint)width, (uint)height);
        await Awaitable.MainThreadAsync();
        
        SendData(encoded.ToArray());
    }

    public void SendData(byte[] bytes)
    {
        Debug.Log(NetworkManager.Singleton.LocalClientId);
        
        if(NetworkManager.Singleton)
        data = bytes;
        SendData(NetworkManager.ServerClientId, data);
    }

    private void OnUnnamedMessage(ulong clientId, FastBufferReader reader)
    {
        int dataLength = reader.Length; // Get the length of the received data
        Debug.Log("OnUnnamedMessage reader length: " + dataLength); // Log the length of the received data

        data = new byte[dataLength]; // Initialize the data array with the received data length
        reader.ReadBytesSafe(ref data, dataLength); // Read the bytes from the reader into the data array

        // Recreate the texture from the byte array
        Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false, true); // Create a new texture with default size
        //Message m = fromBytes(data);
        texture.LoadImage(data); // Load the texture from the byte array

        Vector4 c = texture.GetPixel(959,0);
        int player = Mathf.FloorToInt(c.x*5f+0.5f);
        if(player == 0)
        {
            Debug.Log("Save");
            PaintController.SaveTexture(ConnectionManager.sessionName, SceneManager.GetActiveScene().name, texture, data);
        }
        else
        {
            Debug.Log("set active");
            PaintController.SetPaintTex(texture, player-1);
        }
            
    }

    private void SendData(ulong receivingClientId, byte[] data)
    {
        if (receivingClientId == NetworkManager.Singleton.LocalClientId) return;

        var writer = new FastBufferWriter(dataSize, Allocator.Temp); // Create a new FastBufferWriter with the data size
        var customMessagingManager = NetworkManager.Singleton.CustomMessagingManager; // Get the CustomMessagingManager
        
        using (writer)
        {
            writer.WriteBytesSafe(data); // Write the data bytes to the writer
                                         // Send the data to the connected client with ReliableFragmentedSequenced delivery
            customMessagingManager.SendUnnamedMessage(receivingClientId, writer, NetworkDelivery.ReliableFragmentedSequenced);
        }
    }
}
