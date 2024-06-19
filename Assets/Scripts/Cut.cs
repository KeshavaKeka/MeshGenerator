using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class Cut : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;

    Vector3 entry;
    Vector3 exit;
    Vector3 currentPosition;
    int previousTriangleID = -1;
    int currentTriangleID = -1;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        CreateShape();
        UpdateMesh();

        MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
        if (meshCollider != null)
        {
            meshCollider.convex = true;
            meshCollider.isTrigger = true;
        }
    }

    void Update()
    {
        // Continuously check the object's position and detect triangle transitions
        if (currentPosition != null)
        {
            previousTriangleID = currentTriangleID;
            currentTriangleID = GetTriangleID(currentPosition);

            // Log the transition if the triangle has changed
            if (previousTriangleID != -1 && currentTriangleID != previousTriangleID)
            {
                // Calculate the actual transition point
                Vector3 transitionPoint = CalculateTransitionPoint(currentPosition, previousTriangleID, currentTriangleID);

                // Log the transition with the adjusted transition point
                Debug.LogFormat("Transition from Triangle {0} to Triangle {1} at Position: {2:0.000}",
                    previousTriangleID, currentTriangleID, transitionPoint);
                exit = transitionPoint;
                //getCut(entry, exit);
                entry = transitionPoint;
                
            }
        }
    }

    void CreateShape()
    {
        // Define the vertices of the shape
        vertices = new Vector3[] {
            new Vector3(1, 0, 0),
            new Vector3(0, 0, 1),
            new Vector3(-1, 0, 0),
            new Vector3(0, 0, -1)
        };

        // Define the triangles that make up the shape
        triangles = new int[]{
            0, 3, 2,
            0, 2, 1
        };
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Get the contact point (entry) from the other object
        Transform contactPoint = other.transform.Find("Contact Point");
        if (contactPoint != null)
        {
            entry = new Vector3(contactPoint.position.x, 0, contactPoint.position.z);
            Debug.LogFormat("Entry: {0:0.000}", entry);

            // Determine and log the initial triangle ID for the entry point
            previousTriangleID = GetTriangleID(entry);
            currentTriangleID = previousTriangleID;
            if (previousTriangleID != -1)
            {
                Debug.LogFormat("Entry Point in Triangle ID {0} - Coordinates: {1:0.000}", previousTriangleID, entry);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Continuously update the current position of the contact point
        Transform contactPoint = other.transform.Find("Contact Point");
        if (contactPoint != null)
        {
            currentPosition = new Vector3(contactPoint.position.x, 0, contactPoint.position.z);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Get the contact point (exit) from the other object
        Transform contactPoint = other.transform.Find("Contact Point");
        if (contactPoint != null)
        {
            exit = new Vector3(contactPoint.position.x, 0, contactPoint.position.z);
            Debug.LogFormat("Exit: {0:0.000}", exit);

            // Determine and log the final triangle ID for the exit point
            int exitTriangleID = GetTriangleID(exit);
            if (exitTriangleID != -1)
            {
                Debug.LogFormat("Exit Point in Triangle ID {0} - Coordinates: {1:0.000}", exitTriangleID, exit);
            }
        }
    }

    int GetTriangleID(Vector3 point)
    {
        for (int i = 0; i < triangles.Length; i += 3)
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
        // Compute vectors
        Vector3 v0v1 = v1 - v0;
        Vector3 v0v2 = v2 - v0;
        Vector3 v0p = p - v0;

        // Compute dot products
        float dot00 = Vector3.Dot(v0v2, v0v2);
        float dot01 = Vector3.Dot(v0v2, v0v1);
        float dot02 = Vector3.Dot(v0v2, v0p);
        float dot11 = Vector3.Dot(v0v1, v0v1);
        float dot12 = Vector3.Dot(v0v1, v0p);

        // Compute barycentric coordinates
        float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
        float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        // Check if point is in triangle
        return (u >= 0) && (v >= 0) && (u + v <= 1);
    }

    Vector3 CalculateTransitionPoint(Vector3 currentPosition, int previousTriangleID, int currentTriangleID)
    {
        // Get the edge between the two triangles
        Vector3[] edge = GetSharedEdge(previousTriangleID, currentTriangleID);

        if (edge != null && edge.Length == 2)
        {
            // Project the current position onto the shared edge to get the transition point
            Vector3 projection = ProjectPointOntoLineSegment(currentPosition, edge[0], edge[1]);
            return projection;
        }

        // If no shared edge or any other issues, return the current position as fallback
        return currentPosition;
    }

    Vector3[] GetSharedEdge(int triangleID1, int triangleID2)
    {
        if (triangleID1 == -1 || triangleID2 == -1) return null;

        // Get vertices of both triangles
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

        // Find the shared edge between the two triangles
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

        // If we have exactly 2 shared vertices, we have found the shared edge
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

        // Project point onto the line defined by lineStart and lineEnd
        float projectionLength = Vector3.Dot(lineToPoint, lineDirection);
        projectionLength = Mathf.Clamp(projectionLength, 0, Vector3.Distance(lineStart, lineEnd));

        return lineStart + lineDirection * projectionLength;
    }
}