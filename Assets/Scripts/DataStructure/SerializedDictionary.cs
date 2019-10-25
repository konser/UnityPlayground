using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SerializedDictionary<K,V> : Dictionary<K,V>, ISerializationCallbackReceiver
{
    [SerializeField]
    List<K> _keyList = new List<K>();
    [SerializeField]
    List<V> _valueList = new List<V>();

    public void OnBeforeSerialize()
    {
        _keyList.Clear();
        _valueList.Clear();
        foreach (KeyValuePair<K, V> tPair in this)
        {
            _keyList.Add(tPair.Key);
            _valueList.Add(tPair.Value);
        }
    }
    public void OnAfterDeserialize()
    {
        for (int i = 0; i < _keyList.Count; i++)
        {
            Add(_keyList[i],_valueList[i]);
        }
        _keyList.Clear();
        _valueList.Clear();
    }
}
