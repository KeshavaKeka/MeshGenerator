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

    public float pointDistance = 1f;
    public Color highlightColor = Color.red;
    private Vector3 selectedPoint1;
    private Vector3 selectedPoint2;
    private GameObject point1Marker, point2Marker;

    private bool cutMadeBetweenPoints = false;

    Mesh mesh;
    public List<Vector3> vertices;
    List<int> triangles;
    int numVertices;
    bool entOnEdge = false;
    bool exitOnEdge = false;
    Vector3 entry;
    Vector3 exit;
    Vector3 currentPosition;
    int previousTriangleID = -1;
    int currentTriangleID = -1;


    void Awake()
    {
        Debug.Log("RemTri Awake method called");
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
            Debug.Log("MeshCollider added and set as trigger");
        }

        SelectConstantPoints();
        SetAndVisualizePoints();
    }

    void CreateShape()
    {
        Debug.Log("Creating mesh shape");
        vertices = new List<Vector3>();
        triangles = new List<int>();

        float dx = width / (verticesAlongX - 1);
        float dy = height / (verticesAlongY - 1);

        for (int y = 0; y < verticesAlongY; y++)
        {
            for (int x = 0; x < verticesAlongX; x++)
            {
                float xPos = -width / 2 + x * dx;
                float yPos = y * dy;
                vertices.Add(new Vector3(xPos, yPos, 0));
            }
        }

        for (int y = 0; y < verticesAlongY - 1; y++)
        {
            for (int x = 0; x < verticesAlongX - 1; x++)
            {
                int start = y * verticesAlongX + x;

                triangles.Add(start);
                triangles.Add(start + verticesAlongX);
                triangles.Add(start + verticesAlongX + 1);

                triangles.Add(start);
                triangles.Add(start + verticesAlongX + 1);
                triangles.Add(start + 1);
            }
        }
        Debug.Log($"Mesh created with {vertices.Count} vertices and {triangles.Count / 3} triangles");
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        Debug.Log("Mesh updated");
    }

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
            int yIndex = Mathf.FloorToInt(linePosition / (height / (verticesAlongY - 1)));
            int xStartIndex = Mathf.FloorToInt((start + width / 2) / (width / (verticesAlongX - 1)));
            int xEndIndex = Mathf.FloorToInt((end + width / 2) / (width / (verticesAlongX - 1)));
            selectedPoint1 = new Vector3(start, linePosition, 0);
            selectedPoint2 = new Vector3(end, linePosition, 0);
        }
        else
        {
            start = 0;
            end = Mathf.Min(pointDistance, height);
            int xIndex = Mathf.FloorToInt((linePosition + width / 2) / (width / (verticesAlongX - 1)));
            int yStartIndex = Mathf.FloorToInt(start / (height / (verticesAlongY - 1)));
            int yEndIndex = Mathf.FloorToInt(end / (height / (verticesAlongY - 1)));
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
        marker.transform.localScale = Vector3.one * 0.04f;
        marker.transform.SetParent(this.transform);

        Renderer renderer = marker.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Standard"));
        renderer.material.color = highlightColor;

        return marker;
    }

    void Update()
    {
        if (currentPosition != null)
        {
            previousTriangleID = currentTriangleID;
            currentTriangleID = GetTriangleID(currentPosition);

            if (previousTriangleID != -1 && currentTriangleID != previousTriangleID)
            {
                Vector3 transitionPoint = CalculateTransitionPoint(currentPosition, previousTriangleID, currentTriangleID);
                Debug.LogFormat("Transition from Triangle {0} to Triangle {1} at Position: {2:0.000}",
                    previousTriangleID, currentTriangleID, transitionPoint);
                exit = transitionPoint;
                exitOnEdge = true;
                getCut(previousTriangleID, entry, exit, 1);
                entry = exit;
                entOnEdge = true;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Trigger Enter detected with {other.gameObject.name}");
        Transform contactPoint = other.transform.Find("Contact Point");
        if (contactPoint != null)
        {
            entry = new Vector3(contactPoint.position.x, contactPoint.position.y, 0);
            entOnEdge = false;
            Debug.LogFormat("Entry: {0:0.000}", entry);

            previousTriangleID = GetTriangleID(entry);
            currentTriangleID = previousTriangleID;
            Debug.Log($"Initial Triangle ID: {previousTriangleID}");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        Debug.Log($"Trigger Stay with {other.gameObject.name}");
        Transform contactPoint = other.transform.Find("Contact Point");
        if (contactPoint != null)
        {
            currentPosition = new Vector3(contactPoint.position.x, contactPoint.position.y, 0);
            Debug.LogFormat("Current Position: {0:0.000}", currentPosition);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"Trigger Exit detected with {other.gameObject.name}");
        Transform contactPoint = other.transform.Find("Contact Point");
        if (contactPoint != null)
        {
            exit = new Vector3(contactPoint.position.x, contactPoint.position.y, 0);
            exitOnEdge = false;
            Debug.LogFormat("Exit: {0:0.000}", exit);

            int exitTriangleID = GetTriangleID(exit);
            Debug.Log($"Exit Triangle ID: {exitTriangleID}");
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
        Debug.LogWarning($"Point {point} is not in any triangle");
        return -1;
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

        Vector3[] triangle1Vertices = GetTriangleVertices(triangleID1);
        Vector3[] triangle2Vertices = GetTriangleVertices(triangleID2);

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

        return null;
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
        Debug.Log($"Attempting to remove quad for triangle ID: {triangleID}");
        if (triangleID < 0 || triangleID >= triangles.Count / 3)
        {
            Debug.LogError("Invalid triangle ID");
            return;
        }

        int triangle1Start = triangleID * 3;
        int triangle2Start = GetPairedTriangleStartIndex(triangleID);

        triangles[triangle1Start] = 0;
        triangles[triangle1Start + 1] = 0;
        triangles[triangle1Start + 2] = 0;

        if (triangle2Start != -1)
        {
            triangles[triangle2Start] = 0;
            triangles[triangle2Start + 1] = 0;
            triangles[triangle2Start + 2] = 0;
        }

        UpdateMesh();
        Debug.Log("Mesh updated after quad removal");
    }

    int GetPairedTriangleStartIndex(int triangleID)
    {
        if (triangleID < 0 || triangleID >= triangles.Count / 3)
        {
            Debug.LogError("Invalid triangle ID");
            return -1;
        }

        int triangleStartIndex = triangleID * 3;

        int v0 = triangles[triangleStartIndex];
        int v1 = triangles[triangleStartIndex + 1];
        int v2 = triangles[triangleStartIndex + 2];

        if (v1 == v2 + 1 || v1 == v2 - 1)
        {
            return (triangleStartIndex % 6 == 0) ? triangleStartIndex + 3 : triangleStartIndex - 3;
        }
        else if (v0 == v2 + 1 || v0 == v2 - 1)
        {
            return (triangleStartIndex % 6 == 0) ? triangleStartIndex + 3 : triangleStartIndex - 3;
        }
        else if (v0 == v1 + 1 || v0 == v1 - 1)
        {
            return (triangleStartIndex % 6 == 0) ? triangleStartIndex + 3 : triangleStartIndex - 3;
        }

        return -1;
    }

    void getCut(int id, Vector3 entry, Vector3 exit, int which)
    {
        Debug.Log($"Attempting to cut. Triangle ID: {id}, Entry: {entry}, Exit: {exit}, Which: {which}");
        if (id > -1)
        {
            Debug.Log($"Removing quad for triangle ID: {id}");
            RemoveQuad(id);

            // Check if the cut is made between the two points
            if (IsCutBetweenPoints(entry, exit))
            {
                cutMadeBetweenPoints = true;
                Debug.Log("Cut made between the two points!");
            }
        }
    }

    private bool IsCutBetweenPoints(Vector3 entry, Vector3 exit)
    {
        // Check if the line segment (entry, exit) intersects with (selectedPoint1, selectedPoint2)
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

    public bool HasCutBeenMadeBetweenPoints()
    {
        Debug.Log($"Checking if cut has been made: {cutMadeBetweenPoints}");
        return cutMadeBetweenPoints;
    }

    public void ForceCutBetweenPoints()
    {
        cutMadeBetweenPoints = true;
        Debug.Log("Cut forcibly made between points");
    }
    public void ResetCut()
    {
        cutMadeBetweenPoints = false;
    }

    // Public getter methods
    public Vector3 GetSelectedPoint1() => selectedPoint1;
    public Vector3 GetSelectedPoint2() => selectedPoint2;
}