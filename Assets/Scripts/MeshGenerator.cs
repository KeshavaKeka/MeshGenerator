using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
public class MeshGenerator : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;

    int xSize = 20;
    int ySize = 10;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        CreateShape();
        UpdateMesh();

        PrintVertices();

        // Move the mesh to the right by 0.75m
        MoveMesh(new Vector3(0.75f, 0, 0));

        // Add a MeshCollider for raycasting
        gameObject.AddComponent<MeshCollider>();
    }

    void CreateShape()
    {
        vertices = new Vector3[(xSize + 1) * (ySize + 1)];

        for (int y = 0, i = 0; y <= ySize; y++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float xPos = (float)x / xSize;
                float yPos = (float)y / ySize;
                vertices[i] = new Vector3(xPos, yPos, 0);
                i++;
            }
        }

        int numQuads = xSize * ySize;
        triangles = new int[numQuads * 6]; // 2 triangles per quad, 3 indices per triangle

        int vert = 0;
        int tris = 0;

        for (int y = 0; y < ySize; y++)
        {
            for (int x = 0; x < xSize; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + (xSize + 1) + 0;
                triangles[tris + 2] = vert + (xSize + 1) + 1;
                triangles[tris + 3] = vert + 0;
                triangles[tris + 4] = vert + (xSize + 1) + 1;
                triangles[tris + 5] = vert + 1;

                vert++;
                tris += 6;
            }
            vert++;
        }
    }

    void UpdateMesh()
    {
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals(); // Optional: Recalculate normals for smooth shading

        // Update the mesh collider after changing the mesh
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    void PrintVertices()
    {
        Debug.Log("Vertex Coordinates:");
        for (int i = 0; i < vertices.Length; i++)
        {
            Debug.Log("Vertex " + i + ": " + vertices[i]);
        }
    }

    // Function to move the entire mesh by an offset
    void MoveMesh(Vector3 offset)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] += offset;
        }
        UpdateMesh(); // Update the mesh after moving the vertices
    }

    // Function to create a cut on the clicked edge
    void CreateCutOnClick()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse button
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                MeshCollider meshCollider = hit.collider as MeshCollider;
                if (meshCollider == null || meshCollider.sharedMesh == null)
                {
                    return;
                }

                Mesh hitMesh = meshCollider.sharedMesh;
                Vector3[] hitVertices = hitMesh.vertices;
                int[] hitTriangles = hitMesh.triangles;

                // Find the closest edge to the hit point
                Vector3 hitPoint = hit.point;
                int hitTriangleIndex = hit.triangleIndex * 3;

                int vertex1 = hitTriangles[hitTriangleIndex + 0];
                int vertex2 = hitTriangles[hitTriangleIndex + 1];
                int vertex3 = hitTriangles[hitTriangleIndex + 2];

                Vector3 closestEdgeStart = Vector3.zero;
                Vector3 closestEdgeEnd = Vector3.zero;
                float minDistance = float.MaxValue;
                int closestEdgeStartIndex = 0;
                int closestEdgeEndIndex = 0;

                CheckEdgeDistance(hitVertices, vertex1, vertex2, hitPoint, ref closestEdgeStart, ref closestEdgeEnd, ref minDistance, ref closestEdgeStartIndex, ref closestEdgeEndIndex);
                CheckEdgeDistance(hitVertices, vertex2, vertex3, hitPoint, ref closestEdgeStart, ref closestEdgeEnd, ref minDistance, ref closestEdgeStartIndex, ref closestEdgeEndIndex);
                CheckEdgeDistance(hitVertices, vertex3, vertex1, hitPoint, ref closestEdgeStart, ref closestEdgeEnd, ref minDistance, ref closestEdgeStartIndex, ref closestEdgeEndIndex);

                // Duplicate the closest edge vertices
                Vector3 newV1 = vertices[closestEdgeStartIndex];
                Vector3 newV2 = vertices[closestEdgeEndIndex];
                int newV1Index = vertices.Length;
                int newV2Index = vertices.Length + 1;

                // Expand the vertices array
                List<Vector3> newVertices = new List<Vector3>(vertices);
                newVertices.Add(newV1);
                newVertices.Add(newV2);

                // Slightly offset the new vertices to create a visible gap
                Vector3 direction = (newV2 - newV1).normalized * 0.01f; // Adjust the gap size as needed
                newVertices[newV1Index] += direction;
                newVertices[newV2Index] -= direction;

                // Update the triangles to use new vertices for the cut
                List<int> newTriangles = new List<int>(triangles);
                for (int j = 0; j < newTriangles.Count; j += 3)
                {
                    if ((newTriangles[j] == closestEdgeStartIndex && newTriangles[j + 1] == closestEdgeEndIndex) ||
                        (newTriangles[j] == closestEdgeEndIndex && newTriangles[j + 1] == closestEdgeStartIndex))
                    {
                        // Replace the triangle's edge vertices with the new duplicated vertices
                        newTriangles[j] = (newTriangles[j] == closestEdgeStartIndex) ? newV1Index : newV2Index;
                        newTriangles[j + 1] = (newTriangles[j + 1] == closestEdgeStartIndex) ? newV1Index : newV2Index;
                    }
                    else if ((newTriangles[j + 1] == closestEdgeStartIndex && newTriangles[j + 2] == closestEdgeEndIndex) ||
                             (newTriangles[j + 1] == closestEdgeEndIndex && newTriangles[j + 2] == closestEdgeStartIndex))
                    {
                        newTriangles[j + 1] = (newTriangles[j + 1] == closestEdgeStartIndex) ? newV1Index : newV2Index;
                        newTriangles[j + 2] = (newTriangles[j + 2] == closestEdgeStartIndex) ? newV1Index : newV2Index;
                    }
                    else if ((newTriangles[j + 2] == closestEdgeStartIndex && newTriangles[j] == closestEdgeEndIndex) ||
                             (newTriangles[j + 2] == closestEdgeEndIndex && newTriangles[j] == closestEdgeStartIndex))
                    {
                        newTriangles[j + 2] = (newTriangles[j + 2] == closestEdgeStartIndex) ? newV1Index : newV2Index;
                        newTriangles[j] = (newTriangles[j] == closestEdgeStartIndex) ? newV1Index : newV2Index;
                    }
                }

                // Update the mesh with new vertices and triangles
                vertices = newVertices.ToArray();
                triangles = newTriangles.ToArray();

                UpdateMesh();
            }
        }
    }

    void CheckEdgeDistance(Vector3[] vertices, int v1, int v2, Vector3 hitPoint, ref Vector3 closestEdgeStart, ref Vector3 closestEdgeEnd, ref float minDistance, ref int closestEdgeStartIndex, ref int closestEdgeEndIndex)
    {
        Vector3 edgeStart = vertices[v1];
        Vector3 edgeEnd = vertices[v2];

        Vector3 closestPointOnEdge = ClosestPointOnLineSegment(edgeStart, edgeEnd, hitPoint);
        float distance = Vector3.Distance(hitPoint, closestPointOnEdge);

        if (distance < minDistance)
        {
            minDistance = distance;
            closestEdgeStart = edgeStart;
            closestEdgeEnd = edgeEnd;
            closestEdgeStartIndex = v1;
            closestEdgeEndIndex = v2;
        }
    }

    Vector3 ClosestPointOnLineSegment(Vector3 start, Vector3 end, Vector3 point)
    {
        Vector3 startToEnd = end - start;
        float edgeLengthSquared = startToEnd.sqrMagnitude;
        if (edgeLengthSquared == 0) return start;

        float t = Mathf.Clamp01(Vector3.Dot(point - start, startToEnd) / edgeLengthSquared);
        return start + t * startToEnd;
    }

    void Update()
    {
        CreateCutOnClick();
    }
}
