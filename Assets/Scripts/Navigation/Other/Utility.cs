using UnityEngine;
using System.Collections.Generic;
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
    private static float Frac0(float x)
    {
        return x - Mathf.Floor(x);
    }

    private static float Frac1(float x)
    {
        return 1 - x + Mathf.Floor(x);
    }

    private static Terrain[] terrains;
    public static float GetTerrainHeight(Vector3 pos)
    {
        if (terrains == null)
        {
            terrains = Terrain.activeTerrains;
        }
#if UNITY_EDITOR
        else
        {
            for (int i = 0; i < terrains.Length; i++)
            {
                if (terrains[i] == null)
                {
                    terrains = Terrain.activeTerrains;
                    break;
                }
            }
        }
#endif

        if (terrains == null || terrains.Length == 0)
        {
            return 0.0f;
        }

        
        for (int i = 0; i < terrains.Length; i++)
        {
            Vector3 terrainPosMin = terrains[i].GetPosition();
            Vector3 terrainPosMax = terrainPosMin + terrains[i].terrainData.size;
            if (pos.x >= terrainPosMin.x && pos.x <= terrainPosMax.x && pos.z >= terrainPosMin.z && pos.z <= terrainPosMax.z)
            {
                return terrains[i].SampleHeight(pos) + terrains[i].GetPosition().y;
            }
        }
        return 0f;
    }

    /// <summary>
    /// 给定起点 终点 网格尺寸计算出经过的格子索引 (x,y)
    /// </summary>
    public static void GridTraverse(Vector3 start, Vector3 end, float gridSize, ref List<Vector2Int> indexList)
    {
        start /= gridSize;
        end /= gridSize;
        float tMaxX, tMaxY, tDeltaX, tDeltaY;
        float dx = end.x - start.x;
        float dy = end.z - start.z;
        int signX = (dx > 0 ? 1 : (dx < 0 ? -1 : 0));
        int signY = (dy > 0 ? 1 : (dy < 0 ? -1 : 0));
        if (dx != 0)
        {
            tDeltaX = Mathf.Min(signX / dx, float.MaxValue);
        }
        else
        {
            tDeltaX = float.MaxValue;
        }

        if (signX > 0)
        {
            tMaxX = tDeltaX * Frac1(start.x);
        }
        else
        {
            tMaxX = tDeltaX * Frac0(start.x);
        }


        if (dy != 0)
        {
            tDeltaY = Mathf.Min(signY / dy, float.MaxValue);
        }
        else
        {
            tDeltaY = float.MaxValue;
        }

        if (signY > 0)
        {
            tMaxY = tDeltaY * Frac1(start.z);
        }
        else
        {
            tMaxY = tDeltaY * Frac0(start.z);
        }

        int idx = (int)start.x;
        int idy = (int)start.z;
        indexList.Clear();
        indexList.Add(new Vector2Int(idx, idy));
        while (true)
        {
            if (tMaxX < tMaxY)
            {
                tMaxX = tMaxX + tDeltaX;
                idx += signX;
            }
            else
            {
                tMaxY = tMaxY + tDeltaY;
                idy += signY;
            }
            if (tMaxX > 1 && tMaxY > 1)
            {
                break;
            }

            indexList.Add(new Vector2Int(idx, idy));
        }
        indexList.Add(new Vector2Int((int)end.x, (int)end.z));
    }

    public static void GridTraverse(Vector3 start, Vector3 end, float gridSize, ref List<Vector2Int> indexList,ref List<Vector3> intersection)
    {
        start /= gridSize;
        end /= gridSize;
        float tMaxX, tMaxY, tDeltaX, tDeltaY;
        float dx = end.x - start.x;
        float dy = end.z - start.z;
        int signX = (dx > 0 ? 1 : (dx < 0 ? -1 : 0));
        int signY = (dy > 0 ? 1 : (dy < 0 ? -1 : 0));
        Vector3 dir = ((end.XZ()) - (start.XZ())).normalized;
        if (dx != 0)
        {
            tDeltaX = Mathf.Min(signX / dx, float.MaxValue);
        }
        else
        {
            tDeltaX = float.MaxValue;
        }

        if (signX > 0)
        {
            tMaxX = tDeltaX * Frac1(start.x);
        }
        else
        {
            tMaxX = tDeltaX * Frac0(start.x);
        }


        if (dy != 0)
        {
            tDeltaY = Mathf.Min(signY / dy, float.MaxValue);
        }
        else
        {
            tDeltaY = float.MaxValue;
        }

        if (signY > 0)
        {
            tMaxY = tDeltaY * Frac1(start.z);
        }
        else
        {
            tMaxY = tDeltaY * Frac0(start.z);
        }

        int idx = (int)start.x;
        int idy = (int)start.z;
        indexList.Clear();
        indexList.Add(new Vector2Int(idx, idy));

        intersection.Clear();
        while (true)
        {
            if (tMaxX < tMaxY)
            {
                tMaxX = tMaxX + tDeltaX;
                idx += signX;
                float length = tMaxX / dir.x;
                intersection.Add(gridSize *(start + length * dir));
            }
            else
            {
                tMaxY = tMaxY + tDeltaY;
                idy += signY;
                float length = tMaxY / dir.z;
                intersection.Add(gridSize * (start + length * dir));
            }
            if (tMaxX > 1 && tMaxY > 1)
            {
                break;
            }

            indexList.Add(new Vector2Int(idx, idy));
        }
        indexList.Add(new Vector2Int((int)end.x, (int)end.z));
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
