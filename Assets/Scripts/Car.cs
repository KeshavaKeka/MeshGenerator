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
    Vector3 entry;
    Vector3 exit;
    Vector3 currentPosition;
    int previousTriangleID = -1;
    int currentTriangleID = -1;
    private Vector3 previousPosition;
    public GameObject collidingObject;
    private int lastHighlightedTriangle = -1;
    public GameObject stick;

    private Vector3 stickPreviousPosition;

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
        stickPreviousPosition = stick.transform.position;
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


    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject == stick)
        {

        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.gameObject == stick)
        {
            Vector3 closestPoint = other.ClosestPoint(transform.position);
            Vector3 localHitPoint = transform.InverseTransformPoint(closestPoint);
            int hitTriangleIndex = FindHitTriangle(localHitPoint);

            if (hitTriangleIndex != -1)
            {
                Vector3 worldContactPoint = transform.TransformPoint(localHitPoint);
                //WHAT IS THE PURPOSE OF worldContactPoint AND HOW IS IT DIFFERENT FROM localHitPoint;

                PointTriangle(worldContactPoint, vertices[hitTriangleIndex * 3], vertices[hitTriangleIndex * 3 + 1], vertices[hitTriangleIndex * 3 + 2]);

            }
        }
    }

    int FindHitTriangle(Vector3 localHitPoint)
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

        if((u >= 0) && (v >= 0) && (u + v < 1))
        {
            Debug.Log("Yes point lies on the triangle.");
        }
    }
}