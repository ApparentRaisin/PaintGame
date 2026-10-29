using UnityEngine;

public class MovingTransformsController : MonoBehaviour
{
    [SerializeField] Transform[] movingObjectTransform;

    public static MovingTransformsController instance;

    void Start()
    {
        instance = this;
        SetAllMaterials();
        Debug.Log(movingObjectTransform[0].localToWorldMatrix);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Matrix4x4[] GetObjectTransforms()
    {
        Matrix4x4[] movingObjectMat = new Matrix4x4[60];
        for(int i = 8; i < Mathf.Min(movingObjectTransform.Length,60); i++)
        {
            movingObjectMat[i] = movingObjectTransform[i].localToWorldMatrix;
        }

        return movingObjectMat;
    }

    void SetAllMaterials()
    {
        for(int i = 0; i < Mathf.Min(movingObjectTransform.Length,60); i++)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetInt("_objectIndex", i);

            movingObjectTransform[i].GetComponent<MeshRenderer>().SetPropertyBlock(block);
        }        
    }
}
