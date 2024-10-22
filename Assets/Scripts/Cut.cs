using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class Cut : MonoBehaviour
{
    public delegate void CutCompletedEventHandler();
    public event CutCompletedEventHandler OnCutCompleted;

    public int verticesAlongX = 100;
    public int verticesAlongY = 60;
    public float width = 5.0f;
    public float height = 3.0f;
    Mesh mesh;
    List<Vector3> vertices;
    List<int> triangles;
    int numVertices;
    bool entOnEdge = false;
    bool exitOnEdge = false;
    Vector3 entry;
    Vector3 exit;
    public Vector3 currentPosition; // Changed to public
    int previousTriangleID = -1;
    int currentTriangleID = -1;
    float offsetDistance = 0.02f;

    // New variables for point selection
    public float pointDistance = 1f;
    public Color highlightColor = Color.red;
    private GameObject point1Marker, point2Marker;

    private Vector3 selectedPoint1;
    private Vector3 selectedPoint2;
    private List<Vector3> cutPath = new List<Vector3>();
    private float cutThreshold = 0.1f; // Tolerance for cut deviation from the line
    public float cutProgressThreshold = 0.95f; // Progress threshold to consider a cut complete

    private bool cutStarted = false;
    private bool cutCompleted = false;
    private bool cutMadeBetweenPoints = false;
    private float currentCutProgress = 0f;
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

        // Add point selection after mesh creation
        SelectConstantPoints();
        SetAndVisualizePoints();
    }

    void Update()
    {
        if (currentPosition != null)
        {
            UpdateCutProgress();
            CheckCutCompletion();
            // Straight line cut detection
            if (!cutStarted && IsNearPoint(currentPosition, selectedPoint1))
            {
                cutStarted = true;
                cutPath.Clear();
                Debug.Log("Cut started near Point 1");
            }

            if (cutStarted && !cutCompleted)
            {
                cutPath.Add(currentPosition);
                CheckCutProgress();
            }

            // Existing triangle transition logic
            previousTriangleID = currentTriangleID;
            currentTriangleID = GetTriangleID(currentPosition);

            if (currentTriangleID != -1 && previousTriangleID == -1)
            {
                Vector3 transitionPoint = CalculateTransitionPoint(currentPosition, currentTriangleID);
                entry = transitionPoint;
                entOnEdge = true;
            }
            else if (currentTriangleID == -1 && previousTriangleID != -1)
            {
                Vector3 transitionPoint = CalculateTransitionPoint(currentPosition, previousTriangleID);
                exit = transitionPoint;
                exitOnEdge = true;
                getCut(previousTriangleID, entry, exit, 1);
                entry = exit;
                entOnEdge = true;
            }
            else if (previousTriangleID != -1 && currentTriangleID != previousTriangleID)
            {
                // Calculate the actual transition point
                Vector3 transitionPoint = CalculateTransitionPoint(currentPosition, previousTriangleID, currentTriangleID);

                // Log the transition with the adjusted transition point
                Debug.LogFormat("Transition from Triangle {0} to Triangle {1} at Position: {2:0.000}",
                    previousTriangleID, currentTriangleID, transitionPoint);
                exit = transitionPoint;
                exitOnEdge = true;
                getCut(previousTriangleID, entry, exit, 1);
                entry = exit;
                entOnEdge = true;
            }

            // Check for straight line cut completion
            //if (cutStarted && IsNearPoint(currentPosition, selectedPoint2))
            //{
            //    cutCompleted = true;
            //    Debug.Log("Straight line cut completed between the two points!");
            //}

            if (cutStarted && IsNearPoint(currentPosition, selectedPoint2))
            {
                cutCompleted = true;
                Debug.Log("Straight line cut completed between the two points!");
                OnCutCompleted?.Invoke(); // Trigger the event
            }
        }
    }
    

    private void UpdateCutProgress()
    {
        if (!cutStarted && IsNearPoint(currentPosition, selectedPoint1))
        {
            cutStarted = true;
            cutPath.Clear();
            Debug.Log("Cut started near Point 1");
        }

        if (cutStarted && !cutCompleted)
        {
            cutPath.Add(currentPosition);
            currentCutProgress = CalculateCutProgress();
            Debug.Log($"Current cut progress: {currentCutProgress}");
        }
    }

    private float CalculateCutProgress()
    {
        Vector3 cutDirection = (selectedPoint2 - selectedPoint1).normalized;
        Vector3 currentCutVector = currentPosition - selectedPoint1;
        float projectionLength = Vector3.Dot(currentCutVector, cutDirection);
        return Mathf.Clamp01(projectionLength / Vector3.Distance(selectedPoint1, selectedPoint2));
    }

    private void CheckCutCompletion()
    {
        if (cutStarted && !cutCompleted)
        {
            if (currentCutProgress >= cutProgressThreshold || IsNearPoint(currentPosition, selectedPoint2))
            {
                CompleteCut();
            }
            else if (!IsValidCutPath())
            {
                ResetCut();
                Debug.Log("Cut deviated too much from the intended path. Resetting.");
            }
        }
    }

    private bool IsValidCutPath()
    {
        Vector3 cutDirection = (selectedPoint2 - selectedPoint1).normalized;
        foreach (Vector3 point in cutPath)
        {
            Vector3 pointVector = point - selectedPoint1;
            Vector3 projection = Vector3.Project(pointVector, cutDirection);
            float deviation = Vector3.Distance(pointVector, projection);
            if (deviation > cutThreshold)
            {
                return false;
            }
        }
        return true;
    }

    public void CompleteCut()
    {
        cutCompleted = true;
        Debug.Log("Cut completed successfully!");
        OnCutCompleted?.Invoke();
    }

    // ... existing methods ...

    public float GetCutProgress()
    {
        return currentCutProgress;
    }

    public bool IsCutStarted()
    {
        return cutStarted;
    }

    public bool IsCutCompleted()
    {
        return cutCompleted;
    }

    private bool IsNearPoint(Vector3 position, Vector3 point)
    {
        return Vector3.Distance(position, point) < cutThreshold;
    }

    private void CheckCutProgress()
    {
        if (cutPath.Count < 2) return;

        Vector3 cutDirection = (selectedPoint2 - selectedPoint1).normalized;
        Vector3 lastPoint = cutPath[cutPath.Count - 1];

        // Check if the cut is still following the line
        if (Vector3.Distance(lastPoint, selectedPoint1 + Vector3.Project(lastPoint - selectedPoint1, cutDirection)) > cutThreshold)
        {
            Debug.Log("Cut deviated from the straight line");
            ResetCut();
            return;
        }
    }

    public void ResetCut()
    {
        Debug.Log("ResetCut method called");

        cutStarted = false;
        cutCompleted = false;
        cutPath.Clear();
        currentCutProgress = 0f;

        // Reset mesh to original state
        CreateShape();
        numVertices = vertices.Count;
        UpdateMesh();

        // Reset other variables
        entOnEdge = false;
        exitOnEdge = false;
        previousTriangleID = -1;
        currentTriangleID = -1;
        currentPosition = Vector3.zero;

        // Reselect points and visualize them
        SelectConstantPoints();
        SetAndVisualizePoints();

        // Reset MeshCollider
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
        }

        Debug.Log($"Mesh reset. Vertex count: {mesh.vertexCount}, Triangle count: {mesh.triangles.Length / 3}");
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
        if (mesh == null)
        {
            mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = mesh;
        }
        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
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

        if (cutStarted && !cutCompleted)
        {
            Debug.Log("Cutting object exited before completing the cut");
            ResetCut();
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
            entry = exit;
            entOnEdge = false;
        }
    }
    Vector3[] GetTriangleVertices(int triangleID)
    {
        if (triangleID < 0 || triangleID >= triangles.Count / 3)
        {
            Debug.LogError($"Invalid triangle ID: {triangleID}");
            return null;
        }

        int index = triangleID * 3;
        if (index + 2 >= triangles.Count || index + 2 >= vertices.Count)
        {
            Debug.LogError($"Triangle index out of range for ID: {triangleID}");
            return null;
        }

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

    Vector3 CalculateTransitionPoint(Vector3 currentPosition, int TriangleID)
    {
        // Get the edge between the two triangles
        Vector3[] edge = GetClosestEdge(TriangleID, currentPosition);

        if (edge != null && edge.Length == 2)
        {
            // Project the current position onto the shared edge to get the transition point
            Vector3 projection = ProjectPointOntoLineSegment(currentPosition, edge[0], edge[1]);
            return projection;
        }

        // If no shared edge or any other issues, return the current position as fallback
        return currentPosition;
    }

    Vector3[] GetClosestEdge(int triangleID, Vector3 point)
    {
        if (triangleID == -1) return null;

        // Get vertices of the triangle
        Vector3[] triangleVertices = {
        vertices[triangles[triangleID * 3]],
        vertices[triangles[triangleID * 3 + 1]],
        vertices[triangles[triangleID * 3 + 2]]
        };

        // Define edges of the triangle
        Vector3[][] edges = {
        new Vector3[] { triangleVertices[0], triangleVertices[1] },
        new Vector3[] { triangleVertices[1], triangleVertices[2] },
        new Vector3[] { triangleVertices[2], triangleVertices[0] }
        };

        // Initialize the closest edge and the minimum distance
        Vector3[] closestEdge = null;
        float minDistance = float.MaxValue;

        // Function to calculate distance from a point to a line segment
        float DistancePointToSegment(Vector3 p, Vector3 v, Vector3 w)
        {
            // Return minimum distance between point p and line segment vw
            float l2 = Vector3.SqrMagnitude(w - v); // i.e. |w-v|^2 -  avoid a sqrt
            if (l2 == 0.0) return Vector3.Distance(p, v); // v == w case
                                                          // Consider the line extending the segment, parameterized as v + t (w - v).
                                                          // We find projection of point p onto the line.
                                                          // It falls where t = [(p-v) . (w-v)] / |w-v|^2
            float t = Mathf.Max(0, Mathf.Min(1, Vector3.Dot(p - v, w - v) / l2));
            Vector3 projection = v + t * (w - v); // Projection falls on the segment
            return Vector3.Distance(p, projection);
        }

        // Find the closest edge
        foreach (var edge in edges)
        {
            float distance = DistancePointToSegment(point, edge[0], edge[1]);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestEdge = edge;
            }
        }

        return closestEdge;
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
    void RemoveTriangle(int id)
    {
        triangles[id * 3 + 0] = 0;
        triangles[id * 3 + 1] = 0;
        triangles[id * 3 + 2] = 0;
        UpdateMesh();
    }
    public Vector3[] GetEdgeVertices(Vector3 vertex, Vector3 v1, Vector3 v2, Vector3 v3)
    {
        bool IsPointOnLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 lineDirection = (lineEnd - lineStart).normalized;
            Vector3 lineToPoint = point - lineStart;
            float projectionLength = Vector3.Dot(lineToPoint, lineDirection);
            if (projectionLength < 0 || projectionLength > Vector3.Distance(lineStart, lineEnd))
                return false;
            Vector3 closestPoint = lineStart + lineDirection * projectionLength;
            float tolerance = 1e-6f;
            return Vector3.Distance(closestPoint, point) < tolerance;
        }
        if (IsPointOnLineSegment(vertex, v1, v2))
        {
            return new Vector3[] { v1, v2, v3 };
        }
        else if (IsPointOnLineSegment(vertex, v2, v3))
        {
            return new Vector3[] { v2, v3, v1 };
        }
        else if (IsPointOnLineSegment(vertex, v3, v1))
        {
            return new Vector3[] { v3, v1, v2 };
        }
        return null;
    }
    public static bool IsClockwise(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 edge1 = p2 - p1;
        Vector3 edge2 = p3 - p1;
        Vector3 normal = Vector3.Cross(edge1, edge2);
        float dotProduct = Vector3.Dot(normal, Vector3.back);
        Debug.Log(dotProduct < 0);
        return dotProduct < 0;
    }

    // New methods for point selection

    void SelectConstantPoints()
    {
        bool useHorizontal = Random.value > 0.5f;
        float linePosition = useHorizontal
            ? Random.Range(0, height)
            : Random.Range(-width / 2, width / 2);

        float start, end;
        if (useHorizontal)
        {
            start = -width / 2;
            end = start + Mathf.Min(pointDistance, width);
            selectedPoint1 = new Vector3(start, linePosition, 0);
            selectedPoint2 = new Vector3(end, linePosition, 0);
        }
        else
        {
            start = 0;
            end = Mathf.Min(pointDistance, height);
            selectedPoint1 = new Vector3(linePosition, start, 0);
            selectedPoint2 = new Vector3(linePosition, end, 0);
        }

        Debug.Log($"Selected Point 1: {selectedPoint1}, Point 2: {selectedPoint2}, Distance: {Vector3.Distance(selectedPoint1, selectedPoint2)}");
    }

    void SetAndVisualizePoints()
    {
        if (point1Marker != null) Destroy(point1Marker);
        if (point2Marker != null) Destroy(point2Marker);

        point1Marker = CreateSphereMarker(selectedPoint1, "Point1Marker");
        point2Marker = CreateSphereMarker(selectedPoint2, "Point2Marker");

        Debug.DrawLine(selectedPoint1, selectedPoint2, Color.green, 100f);
        Debug.Log($"Visualizing - Point 1: {selectedPoint1}, Point 2: {selectedPoint2}, Distance: {Vector3.Distance(selectedPoint1, selectedPoint2)}");
    }

    GameObject CreateSphereMarker(Vector3 position, string name)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = name;
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * 0.1f; // Keep the larger size
        marker.transform.SetParent(this.transform);

        // Remove the collider to prevent physical interactions
        Destroy(marker.GetComponent<Collider>());

        // Make the marker ignore raycasts
        marker.layer = LayerMask.NameToLayer("Ignore Raycast");

        Renderer renderer = marker.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Standard"));
        renderer.material.color = highlightColor;

        return marker;
    }
    void getCut(int id, Vector3 entry, Vector3 exit, int which)
    {
        if (vertices == null || triangles == null)
        {
            Debug.LogError("Vertices or triangles list is null. Ensure mesh is properly initialized.");
            return;
        }
        if (triangles == null)
        {
            Debug.LogError("Triangles list is null. Ensure mesh is properly initialized.");
            return;
        }

        if (id < 0 || id >= triangles.Count / 3)
        {
            Debug.LogError($"Invalid triangle ID: {id}");
            return;
        }

        if (id >= 0)
        {
            Vector3[] triangleVertices = GetTriangleVertices(id);
            bool entryCheck = triangleVertices[0] != entry && triangleVertices[1] != entry && triangleVertices[2] != entry;
            bool exitCheck = triangleVertices[0] != exit && triangleVertices[1] != exit && triangleVertices[2] != exit;
            if (entryCheck && exitCheck)
            {
                if (entOnEdge == true && exitOnEdge == true)
                {
                    //Debug.LogFormat("Inside index {0}", id);
                    //Debug.LogFormat("v1 {0:0.000}", triangleVertices[0]);
                    //Debug.LogFormat("v2 {0:0.000}", triangleVertices[1]);
                    //Debug.LogFormat("v3 {0:0.000}", triangleVertices[2]);
                    Vector3 transitionPoint = CalculateTransitionPoint(entry, id);
                    entry = transitionPoint;
                    transitionPoint = CalculateTransitionPoint(exit, id);
                    exit = transitionPoint;
                    //Debug.LogFormat("entry {0:0.000}", entry);
                    //Debug.LogFormat("entry {0:0.000}", exit);
                    //RemoveTriangle(id);
                    //Debug.Log(string.Format("{0:0.000}", exit));
                    Vector3[] notEntry = GetEdgeVertices(entry, triangleVertices[0], triangleVertices[1], triangleVertices[2]);
                    Vector3[] notExit = GetEdgeVertices(exit, triangleVertices[0], triangleVertices[1], triangleVertices[2]);
                    if (notEntry[2] != notExit[2])
                    {
                        triangleVertices[2] = notEntry[2];
                        triangleVertices[1] = notExit[2];
                        for (int i = 0; i < 2; i++)
                        {
                            if (notEntry[i] != notEntry[2] && notEntry[i] != notExit[2])
                                triangleVertices[0] = notEntry[i];
                        }
                        Vector3 dir1 = triangleVertices[1] - triangleVertices[0];
                        Vector3 dir2 = triangleVertices[2] - triangleVertices[0];
                        Ray entRay = new Ray(triangleVertices[0], dir1);
                        Ray exiRay = new Ray(triangleVertices[0], dir2);
                        Vector3 entry1 = entry - dir1.normalized * offsetDistance;
                        Vector3 entry2 = entry + dir1.normalized * offsetDistance;
                        Vector3 exit1 = exit - dir2.normalized * offsetDistance;
                        Vector3 exit2 = exit + dir2.normalized * offsetDistance;
                        vertices.Add(triangleVertices[0]);
                        vertices.Add(triangleVertices[1]);
                        vertices.Add(triangleVertices[2]);
                        vertices.Add(entry1);
                        vertices.Add(entry2);
                        vertices.Add(exit1);
                        vertices.Add(exit2);
                        if (IsClockwise(triangleVertices[0], entry1, exit1))
                        {
                            triangles[id * 3 + 0] = numVertices;
                            triangles[id * 3 + 1] = numVertices + 5;
                            triangles[id * 3 + 2] = numVertices + 3;
                        }
                        else
                        {
                            triangles[id * 3 + 0] = numVertices;
                            triangles[id * 3 + 1] = numVertices + 3;
                            triangles[id * 3 + 2] = numVertices + 5;
                        }
                        if (IsClockwise(entry2, triangleVertices[1], exit2))
                        {
                            triangles.Add(numVertices + 4);
                            triangles.Add(numVertices + 6);
                            triangles.Add(numVertices + 1);
                        }
                        else
                        {
                            triangles.Add(numVertices + 4);
                            triangles.Add(numVertices + 1);
                            triangles.Add(numVertices + 6);
                        }
                        if (IsClockwise(triangleVertices[1], triangleVertices[2], exit2))
                        {
                            triangles.Add(numVertices + 1);
                            triangles.Add(numVertices + 6);
                            triangles.Add(numVertices + 2);
                        }
                        else
                        {
                            triangles.Add(numVertices + 1);
                            triangles.Add(numVertices + 2);
                            triangles.Add(numVertices + 6);
                        }
                        UpdateMesh();
                        numVertices += 7;
                    }
                }
                else if (entOnEdge == false && exitOnEdge == false)
                {
                    Debug.Log("0");
                }
                else
                {
                    //RemoveTriangle(id);
                    if (which == 1)
                    {
                        triangleVertices = GetEdgeVertices(exit, triangleVertices[0], triangleVertices[1], triangleVertices[2]);
                        Vector3 v1 = triangleVertices[0];
                        Vector3 v2 = triangleVertices[1];
                        Vector3 v3 = triangleVertices[2];
                        Vector3 dir = v2 - v1;
                        Ray ray = new Ray(v1, dir);
                        Vector3 exit1 = exit - dir.normalized * offsetDistance;
                        Vector3 exit2 = exit + dir.normalized * offsetDistance;
                        vertices.Add(v1);
                        vertices.Add(v2);
                        vertices.Add(v3);
                        vertices.Add(entry);
                        vertices.Add(exit1);
                        vertices.Add(exit2);
                        triangles[id * 3 + 0] = numVertices;
                        triangles[id * 3 + 1] = numVertices + 4;
                        triangles[id * 3 + 2] = numVertices + 3;
                        triangles.Add(numVertices);
                        triangles.Add(numVertices + 3);
                        triangles.Add(numVertices + 2);
                        triangles.Add(numVertices + 3);
                        triangles.Add(numVertices + 1);
                        triangles.Add(numVertices + 2);
                        triangles.Add(numVertices + 3);
                        triangles.Add(numVertices + 5);
                        triangles.Add(numVertices + 1);
                        numVertices += 6;
                        UpdateMesh();
                    }
                    else
                    {
                        triangleVertices = GetEdgeVertices(entry, triangleVertices[0], triangleVertices[1], triangleVertices[2]);
                        Vector3 v1 = triangleVertices[0];
                        Vector3 v2 = triangleVertices[1];
                        Vector3 v3 = triangleVertices[2];
                        Vector3 dir = v2 - v1;
                        Ray ray = new Ray(v1, dir);
                        Vector3 entry1 = entry - dir.normalized * offsetDistance;
                        Vector3 entry2 = entry + dir.normalized * offsetDistance;
                        vertices.Add(v1);
                        vertices.Add(v2);
                        vertices.Add(v3);
                        vertices.Add(entry1);
                        vertices.Add(entry2);
                        vertices.Add(exit);
                        triangles[id * 3 + 0] = numVertices + 3;
                        triangles[id * 3 + 1] = numVertices + 5;
                        triangles[id * 3 + 2] = numVertices;
                        triangles.Add(numVertices + 5);
                        triangles.Add(numVertices + 2);
                        triangles.Add(numVertices);
                        triangles.Add(numVertices + 4);
                        triangles.Add(numVertices + 1);
                        triangles.Add(numVertices + 5);
                        triangles.Add(numVertices + 5);
                        triangles.Add(numVertices + 1);
                        triangles.Add(numVertices + 2);
                        numVertices += 6;
                        UpdateMesh();
                    }
                }
            }
        }

        if (IsCutBetweenPoints(entry, exit))
        {
            cutMadeBetweenPoints = true;
            Debug.Log("Cut made between the two points!");
        }
    }

    private bool IsCutBetweenPoints(Vector3 entry, Vector3 exit)
    {
        bool intersects = LineSegmentsIntersect(entry, exit, selectedPoint1, selectedPoint2);
        Debug.Log($"Checking cut between Entry: {entry}, Exit: {exit}");
        Debug.Log($"Selected Points: Point1: {selectedPoint1}, Point2: {selectedPoint2}");
        Debug.Log($"Cut intersects points: {intersects}");
        return intersects;
    }

    private bool LineSegmentsIntersect(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        float d = (p2.x - p1.x) * (p4.y - p3.y) - (p2.y - p1.y) * (p4.x - p3.x);
        if (d == 0) return false;

        float u = ((p3.x - p1.x) * (p4.y - p3.y) - (p3.y - p1.y) * (p4.x - p3.x)) / d;
        float v = ((p3.x - p1.x) * (p2.y - p1.y) - (p3.y - p1.y) * (p2.x - p1.x)) / d;

        return u >= 0 && u <= 1 && v >= 0 && v <= 1;
    }

    // Public methods for external access
    public bool HasStraightLineCutBeenMade()
    {
        return cutCompleted;
    }

    public void ForceCutBetweenPoints()
    {
        cutMadeBetweenPoints = true;
        Debug.Log("Cut forcibly made between points");
    }


    // Add this method to properly clean up resources
    private void OnDestroy()
    {
        if (point1Marker != null) Destroy(point1Marker);
        if (point2Marker != null) Destroy(point2Marker);
    }

    public Vector3 GetSelectedPoint1() => selectedPoint1;
    public Vector3 GetSelectedPoint2() => selectedPoint2;
}