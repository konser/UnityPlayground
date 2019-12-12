using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
#endif
namespace ComputationalGeometry
{
    public class TestGeometry : MonoBehaviour
    {
        public struct TestConvexPoint : IConvexPoint
        {
            public TestConvexPoint(float x, float y, float z)
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
        List<TestConvexPoint> randomPoints = new List<TestConvexPoint>(300);
        private List<TestConvexPoint> convexHull = new List<TestConvexPoint>(300);
        [ContextMenu("GenRandomPoint")]
        private void GenRandomPoints()
        {
            randomPoints.Clear();
            for (int i = 0; i < randomCount; i++)
            {
                Vector2 vec2 = Random.insideUnitCircle;
                randomPoints.Add(new TestConvexPoint(vec2.x* randomRadius, 0, vec2.y* randomRadius));
            }
        }


        private void Update()
        {
            if (randomPoints.Count != 0)
            {
                if (!ConvexHull2D<TestConvexPoint>.GetConvexHull2D(randomPoints, convexHull))
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