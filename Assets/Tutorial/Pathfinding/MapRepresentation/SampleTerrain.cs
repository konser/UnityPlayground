using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TerrianMeshChunk
{
    public Mesh mesh;
    public GameObject gameObject;
    public void CreateGameObject(Material mat)
    {
        gameObject = new GameObject("DisplayMesh");
        gameObject.layer = LayerMask.NameToLayer("Obstacle");
        MeshFilter filter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer rend = gameObject.AddComponent<MeshRenderer>();
        mesh.RecalculateNormals();
        filter.mesh = mesh;
        rend.material = mat;
    }
}

public class SampleTerrain : MonoBehaviour
{
    public float chunkSize=32.0f;
    public int terrainSampleSize=4;
    public BoxCollider boxCollider;
    public Material mat;
    public Bounds sampleBounds => boxCollider.bounds;

    public List<TerrianMeshChunk> meshChunkList = new List<TerrianMeshChunk>();

    [ContextMenu("Sample Terrain")]
    public void CreateMesh()
    {
        for (int i = 0; i < meshChunkList.Count; i++)
        {
            DestroyImmediate(meshChunkList[i].gameObject);
        }
        meshChunkList.Clear();
        CollectTerrainMeshes(false, chunkSize, meshChunkList);
        for (int i = 0; i < meshChunkList.Count; i++)
        {
            meshChunkList[i].CreateGameObject(mat);
        }
    }

    [ContextMenu("Sample Terrain")]
    public void CollectTerrainMeshes(bool rasterizeTrees, float desiredChunkSize, List<TerrianMeshChunk> result)
    {
        // Find all terrains in the scene
        var terrains = Terrain.activeTerrains;

        if (terrains.Length > 0)
        {
            // Loop through all terrains in the scene
            for (int j = 0; j < terrains.Length; j++)
            {
                if (terrains[j].terrainData == null) continue;

                GenerateTerrainChunks(terrains[j], sampleBounds, desiredChunkSize, result);
            }
        }
    }

    private void GenerateTerrainChunks(Terrain terrain, Bounds bounds, float desiredChunkSize, List<TerrianMeshChunk> result)
    {
        TerrainData terrainData = terrain.terrainData;
        if (terrainData == null)
        {
            Debug.LogError("No terrain data.");
            return;
        }


        Vector3 offset = terrain.GetPosition();
        Vector3 center = offset + terrainData.size * 0.5F;

        Bounds terrainBounds = new Bounds(center,terrainData.size);
        if (!terrainBounds.Intersects(sampleBounds))
        {
            Debug.LogError("Not intersect.");
            return;
        }

        int heightmapWidth = terrainData.heightmapResolution;
        int heightmapHeight = terrainData.heightmapResolution;

        float[,] heights = terrainData.GetHeights(0, 0, heightmapWidth, heightmapHeight);

        Vector3 sampleSize = terrainData.heightmapScale;
        sampleSize.y = terrainData.size.y;

        const int MinChunkSize = 12;

        int chunkSizeAlongX = Mathf.CeilToInt(Mathf.Max(desiredChunkSize / (sampleSize.x * terrainSampleSize), MinChunkSize)) * terrainSampleSize;
        int chunkSizeAlongZ = Mathf.CeilToInt(Mathf.Max(desiredChunkSize / (sampleSize.z * terrainSampleSize), MinChunkSize)) * terrainSampleSize;

        Debug.Log($"Sample Terrain : HeightMap {heightmapWidth}x{heightmapHeight}, SampleSize {sampleSize} - {terrainSampleSize}, Chunk {chunkSizeAlongX}x{chunkSizeAlongZ}. ");
        for (int z = 0; z < heightmapHeight; z += chunkSizeAlongZ)
        {
            for (int x = 0; x < heightmapWidth; x += chunkSizeAlongX)
            {
                var width = Mathf.Min(chunkSizeAlongX, heightmapWidth - x);
                var depth = Mathf.Min(chunkSizeAlongZ, heightmapHeight - z);
                var chunkMin = offset + new Vector3(z * sampleSize.x, 0, x * sampleSize.z);
                var chunkMax = offset + new Vector3((z + depth) * sampleSize.x, sampleSize.y, (x + width) * sampleSize.z);
                var chunkBounds = new Bounds();
                chunkBounds.SetMinMax(chunkMin, chunkMax);

                if (chunkBounds.Intersects(bounds))
                {
                    var chunk = GenerateHeightmapChunk(heights, sampleSize, offset, x, z, width, depth, terrainSampleSize);
                    result.Add(chunk);
                }
            }
        }
    }

    static int CeilDivision(int lhs, int rhs)
    {
        return (lhs + rhs - 1) / rhs;
    }

    private TerrianMeshChunk GenerateHeightmapChunk(float[,] heights, Vector3 sampleSize, Vector3 offset, int x0, int z0, int width, int depth, int stride)
    {
        int resultWidth = CeilDivision(width, terrainSampleSize) + 1;
        int resultDepth = CeilDivision(depth, terrainSampleSize) + 1;

        var heightmapWidth = heights.GetLength(0);
        var heightmapDepth = heights.GetLength(1);

        var numVerts = resultWidth * resultDepth;
        var terrainVertices = new Vector3[numVerts];

        for (int z = 0; z < resultDepth; z++)
        {
            for (int x = 0; x < resultWidth; x++)
            {
                int sampleX = Math.Min(x0 + x * stride, heightmapWidth - 1);
                int sampleZ = Math.Min(z0 + z * stride, heightmapDepth - 1);
                
                terrainVertices[z * resultWidth + x] = new Vector3(sampleZ * sampleSize.x, heights[sampleX, sampleZ] * sampleSize.y, sampleX * sampleSize.z) + offset;
            }
        }

        int numTris = (resultWidth - 1) * (resultDepth - 1) * 2 * 3;
        var tris = new int[numTris];
        int triangleIndex = 0;
        for (int z = 0; z < resultDepth - 1; z++)
        {
            for (int x = 0; x < resultWidth - 1; x++)
            {
                tris[triangleIndex] = z * resultWidth + x;
                tris[triangleIndex + 1] = z * resultWidth + x + 1;
                tris[triangleIndex + 2] = (z + 1) * resultWidth + x + 1;
                triangleIndex += 3;
                tris[triangleIndex] = z * resultWidth + x;
                tris[triangleIndex + 1] = (z + 1) * resultWidth + x + 1;
                tris[triangleIndex + 2] = (z + 1) * resultWidth + x;
                triangleIndex += 3;
            }
        }
        TerrianMeshChunk tmesh = new TerrianMeshChunk();
        tmesh.mesh = new Mesh();
        tmesh.mesh.vertices = terrainVertices;
        tmesh.mesh.SetIndices(tris,MeshTopology.Triangles,0);
        return tmesh;
    }
}
