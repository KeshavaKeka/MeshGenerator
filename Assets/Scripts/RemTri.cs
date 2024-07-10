using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class RemTri : MonoBehaviour
{
    public int verticesAlongX = 50;
    public int verticesAlongY = 30;
    public float width = 5.0f;
    public float height = 3.0f;

    public float pointDistance = 1f; // Distance between the two points
    public Color highlightColor = Color.red; // Color to highlight the points
    private Vector3 point1, point2; // The two points to highlight
    private bool pointsSet = false; // Flag to check if points have been set
    private GameObject point1Marker, point2Marker;

    private Vector3 selectedPoint1;
    private Vector3 selectedPoint2;

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


    GameObject CreateSphereMarker(Vector3 position, string name)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = name;
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * 0.1f; // Adjust size as needed
        marker.transform.SetParent(this.transform);

        Renderer renderer = marker.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Standard"));
        renderer.material.color = highlightColor;

        return marker;
    }


    void SetHighlightedPoints()
    {
        // Choose a random vertex as the starting point
        int randomIndex = Random.Range(0, vertices.Count);
        point1 = vertices[randomIndex];

        // Find the closest vertex that is approximately 'pointDistance' away
        float closestDistance = float.MaxValue;
        foreach (Vector3 vertex in vertices)
        {
            float distance = Vector3.Distance(point1, vertex);
            if (Mathf.Abs(distance - pointDistance) < closestDistance)
            {
                closestDistance = Mathf.Abs(distance - pointDistance);
                point2 = vertex;
            }
        }

        pointsSet = true;
    }


    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        CreateShape();
        numVertices = vertices.Count;
        UpdateMesh();

        MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
        if (meshCollider != null)
        {
            meshCollider.convex = true;
            meshCollider.isTrigger = true;
        }

        SelectConstantPoints(); // Select the points once
        SetAndVisualizePoints(); // Visualize the selected points
    }

    void SelectConstantPoints()
    {
        // Choose a random direction (horizontal or vertical)
        bool useHorizontal = Random.value > 0.5f;

        // Calculate step sizes
        float stepX = width / (verticesAlongX - 1);
        float stepY = height / (verticesAlongY - 1);

        // Select a random line
        float linePosition = useHorizontal
            ? Random.Range(0, height)
            : Random.Range(-width / 2, width / 2);

        // Calculate start and end points
        float start, end;
        if (useHorizontal)
        {
            start = -width / 2;
            end = start + Mathf.Min(pointDistance, width);
        }
        else
        {
            start = 0;
            end = Mathf.Min(pointDistance, height);
        }

        // Ensure points are within mesh bounds
        if (useHorizontal)
        {
            selectedPoint1 = new Vector3(start, linePosition, 0);
            selectedPoint2 = new Vector3(end, linePosition, 0);
        }
        else
        {
            selectedPoint1 = new Vector3(linePosition, start, 0);
            selectedPoint2 = new Vector3(linePosition, end, 0);
        }

        Debug.Log($"Selected Point 1: {selectedPoint1}, Point 2: {selectedPoint2}, Distance: {Vector3.Distance(selectedPoint1, selectedPoint2)}");
    }

    void SetAndVisualizePoints()
    {
        // Remove existing markers if they exist
        if (point1Marker != null) Destroy(point1Marker);
        if (point2Marker != null) Destroy(point2Marker);

        // Create sphere markers for the points
        point1Marker = CreateSphereMarker(selectedPoint1, "Point1Marker");
        point2Marker = CreateSphereMarker(selectedPoint2, "Point2Marker");

        // Log the points for debugging
        Debug.Log($"Visualizing - Point 1: {selectedPoint1}, Point 2: {selectedPoint2}, Distance: {Vector3.Distance(selectedPoint1, selectedPoint2)}");
    }


    void Update()
    {
        if (currentPosition != null)
        {
            previousTriangleID = currentTriangleID;
            currentTriangleID = GetTriangleID(currentPosition);

            if (previousTriangleID != -1 && currentTriangleID != previousTriangleID)
            {
                // Calculate the actual transition point
                Vector3 transitionPoint = CalculateTransitionPoint(currentPosition, previousTriangleID, currentTriangleID);

                // Log the transition with the adjusted transition point
                Debug.LogFormat("Transition from Triangle {0} to Triangle {1} at Position: {2:0.000}",
                    previousTriangleID, currentTriangleID, transitionPoint);
                exit = transitionPoint;
                //Debug.LogFormat("Exit lallala: {0:0.000}", exit);
                exitOnEdge = true;
                getCut(previousTriangleID, entry, exit, 1);
                entry = exit;
                //Debug.Log("Entry: {0:0.000}", entry);
                entOnEdge = true;
            }
        }
    }

    void CreateShape()
    {
        vertices = new List<Vector3>();
        triangles = new List<int>();

        float dx = width / (verticesAlongX - 1);
        float dy = height / (verticesAlongY - 1);

        for (int y = 0; y < verticesAlongY; y++)
        {
            for (int x = 0; x < verticesAlongX; x++)
            {
                // Calculate the x position, ranging from -width/2 to width/2
                float xPos = -width / 2 + x * dx;
                // Calculate the y position, ranging from 0 to height
                float yPos = y * dy;
                // Add the vertex to the list
                vertices.Add(new Vector3(xPos, yPos, 0));
            }
        }

        // Generate triangles
        for (int y = 0; y < verticesAlongY - 1; y++)
        {
            for (int x = 0; x < verticesAlongX - 1; x++)
            {
                // Calculate the starting index of the vertex in the grid
                int start = y * verticesAlongX + x;

                // First triangle (top-left, bottom-left, bottom-right)
                triangles.Add(start);
                triangles.Add(start + verticesAlongX);
                triangles.Add(start + verticesAlongX + 1);

                // Second triangle (top-left, bottom-right, top-right)
                triangles.Add(start);
                triangles.Add(start + verticesAlongX + 1);
                triangles.Add(start + 1);
            }
        }
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
        // Get the contact point (entry) from the other object
        Transform contactPoint = other.transform.Find("Contact Point");
        if (contactPoint != null)
        {
            entry = new Vector3(contactPoint.position.x, contactPoint.position.y, 0);
            entOnEdge = false;
            Debug.LogFormat("Entry: {0:0.000}", entry);

            // Determine and log the initial triangle ID for the entry point
            previousTriangleID = GetTriangleID(entry);
            currentTriangleID = previousTriangleID;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Continuously update the current position of the contact point
        Transform contactPoint = other.transform.Find("Contact Point");
        if (contactPoint != null)
        {
            currentPosition = new Vector3(contactPoint.position.x, contactPoint.position.y, 0);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Get the contact point (exit) from the other object
        Transform contactPoint = other.transform.Find("Contact Point");
        if (contactPoint != null)
        {
            exit = new Vector3(contactPoint.position.x, contactPoint.position.y, 0);
            exitOnEdge = false;
            Debug.LogFormat("Exit: {0:0.000}", exit);

            // Determine and log the final triangle ID for the exit point
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
    void RemoveQuad(int triangleID)
    {
        // Check if the triangle ID is valid
        if (triangleID < 0 || triangleID >= triangles.Count / 3)
        {
            Debug.LogError("Invalid triangle ID");
            return;
        }

        // Calculate the indices of the triangles that form the quad
        int triangle1Start = triangleID * 3;

        // Determine the second triangle forming the quad
        int triangle2Start = GetPairedTriangleStartIndex(triangleID);

        // Mark the vertices of the first triangle for removal
        triangles[triangle1Start] = 0;
        triangles[triangle1Start + 1] = 0;
        triangles[triangle1Start + 2] = 0;

        // Mark the vertices of the paired triangle for removal, if valid
        if (triangle2Start != -1)
        {
            triangles[triangle2Start] = 0;
            triangles[triangle2Start + 1] = 0;
            triangles[triangle2Start + 2] = 0;
        }

        // Update the mesh
        UpdateMesh();
    }

    int GetPairedTriangleStartIndex(int triangleID)
    {
        // Check if the triangle ID is valid
        if (triangleID < 0 || triangleID >= triangles.Count / 3)
        {
            Debug.LogError("Invalid triangle ID");
            return -1;
        }

        int triangleStartIndex = triangleID * 3;

        // Get the vertices of the current triangle
        int v0 = triangles[triangleStartIndex];
        int v1 = triangles[triangleStartIndex + 1];
        int v2 = triangles[triangleStartIndex + 2];

        // Determine which of the triangle's edges is shared with its paired triangle
        if (v1 == v2 + 1 || v1 == v2 - 1) // Shared edge between v1 and v2
        {
            return (triangleStartIndex % 6 == 0) ? triangleStartIndex + 3 : triangleStartIndex - 3;
        }
        else if (v0 == v2 + 1 || v0 == v2 - 1) // Shared edge between v0 and v2
        {
            return (triangleStartIndex % 6 == 0) ? triangleStartIndex + 3 : triangleStartIndex - 3;
        }
        else if (v0 == v1 + 1 || v0 == v1 - 1) // Shared edge between v0 and v1
        {
            return (triangleStartIndex % 6 == 0) ? triangleStartIndex + 3 : triangleStartIndex - 3;
        }

        // If no paired triangle found, return an invalid index
        return -1;
    }

    void getCut(int id, Vector3 entry, Vector3 exit, int which)
    {
        if (id > -1)
            RemoveQuad(id);
    }

}