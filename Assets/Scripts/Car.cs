using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class Car : MonoBehaviour
{
    public string filePathVertices = "Assets/Mesh/meshVertices.txt";
    public string filePathTriangles= "Assets/Mesh/MeshTriangles.txt";
    Mesh mesh;
    List<Vector3> vertices;
    List<int> triangles;
    int numVertices;
    Vector3 entry;
    Vector3 exit;
    Vector3 currentPosition;
    int previousTriangleID = -1;
    int currentTriangleID = -1;
    private Vector3 previousPosition;
    public GameObject collidingObject;
    public Material defaultMaterial;
    public Material highlightMaterial;
    private int lastHighlightedTriangle = -1;
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
        previousPosition = collidingObject.transform.position;
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
        // Get the position of the colliding object
        Vector3 colliderPosition = other.transform.position;

        // Calculate direction from the colliding object to this mesh object
        Vector3 direction = transform.position - colliderPosition;

        // Perform the raycast
        RaycastHit hit;
        if (Physics.Raycast(colliderPosition, direction, out hit))
        {
            // Check if the hit object is this mesh
            if (hit.collider.gameObject == gameObject)
            {
                Vector3 intersectionPoint = hit.point;
                Debug.Log($"Intersection point on mesh: {intersectionPoint}");

                // You can do further processing with the intersection point here
                // For example, you might want to mark this point on the mesh, or use it for cutting logic
            }
        }
    }
    void Update()
    {
        Vector3 currentPosition = collidingObject.transform.position;
        Vector3 movement = currentPosition - previousPosition;

        if (movement.magnitude > 0)
        {
            CheckMeshIntersection(previousPosition, currentPosition);
        }

        previousPosition = currentPosition;
    }

    void CheckMeshIntersection(Vector3 fromPosition, Vector3 toPosition)
    {
        Vector3[] meshVertices = mesh.vertices;
        int[] meshTriangles = mesh.triangles;

        for (int i = 0; i < meshTriangles.Length; i += 3)
        {
            Vector3 v1 = transform.TransformPoint(meshVertices[meshTriangles[i]]);
            Vector3 v2 = transform.TransformPoint(meshVertices[meshTriangles[i + 1]]);
            Vector3 v3 = transform.TransformPoint(meshVertices[meshTriangles[i + 2]]);

            Vector3 intersectionPoint;
            if (LinePlaneIntersection(out intersectionPoint, fromPosition, toPosition, v1, v2, v3))
            {
                int triangleId = i / 3;
                Debug.Log($"Intersection point on mesh: {intersectionPoint}, Triangle ID: {triangleId}");

                HighlightTriangle(triangleId);
                return; // Exit after finding the first intersection
            }
        }
    }

    bool LinePlaneIntersection(out Vector3 intersection, Vector3 linePoint, Vector3 lineVec, Vector3 planeP1, Vector3 planeP2, Vector3 planeP3)
    {
        Vector3 planeNormal = Vector3.Cross(planeP2 - planeP1, planeP3 - planeP1).normalized;
        Vector3 planePosition = planeP1;

        float planeD = -Vector3.Dot(planeNormal, planePosition);
        float ad = Vector3.Dot(linePoint, planeNormal);
        float bd = Vector3.Dot(lineVec, planeNormal);
        float t = (-planeD - ad) / bd;

        intersection = linePoint + t * lineVec;

        Vector3 cp1 = Vector3.Cross(planeP2 - planeP1, intersection - planeP1);
        Vector3 cp2 = Vector3.Cross(planeP3 - planeP2, intersection - planeP2);
        Vector3 cp3 = Vector3.Cross(planeP1 - planeP3, intersection - planeP3);

        if (Vector3.Dot(cp1, planeNormal) >= 0 &&
            Vector3.Dot(cp2, planeNormal) >= 0 &&
            Vector3.Dot(cp3, planeNormal) >= 0 &&
            t >= 0 && t <= 1)
        {
            return true;
        }

        return false;
    }

    void HighlightTriangle(int triangleId)
    {
        if (lastHighlightedTriangle != -1)
        {
            // Reset the previous highlighted triangle
            SetTriangleMaterial(lastHighlightedTriangle, defaultMaterial);
        }

        // Highlight the new triangle
        SetTriangleMaterial(triangleId, highlightMaterial);
        lastHighlightedTriangle = triangleId;
    }

    void SetTriangleMaterial(int triangleId, Material material)
    {
        int[] triangles = mesh.triangles;
        int[] newTriangles = new int[3];
        newTriangles[0] = triangles[triangleId * 3];
        newTriangles[1] = triangles[triangleId * 3 + 1];
        newTriangles[2] = triangles[triangleId * 3 + 2];

        Mesh newMesh = new Mesh();
        newMesh.vertices = mesh.vertices;
        newMesh.triangles = newTriangles;
        newMesh.normals = mesh.normals;
        newMesh.uv = mesh.uv;

        GameObject highlightObject = new GameObject($"HighlightTriangle_{triangleId}");
        highlightObject.transform.SetParent(transform, false);
        highlightObject.transform.localPosition = Vector3.zero;
        highlightObject.transform.localRotation = Quaternion.identity;
        highlightObject.transform.localScale = Vector3.one;

        MeshFilter meshFilter = highlightObject.AddComponent<MeshFilter>();
        meshFilter.mesh = newMesh;

        MeshRenderer meshRenderer = highlightObject.AddComponent<MeshRenderer>();
        meshRenderer.material = material;
    }
}
