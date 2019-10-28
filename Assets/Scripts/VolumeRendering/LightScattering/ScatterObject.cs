using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 散射对象的基类
/// </summary>
public abstract class ScatterObject : MonoBehaviour
{
    /// <summary>
    /// 所有散射对象的静态列表
    /// </summary>
    public static List<ScatterObject> s_objList = new List<ScatterObject>();

    protected void Register(bool addToHead)
    {
        if (addToHead)
        {
            s_objList.Insert(0,this);
        }
        else
        {
            s_objList.Add(this);
        }
    }

    protected void Unregister()
    {
        int index = s_objList.IndexOf(this);
        if (index != -1)
        {
            // 把原来末尾的对象保存至该位置，从末尾移除一个对象
            s_objList[index] = s_objList[s_objList.Count - 1];
            s_objList.RemoveAt(s_objList.Count - 1);
        }
    }

    public abstract void Inject(Scattering scat, ComputeShader comp, Matrix4x4 viewProj);
    public abstract void GetBounds(Transform space, List<Vector3> camWorldBounds);
    public abstract float CullRange { get; }
}
