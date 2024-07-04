using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO; // Include this for file operations

[RequireComponent(typeof(MeshFilter))]
public class RemTri : MonoBehaviour
{
    string path = Path.Combine(Application.dataPath, "Vertices.txt");
    Mesh mesh;
    List<Vector3> vertices;
    List<int> triangles;
    int numVertices;
    bool entOnEdge = false;
    bool exitOnEdge = false;
    Vector3 entry;
    Vector3 exit;
    Vector3 currentPosition;
    int previousTriangleID = -1;
    int currentTriangleID = -1;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        // Read the vertices from the file
        Vector3[] verticesArray = ReadVerticesFromFile(path);
        if (verticesArray != null && verticesArray.Length > 0)
        {
            Debug.Log("Vertices successfully read from file.");
            vertices = new List<Vector3>(verticesArray);

            // Generate triangles based on the vertices
            triangles = new List<int>(GenerateTriangles(vertices.Count));

            numVertices = vertices.Count;
            UpdateMesh();

            MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
            if (meshCollider != null)
            {
                meshCollider.convex = true;
                meshCollider.isTrigger = true;
            }
        }
        else
        {
            Debug.LogError("No vertices found in file or failed to read vertices.");
        }
    }

    Vector3[] ReadVerticesFromFile(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("File not found: " + path);
            return null;
        }

        List<Vector3> verticesList = new List<Vector3>();
        string[] lines = File.ReadAllLines(path);

        foreach (string line in lines)
        {
            string[] parts = line.Trim(new char[] { '(', ')', ' ' }).Split(',');
            if (parts.Length == 3)
            {
                if (float.TryParse(parts[0], out float x) && float.TryParse(parts[1], out float y) && float.TryParse(parts[2], out float z))
                {
                    verticesList.Add(new Vector3(x, y, z));
                }
                else
                {
                    Debug.LogError("Failed to parse vertex: " + line);
                }
            }
        }

        return verticesList.ToArray();
    }

    int[] GenerateTriangles(int vertexCount)
    {
        // Generate dummy triangles assuming the vertices form a convex polygon.
        if (vertexCount < 3)
        {
            Debug.LogError("Not enough vertices to form triangles.");
            return new int[0];
        }

        List<int> triangles = new List<int>();
        for (int i = 0; i < vertexCount - 2; i++)
        {
            triangles.Add(0);
            triangles.Add(i + 1);
            triangles.Add(i + 2);
        }

        Debug.Log("Triangles generated: " + triangles.Count / 3);
        return triangles.ToArray();
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
    }



    private void OnTriggerEnter(Collider other)
    {
        Transform contactPoint = other.transform.Find("Contact Point");
        if (contactPoint != null)
        {
            entry = new Vector3(contactPoint.position.x, contactPoint.position.y, 0);
            entOnEdge = false;
            Debug.LogFormat("Entry: {0:0.000}", entry);

            previousTriangleID = GetTriangleID(entry);
            currentTriangleID = previousTriangleID;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        Transform contactPoint = other.transform.Find("Contact Point");
        if (contactPoint != null)
        {
            currentPosition = new Vector3(contactPoint.position.x, contactPoint.position.y, 0);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Transform contactPoint = other.transform.Find("Contact Point");
        if (contactPoint != null)
        {
            exit = new Vector3(contactPoint.position.x, contactPoint.position.y, 0);
            exitOnEdge = false;
            Debug.LogFormat("Exit: {0:0.000}", exit);

            int exitTriangleID = GetTriangleID(exit);
            getCut(exitTriangleID, entry, exit, 0);
        }
    }

    Vector3[] GetTriangleVertices(int triangleID)
    {
        if (triangleID < 0 || triangleID >= triangles.Count / 3)
        {
            Debug.LogError("Invalid triangle ID");
            return null;
        }

        int index = triangleID * 3;
        Vector3 v0 = vertices[triangles[index]];
        Vector3 v1 = vertices[triangles[index + 1]];
        Vector3 v2 = vertices[triangles[index + 2]];

        return new Vector3[] { v0, v1, v2 };
    }

    int GetTriangleID(Vector3 point)
    {
        for (int i = 0; i < triangles.Count; i += 3)
        {
            int triangleID = i / 3;
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];

            if (IsPointInTriangle(point, v0, v1, v2))
            {
                return triangleID;
            }
        }
        return -1; // Return -1 if point is not in any triangle
    }

    bool IsPointInTriangle(Vector3 p, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        Vector3 v0v1 = v1 - v0;
        Vector3 v0v2 = v2 - v0;
        Vector3 v0p = p - v0;

        float dot00 = Vector3.Dot(v0v2, v0v2);
        float dot01 = Vector3.Dot(v0v2, v0v1);
        float dot02 = Vector3.Dot(v0v2, v0p);
        float dot11 = Vector3.Dot(v0v1, v0v1);
        float dot12 = Vector3.Dot(v0v1, v0p);

        float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
        float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        return (u >= 0) && (v >= 0) && (u + v <= 1);
    }

    Vector3 CalculateTransitionPoint(Vector3 currentPosition, int previousTriangleID, int currentTriangleID)
    {
        Vector3[] edge = GetSharedEdge(previousTriangleID, currentTriangleID);

        if (edge != null && edge.Length == 2)
        {
            Vector3 projection = ProjectPointOntoLineSegment(currentPosition, edge[0], edge[1]);
            return projection;
        }

        return currentPosition;
    }

    Vector3[] GetSharedEdge(int triangleID1, int triangleID2)
    {
        if (triangleID1 == -1 || triangleID2 == -1) return null;

        Vector3[] triangle1Vertices = {
            vertices[triangles[triangleID1 * 3]],
            vertices[triangles[triangleID1 * 3 + 1]],
            vertices[triangles[triangleID1 * 3 + 2]]
        };

        Vector3[] triangle2Vertices = {
            vertices[triangles[triangleID2 * 3]],
            vertices[triangles[triangleID2 * 3 + 1]],
            vertices[triangles[triangleID2 * 3 + 2]]
        };

        List<Vector3> sharedVertices = new List<Vector3>();

        foreach (Vector3 vertex1 in triangle1Vertices)
        {
            foreach (Vector3 vertex2 in triangle2Vertices)
            {
                if (vertex1 == vertex2)
                {
                    sharedVertices.Add(vertex1);
                }
            }
        }

        if (sharedVertices.Count == 2)
        {
            return sharedVertices.ToArray();
        }

        return null; // No shared edge found
    }

    Vector3 ProjectPointOntoLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 lineDirection = (lineEnd - lineStart).normalized;
        Vector3 lineToPoint = point - lineStart;

        float projectionLength = Vector3.Dot(lineToPoint, lineDirection);
        projectionLength = Mathf.Clamp(projectionLength, 0, Vector3.Distance(lineStart, lineEnd));

        return lineStart + lineDirection * projectionLength;
    }

    void RemoveQuad(int triangleID)
    {
        if (triangleID < 0 || triangleID >= triangles.Count / 3) return;

        for (int i = 0; i < 3; i++)
        {
            int vertexIndex = triangles[triangleID * 3 + i];
            if (vertexIndex >= 0 && vertexIndex < vertices.Count)
            {
                vertices.RemoveAt(vertexIndex);
                numVertices--;
            }
        }

        for (int i = 0; i < 3; i++)
        {
            triangles[triangleID * 3 + i] = -1;
        }

        triangles.RemoveAll(t => t == -1);

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = mesh;
        }
    }

    void getCut(int exitTriangleID, Vector3 entry, Vector3 exit, int currentTriangleID)
    {
        if (exitTriangleID == -1)
        {
            Debug.Log("Exit triangle not found");
            return;
        }

        RemoveQuad(exitTriangleID);
    }
}