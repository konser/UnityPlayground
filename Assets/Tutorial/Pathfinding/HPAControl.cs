using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;
using Debug = UnityEngine.Debug;

public class HPAControl : MonoBehaviour
{
    private NavMeshSurface surface;
    // Start is called before the first frame update
    void Start()
    {
        surface = this.GetComponent<NavMeshSurface>();
    }



    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            surface.RemoveData();
            Stopwatch watch = Stopwatch.StartNew();
            surface.BuildNavMesh();
            watch.Stop();
            Debug.Log($"{watch.ElapsedMilliseconds}");
        }    
    }
}
