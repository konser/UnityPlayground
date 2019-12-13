using System.Collections.Generic;
using UnityEngine;

namespace ComputationalGeometry
{
    public interface IConvexPoint
    {
        Vector3 position { get; }
    }

    public class ConvexHull2D<T>  where T : IConvexPoint
    {
        public static bool GetConvexHull2D(List<T> pointList,List<T> result)
        {
            List<T> _upperHullCacheList = new List<T>(100);
            List<T> _lowHullCacheList = new List<T>(100);
            result.Clear();
            if (pointList == null)
            {
                return false;
            }

            if (pointList.Count < 3)
            {
                return false;
            } 

            //if (_upperHullCacheList == null)
            //{
            //    _upperHullCacheList = new List<T>(100);
            //}

            //if (_lowHullCacheList == null)
            //{
            //    _lowHullCacheList = new List<T>(100);
            //}

            //_upperHullCacheList.Clear();
            //_lowHullCacheList.Clear();

            // 按x轴从小到大排序 x轴相同则比较z轴
            pointList.Sort((a, b) =>
            {
                int xCompare = a.position.x.CompareTo(b.position.x);
                if (xCompare != 0)
                {
                    return xCompare;
                }
                return a.position.z.CompareTo(b.position.z);
            });
            // 上半边
            _upperHullCacheList.Add(pointList[0]);
            _upperHullCacheList.Add(pointList[1]);

            for (int i = 2; i < pointList.Count; i++)
            {
                _upperHullCacheList.Add(pointList[i]);
                while (_upperHullCacheList.Count > 2  
                       && !IsTurnRight(
                           _upperHullCacheList[_upperHullCacheList.Count - 3].position,
                           _upperHullCacheList[_upperHullCacheList.Count - 2].position,
                           _upperHullCacheList[_upperHullCacheList.Count - 1].position))
                {
                    _upperHullCacheList.RemoveAt(_upperHullCacheList.Count-2);
                }
            }

            // 下半边
            _lowHullCacheList.Add(pointList[pointList.Count-1]);
            _lowHullCacheList.Add(pointList[pointList.Count-2]);
            for (int i = pointList.Count-3; i >=0; i--)
            {
                _lowHullCacheList.Add(pointList[i]);
                while (_lowHullCacheList.Count > 2 
                       && !IsTurnRight(
                           _lowHullCacheList[_lowHullCacheList.Count - 3].position,
                           _lowHullCacheList[_lowHullCacheList.Count - 2].position,
                           _lowHullCacheList[_lowHullCacheList.Count - 1].position))
                {
                    _lowHullCacheList.RemoveAt(_lowHullCacheList.Count - 2);
                }
            }

            // 移除重复点（最左 最右的两点）
            if (_lowHullCacheList.Count > 0)
            {
                _lowHullCacheList.RemoveAt(_lowHullCacheList.Count - 1);
                _lowHullCacheList.RemoveAt(0);
            }

            _upperHullCacheList.AddRange(_lowHullCacheList);

            result.AddRange(_upperHullCacheList);

            return true;
        }

        // 下一个点是否朝右偏转
        private static bool IsTurnRight(Vector3 start, Vector3 end, Vector3 p)
        {
            return GeometryTool.CheckLineSide(start, end, p) < 0;
        }

    }

}