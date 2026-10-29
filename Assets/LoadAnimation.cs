using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class LoadAnimation : MonoBehaviour
{
    RenderTexture tex;
    public ComputeShader shader;
    RawImage image; 
    public Texture2D brush;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tex = new RenderTexture(960,540, 0) {enableRandomWrite = true};
        Graphics.Blit(Texture2D.whiteTexture, tex);
        image = this.GetComponent<RawImage>();
        image.texture = tex;
        //image.material.SetTexture("_BaseMap", tex);
        StartCoroutine(LoadingAnimationCO());
    }

    void OnDestroy()
    {
        tex.Release();
    }

    public void Load()
    {
        
    }

    IEnumerator LoadingAnimationCO()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        int kernel = shader.FindKernel("CSMain");
        float time = 0;
        shader.SetTexture(kernel, "Result", tex);
        shader.SetTexture(kernel, "_BrushTexture", brush);
        Vector2 pos = new Vector2(10,10);
        Vector2 velocity = new Vector2(2f,-1);
        do
        {
            Debug.Log(time);
            time += 0.01f;
            
            shader.SetFloat("_UnscaledTime", time);
            shader.SetVector("_Position", new Vector4(pos.x, pos.y, -1, 1f));
            shader.Dispatch(kernel,tex.width/8,tex.height/8,1);
            image.texture = tex;

            pos += velocity * 0.01f * 2048;
            
            if(pos.x < 0 || pos.x > Screen.width || pos.y < 0 || pos.y > Screen.height)
            {
                pos.x = pos.x < 0? 0 : pos.x;
                pos.y = pos.y < 0? 0 : pos.y;
                pos.x = pos.x > Screen.width? Screen.width : pos.x;
                pos.y = pos.y > Screen.height? Screen.height : pos.y;
                velocity.y = -velocity.y;
                velocity.x = velocity.x < 0 ? 2f : -1f;
            }
            yield return new WaitForSecondsRealtime(0.01f);
            //image.material.SetTexture("_BaseMap", tex);
        }while(time < 5);
    }

}
