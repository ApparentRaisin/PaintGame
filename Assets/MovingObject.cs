using UnityEngine;

[ExecuteAlways]
public class MovingObject : MonoBehaviour
{
    public static MovingObject[] movingObjectsInScene = new MovingObject[30];
    static int currentCount;
    public int thisCount;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Application.isPlaying == false)
        {
            thisCount = currentCount;
            currentCount++;
        }
        else
        {
            movingObjectsInScene[thisCount] = this;
            currentCount++;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetInt("_objectIndex", thisCount);

            this.GetComponent<MeshRenderer>().SetPropertyBlock(block);
        }   
    }

    public Matrix4x4 GetLocalToWorld()
    {
        return this.transform.localToWorldMatrix;
    }
}
