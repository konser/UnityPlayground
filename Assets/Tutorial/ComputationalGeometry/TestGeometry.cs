using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
#endif
namespace ComputationalGeometry
{
    public class TestGeometry : MonoBehaviour
    {
        public struct TestPoint : IPoint
        {
            public TestPoint(float x, float y, float z)
            {
                pos.x = x;
                pos.y = y;
                pos.z = z;
            }
            public Vector3 pos;
            public Vector3 position
            {
                get { return pos; }
            }
        }

        public float randomRadius = 10f;
        public int randomCount = 30;
        List<TestPoint> randomPoints = new List<TestPoint>(300);
        private List<TestPoint> convexHull = new List<TestPoint>(300);
        [ContextMenu("GenRandomPoint")]
        private void GenRandomPoints()
        {
            randomPoints.Clear();
            for (int i = 0; i < randomCount; i++)
            {
                Vector2 vec2 = Random.insideUnitCircle;
                randomPoints.Add(new TestPoint(vec2.x* randomRadius, 0, vec2.y* randomRadius));
            }
        }


        private void Update()
        {
            if (randomPoints.Count != 0)
            {
                if (!ConvexHull2D<TestPoint>.GetConvexHull2D(randomPoints, convexHull))
                {
                    Debug.LogError("Error");
                }
            }
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                return;
            }

            for (int i = 0; i < randomPoints.Count; i++)
            {
                Gizmos.DrawSphere(randomPoints[i].position, 0.1f);
            }

            Gizmos.color = Color.red;
            for (int i = 0; i < convexHull.Count; i++)
            {
                Gizmos.DrawLine(convexHull[i].position, convexHull[(i + 1) % convexHull.Count].position);
            }
#endif
        }
    }

}