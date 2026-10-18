using UnityEngine;

[ExecuteAlways]
public class GlobalColorController : MonoBehaviour
{
    [SerializeField] Texture2D colorLookup;
    Texture2D lastLookup;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Shader.SetGlobalTexture("_ColorLookup",colorLookup);
    }

    // Update is called once per frame
    void Update()
    {
        if(colorLookup != lastLookup)
        {
            Shader.SetGlobalTexture("_ColorLookup",colorLookup);
            lastLookup = colorLookup;
        }
        
    }
}
