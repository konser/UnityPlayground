using UnityEngine;
using System.Collections;
using System.Runtime.CompilerServices;
using RVO;

public static class Utility
{
    public static Vector3 XZ(this Vector3 v)
    {
        return new Vector3(v.x,0,v.z);
    }

    public static RVO.Vector2 ToRVOVec2(this Vector3 v)
    {
        return new  RVO.Vector2(v.x,v.z);
    }

    public static Vector3 RVOToVec3(this RVO.Vector2 v)
    {
        return new Vector3(v.x(),0,v.y());
    }

    #region Debug draw

    public static void DrawGreenUpLine(Vector3 pos, float time)
    {
        Debug.DrawLine(pos, pos + Vector3.up * 5, Color.green, time);
    }

    public static void DrawRedUpLine(Vector3 pos, float time)
    {
        Debug.DrawLine(pos, pos + Vector3.up * 5f, Color.red, time);
    }

    public static void DrawBlueUpLine(Vector3 pos, float time)
    {
        Debug.DrawLine(pos, pos + Vector3.up * 5f, Color.blue, time);
    }

    public static void DrawDirLine(Vector3 start, Vector3 dir, float time)
    {
        Debug.DrawLine(start, start + dir * 2f, Color.cyan, time);
    }

    public static void DrawDir(this Transform t, Vector3 dir, float time)
    {
        Debug.DrawLine(t.position, t.position + dir, Color.cyan, time);
    }

    #endregion
}
