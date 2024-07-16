using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class Car : MonoBehaviour
{
    public string filePathVertices = "Assets/Mesh/meshVertices.txt";
    public string filePathTriangles = "Assets/Mesh/MeshTriangles.txt";
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
    float offsetDistance = 0.02f;
    public GameObject stick;

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
    }

    void Update()
    {
        if (currentPosition != null)
        {
            previousTriangleID = currentTriangleID;
            currentTriangleID = GetTriangleID(currentPosition);

            if (currentTriangleID != 1 && previousTriangleID == -1)
            {
                Vector3 transitionPoint = CalculateTransitionPoint(currentPosition, currentTriangleID);
                entry = transitionPoint;
                entOnEdge = true;
            }
            if (currentTriangleID == -1 && previousTriangleID != -1)
            {
                Vector3 transitionPoint = CalculateTransitionPoint(currentPosition, previousTriangleID);
                exit = transitionPoint;
                exitOnEdge = true;
                //Debug.LogFormat("Call index {0}",previousTriangleID);
                //Debug.LogFormat("Entry: {0:0.000}", entry);
                //Debug.LogFormat("Exit: {0:0.000}", exit);
                getCut(previousTriangleID, entry, exit, 1);
                entry = exit;
                entOnEdge = true;
            }
            else if (previousTriangleID != -1 && currentTriangleID != previousTriangleID)
            {
                // Calculate the actual transition point
                Vector3 transitionPoint = CalculateTransitionPoint(currentPosition, previousTriangleID, currentTriangleID);

                // Log the transition with the adjusted transition point
                //Debug.LogFormat("Transition from Triangle {0} to Triangle {1} at Position: {2:0.000}",
                //previousTriangleID, currentTriangleID, transitionPoint);
                exit = transitionPoint;
                //Debug.LogFormat("Exit lallala: {0:0.000}", exit);
                exitOnEdge = true;
                //Debug.LogFormat("Call index {0}", previousTriangleID);
                //Debug.LogFormat("Entry: {0:0.000}", entry);
                //Debug.LogFormat("Exit: {0:0.000}", exit);
                getCut(previousTriangleID, entry, exit, 1);
                entry = exit;
                //Debug.Log("Entry: {0:0.000}", entry);
                entOnEdge = true;
            }
        }
    }

    void CreateShape()
    {
        vertices = LoadVerticesFromFile(filePathVertices);

        triangles = LoadTrianglesFromFile(filePathTriangles);
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
    }

    List<int> LoadTrianglesFromFile(string filepath)
    {
        List<int> triangles = new List<int>();

        try
        {
            using (StreamReader reader = new StreamReader(filepath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (int.TryParse(line, out int triangleIndex))
                    {
                        triangles.Add(triangleIndex);
                    }
                    else
                    {
                        Debug.LogWarning($"Could not parse line '{line}' to an integer.");
                    }
                }
            }
        }
        catch (FileNotFoundException e)
        {
            Debug.LogError($"File not found: {e.Message}");
        }
        catch (IOException e)
        {
            Debug.LogError($"I/O error while reading file: {e.Message}");
        }

        return triangles;
    }

    List<Vector3> LoadVerticesFromFile(string fileName)
    {
        List<Vector3> vertices = new List<Vector3>();

        if (File.Exists(fileName))
        {
            using (StreamReader reader = new StreamReader(fileName))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] values = line.Split(',');

                    if (values.Length == 3)
                    {
                        if (float.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                            float.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
                            float.TryParse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
                        {
                            vertices.Add(new Vector3(x, y, z));
                        }
                        else
                        {
                            Debug.LogWarning($"Unable to parse line: {line}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid line format: {line}");
                    }
                }
            }
        }
        else
        {
            Debug.LogError($"File not found: {fileName}");
        }

        return vertices;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == stick)
        {
            Vector3 contactPoint = other.ClosestPointOnBounds(transform.position);
            Vector3 localHitPoint = transform.InverseTransformPoint(contactPoint);
            //Debug.Log(contactPoint);
            if (contactPoint != null)
            {
                entry = transform.TransformPoint(localHitPoint);
                entOnEdge = false;
                Debug.LogFormat("Entry: {0:0.000}", entry);

                // Determine and log the initial triangle ID for the entry point
                previousTriangleID = GetTriangleID(entry);
                currentTriangleID = previousTriangleID;
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.gameObject == stick)
        {
            Vector3 closestPoint = other.ClosestPoint(transform.position);
            Vector3 localHitPoint = transform.InverseTransformPoint(closestPoint);
            int hitTriangleIndex = GetTriangleID(localHitPoint);

            if (hitTriangleIndex != -1)
            {
                currentPosition = transform.TransformPoint(localHitPoint);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == stick)
        {
            Vector3 contactPoint = other.ClosestPointOnBounds(transform.position);
            Vector3 localHitPoint = transform.InverseTransformPoint(contactPoint);
            if (contactPoint != null)
            {
                exit = transform.TransformPoint(localHitPoint);
                exitOnEdge = false;
                Debug.LogFormat("Exit: {0:0.000}", exit);

                // Determine and log the final triangle ID for the exit point
                int exitTriangleID = GetTriangleID(exit);
                getCut(exitTriangleID, entry, exit, 0);
                entry = exit;
                entOnEdge = false;
            }
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

    int GetTriangleID(Vector3 localHitPoint)
    {
        for (int i = 0; i < triangles.Count; i += 3)
        {
            Vector3 v1 = vertices[triangles[i]];
            Vector3 v2 = vertices[triangles[i + 1]];
            Vector3 v3 = vertices[triangles[i + 2]];

            if (PointInTriangle(localHitPoint, v1, v2, v3))
            {
                return i / 3;
            }
        }
        return -1;
    }

    bool PointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 v0 = c - a;
        Vector3 v1 = b - a;
        Vector3 v2 = p - a;

        float dot00 = Vector3.Dot(v0, v0);
        float dot01 = Vector3.Dot(v0, v1);
        float dot02 = Vector3.Dot(v0, v2);
        float dot11 = Vector3.Dot(v1, v1);
        float dot12 = Vector3.Dot(v1, v2);

        float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
        float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        return (u >= 0) && (v >= 0) && (u + v < 1);
    }

    void PointTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 v0 = c - a;
        Vector3 v1 = b - a;
        Vector3 v2 = p - a;

        float dot00 = Vector3.Dot(v0, v0);
        float dot01 = Vector3.Dot(v0, v1);
        float dot02 = Vector3.Dot(v0, v2);
        float dot11 = Vector3.Dot(v1, v1);
        float dot12 = Vector3.Dot(v1, v2);

        float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
        float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        if ((u >= 0) && (v >= 0) && (u + v < 1))
        {
            Debug.Log("Yes point lies on the triangle.");
        }
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
        //ADD TRIANGLES TO THE RESTITCHING ARRAY.
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
        else
        {
            Debug.LogFormat("Point {0:0.000} not on any edge", vertex);
            return null;
        }
    }

    public static bool IsClockwise(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 edge1 = p2 - p1;
        Vector3 edge2 = p3 - p1;
        Vector3 normal = Vector3.Cross(edge1, edge2);
        float dotProduct = Vector3.Dot(normal, Vector3.up);
        return dotProduct < 0;
    }

    void getCut(int id, Vector3 entry, Vector3 exit, int which)
    {
        if (id >= 0)
        {
            Vector3[] triangleVertices = GetTriangleVertices(id);
            bool entryCheck = triangleVertices[0] != entry && triangleVertices[1] != entry && triangleVertices[2] != entry;
            bool exitCheck = triangleVertices[0] != exit && triangleVertices[1] != exit && triangleVertices[2] != exit;
            if (entryCheck && exitCheck)
            {
                //print("holla");
                //Debug.Log(triangleVertices[0]);
                //Debug.Log(triangleVertices[1]);
                //Debug.Log(triangleVertices[2]);
                //Debug.Log(entry);
                //Debug.Log(exit);
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
                        Vector3 transitionPoint = CalculateTransitionPoint(exit, id);
                        exit = transitionPoint;
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
                        Vector3 transitionPoint = CalculateTransitionPoint(entry, id);
                        entry = transitionPoint;
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
        //Debug.Log("Exit GetCut");
    }
}