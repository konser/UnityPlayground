using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class NavmeshToMesh : ScriptableWizard
{

    [MenuItem("GameObject/Navmesh To Mesh")]
    static void CreateWizard()
    {
        ScriptableWizard.DisplayWizard<NavmeshToMesh>("Convert navmesh to mesh", "Convert");
    }

    void OnWizardCreate()
    {
        NavMeshTriangulation navmesh = NavMesh.CalculateTriangulation();
        Vector3[] vertices = navmesh.vertices;
        int[] indices = navmesh.indices;
        Debug.Log($"vertice count {vertices.Length}, indice count {indices.Length},area indices {navmesh.areas.Length}");

        GameObject meshObj = new GameObject("NavmeshObj");
        MeshFilter filter = meshObj.AddComponent<MeshFilter>();
        MeshRenderer render = meshObj.AddComponent<MeshRenderer>();
        render.material = new Material(Shader.Find("Standard"));
        filter.sharedMesh = CreateMesh(vertices, indices);
    }

    Mesh CreateMesh(Vector3[] vertices, int[] indices)
    {
        Mesh mesh = new Mesh();
        mesh.name = "NavMesh";
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetIndices(indices,MeshTopology.Triangles,0);
        mesh.RecalculateNormals();
        mesh.UploadMeshData(false);
        return mesh;
    }

    void OnWizardUpdate()
    {
        helpString = "";
    }

    void OnWizardOtherButton()
    {

    }
}
