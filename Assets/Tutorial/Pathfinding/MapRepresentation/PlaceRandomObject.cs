using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using RuntimePathfinding;
using UnityEditor;
using UnityEngine;

public class PlaceRandomObject : MonoBehaviour
{
    public int count;
    public float mapSizeX;
    public float mapSizeZ;
    public float yOffset;
    private HaltonSequenceData _haltonSequenceData;
    private GameObject _prefab;
    private HaltonSequence _haltonSeq;
    private void Start()
    {

    }

    [ContextMenu("Place")]
    private void PlaceObject()
    {
        TextAsset haltonSeq = Resources.Load<TextAsset>("HaltonSequence");
        BinaryFormatter bf = new BinaryFormatter();
        _haltonSequenceData = bf.Deserialize(new MemoryStream(haltonSeq.bytes)) as HaltonSequenceData;
        _haltonSeq = new HaltonSequence();
        _prefab = Resources.Load<GameObject>("SimpleBuilding");
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetRandomPos();
            Instantiate(_prefab, pos, Quaternion.identity, this.transform);
        }
    }

    private Vector3 GetRandomPos()
    {
        _haltonSeq.Increment();
        Vector3 rand = _haltonSeq.m_CurrentPos;
        Vector3 pos = new Vector3(rand.x * mapSizeX, 0, rand.y * mapSizeZ);
        pos.y = Utility.GetTerrainHeight(pos)+yOffset;
        return pos;
    }
}
