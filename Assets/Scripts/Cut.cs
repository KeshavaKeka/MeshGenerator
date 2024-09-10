using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class Cut : MonoBehaviour
{
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

    // Stitching variables
    public float stitchThreshold = 0.01f;
    public KeyCode stitchKey = KeyCode.R; // Press 'R' to stitch

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
            if(currentTriangleID == -1 && previousTriangleID != -1)
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

        // New stitching trigger
        if (Input.GetKeyDown(stitchKey))
        {
            StitchMesh();
            Debug.Log("Mesh stitching attempted");
        }
    }

    void CreateShape()
    {
        // Define the vertices of the shape
        vertices = new List<Vector3> {

            new Vector3(1, 0, 0),
            new Vector3(0, 0, 1),
            new Vector3(-1, 0, 0),
            new Vector3(0, 0, -1),
            new Vector3(1.5f, 0, 0.5f)
        };

        // Define the triangles that make up the shape
        triangles = new List<int> {
            0, 3, 2,
            0, 2, 1,
            0, 1, 4
        };
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
        Vector3 contactPoint = other.ClosestPointOnBounds(transform.position);
        //Debug.Log(contactPoint);
        if (contactPoint != null)
        {
            entry = new Vector3(other.transform.position.x, 0, other.transform.position.z);
            entOnEdge = false;
            //Debug.LogFormat("Entry: {0:0.000}", entry);

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
            exitOnEdge = false;
            //Debug.LogFormat("Exit: {0:0.000}", exit);

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
        if (id>=0)
        {
            Vector3[] triangleVertices = GetTriangleVertices(id);
            bool entryCheck = triangleVertices[0] != entry && triangleVertices[1] != entry && triangleVertices[2] != entry;
            bool exitCheck = triangleVertices[0] != exit && triangleVertices[1] != exit && triangleVertices[2] != exit;
            if (entryCheck && exitCheck)
            {
                print("holla");
                Debug.Log(triangleVertices[0]);
                Debug.Log(triangleVertices[1]);
                Debug.Log(triangleVertices[2]);
                Debug.Log(entry);
                Debug.Log(exit);
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
                    if(notEntry[2]!=notExit[2])
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

    // New stitching methods
    void StitchMesh()
    {
        List<Edge> edges = FindCutEdges();
        List<Edge> stitchableEdges = FindStitchableEdges(edges);

        foreach (Edge edge in stitchableEdges)
        {
            StitchEdge(edge);
        }

        UpdateMesh();
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

        // Add new triangles
        triangles.Add(edge.v1);
        triangles.Add(newTriangle1);
        triangles.Add(edge.v2);

        triangles.Add(edge.v2);
        triangles.Add(newTriangle2);
        triangles.Add(edge.v1);

        numVertices = vertices.Count;
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