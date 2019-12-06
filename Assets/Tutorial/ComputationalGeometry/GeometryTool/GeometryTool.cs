#define DEBUG

using UnityEngine;
using System.Collections;

namespace ComputationalGeometry
{

    public static class GeometryTool
    {
        // 1->left  -1 -> right 
        public static int CheckLineSide(Vector3 startPoint, Vector3 endPoint, Vector3 checkPoint)
        {
            float value = (endPoint.x - startPoint.x) * (checkPoint.z - startPoint.z) - 
                          (endPoint.z - startPoint.z) * (checkPoint.x - startPoint.x);
            if (value > 0)
            {
                return 1;
            }
            if (value < 0)
            {
                return -1;
            }
            return 0;
        }
    }
}