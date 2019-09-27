using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class TestOctree : MonoBehaviour
{
    public int count;
    public BoxObject prefab;
    public List<BoxObject> collisionObjects = new List<BoxObject>();
    public void RandomGenCubes()
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = Random.insideUnitSphere * 5000f;
            Vector3 randomSize = (2 * Random.insideUnitSphere - Vector3.one) * 100f;
            BoxObject obj = GameObject.Instantiate(prefab, pos, Quaternion.identity);
            obj.transform.localScale = randomSize;
        }
    }

    private void Start()
    {
        RandomGenCubes();
        var boxes = FindObjectsOfType<BoxObject>();
        for (int i = 0; i < boxes.Length; i++)
        {
            collisionObjects.Add(boxes[i]);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (octree != null)
        {
            octree.DebugDraw(0);
        }
    }

    private OctTree<BoxObject> octree;

    [ContextMenu("CreatOctree")]
    public void CreateOctree()
    {
        octree = new OctTree<BoxObject>(new AABBBoundBox(-5000f * Vector3.one,5000f * Vector3.one));
        for (int i = 0; i < collisionObjects.Count; i++)
        {
            octree.Insert(collisionObjects[i]);
        }
        octree.UpdateTree();
        int d = octree.GetMaxTreeDepth();
        Debug.Log(d+ " " + Mathf.Pow(8,d)/(d-1));
    }

    
}
