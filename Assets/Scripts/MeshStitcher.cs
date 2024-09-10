using UnityEngine;
using System.Collections.Generic;

public class MeshStitcher : MonoBehaviour
{
    public float stitchThreshold = 0.01f;

    private Mesh meshToStitch;
    private List<Vector3> vertices;
    private List<int> triangles;
    private List<Vector2> uv;

    void Awake()
    {
        // Get the mesh from the MeshFilter component
        meshToStitch = GetComponent<MeshFilter>().mesh;
    }

    public void StitchMesh()
    {
        if (meshToStitch == null)
        {
            Debug.LogError("No mesh found to stitch!");
            return;
        }

        vertices = new List<Vector3>(meshToStitch.vertices);
        triangles = new List<int>(meshToStitch.triangles);
        uv = new List<Vector2>(meshToStitch.uv);

        List<Edge> edges = FindCutEdges();
        List<Edge> stitchableEdges = FindStitchableEdges(edges);

        foreach (Edge edge in stitchableEdges)
        {
            StitchEdge(edge);
        }

        // Apply changes to the mesh
        meshToStitch.Clear();
        meshToStitch.SetVertices(vertices);
        meshToStitch.SetTriangles(triangles, 0);
        meshToStitch.SetUVs(0, uv);
        meshToStitch.RecalculateNormals();
        meshToStitch.RecalculateBounds();

        // Update the collider if it exists
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = meshToStitch;
        }
    }

    List<Edge> FindCutEdges()
    {
        Dictionary<Edge, int> edgeCount = new Dictionary<Edge, int>();

        for (int i = 0; i < triangles.Count; i += 3)
        {
            AddEdge(edgeCount, triangles[i], triangles[i + 1]);
            AddEdge(edgeCount, triangles[i + 1], triangles[i + 2]);
            AddEdge(edgeCount, triangles[i + 2], triangles[i]);
        }

        List<Edge> cutEdges = new List<Edge>();
        foreach (var kvp in edgeCount)
        {
            if (kvp.Value == 1)
            {
                cutEdges.Add(kvp.Key);
            }
        }

        return cutEdges;
    }

    void AddEdge(Dictionary<Edge, int> edgeCount, int v1, int v2)
    {
        Edge edge = new Edge(v1, v2);
        if (edgeCount.ContainsKey(edge))
        {
            edgeCount[edge]++;
        }
        else
        {
            edgeCount[edge] = 1;
        }
    }

    List<Edge> FindStitchableEdges(List<Edge> cutEdges)
    {
        List<Edge> stitchableEdges = new List<Edge>();

        for (int i = 0; i < cutEdges.Count; i++)
        {
            for (int j = i + 1; j < cutEdges.Count; j++)
            {
                if (IsStitchable(cutEdges[i], cutEdges[j]))
                {
                    stitchableEdges.Add(new Edge(cutEdges[i].v1, cutEdges[j].v1));
                    stitchableEdges.Add(new Edge(cutEdges[i].v2, cutEdges[j].v2));
                    break;
                }
            }
        }

        return stitchableEdges;
    }

    bool IsStitchable(Edge e1, Edge e2)
    {
        return Vector3.Distance(vertices[e1.v1], vertices[e2.v1]) < stitchThreshold &&
               Vector3.Distance(vertices[e1.v2], vertices[e2.v2]) < stitchThreshold;
    }

    void StitchEdge(Edge edge)
    {
        int newTriangle1 = vertices.Count;
        int newTriangle2 = vertices.Count + 1;

        // Add new vertices
        vertices.Add((vertices[edge.v1] + vertices[edge.v2]) * 0.5f);
        vertices.Add((vertices[edge.v1] + vertices[edge.v2]) * 0.5f);

        // Add new UVs (you might want to interpolate these based on the original UVs)
        uv.Add((uv[edge.v1] + uv[edge.v2]) * 0.5f);
        uv.Add((uv[edge.v1] + uv[edge.v2]) * 0.5f);

        // Add new triangles
        triangles.Add(edge.v1);
        triangles.Add(newTriangle1);
        triangles.Add(edge.v2);

        triangles.Add(edge.v2);
        triangles.Add(newTriangle2);
        triangles.Add(edge.v1);
    }

    private struct Edge
    {
        public int v1;
        public int v2;

        public Edge(int v1, int v2)
        {
            this.v1 = Mathf.Min(v1, v2);
            this.v2 = Mathf.Max(v1, v2);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Edge)) return false;
            Edge other = (Edge)obj;
            return (v1 == other.v1 && v2 == other.v2) || (v1 == other.v2 && v2 == other.v1);
        }

        public override int GetHashCode()
        {
            return v1.GetHashCode() ^ v2.GetHashCode();
        }
    }
}