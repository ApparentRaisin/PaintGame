using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
[ExecuteAlways]
public class ColorButton : MonoBehaviour, IPointerEnterHandler
{
    [Range(0,15)] public int x, y;
    float currentColor;
    public Image childImage;
    Material childMaterial;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        childImage.material = new Material(childImage.material);
        this.GetComponentInChildren<Button>().onClick.AddListener(delegate {GlobalCameraData.currentColorValue = currentColor; });
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Over");
        StartCoroutine(Bounce(0.1f));
    }

    IEnumerator Bounce(float time)
    {
        float t = 0;
        do
        {
            t += Time.deltaTime;
            float delta = t/time;
            yield return new WaitForEndOfFrame();
            this.transform.localScale = new Vector3(1 - Mathf.Pow(Mathf.PingPong(delta*2,1f),2) * 0.1f, 1 + Mathf.Pow(Mathf.PingPong(delta*2,1f),2) * 0.1f, 1);
        }while(t < time);
    }

    // Update is called once per frame
    void Update()
    {
        currentColor = (x * 16 + y)/256f;
        childImage.material.SetFloat("_ColorValue", currentColor);
    }


}
