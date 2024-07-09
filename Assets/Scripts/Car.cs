using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class Car : MonoBehaviour
{
    Mesh mesh;
    List<Vector3> vertices;
    List<int> triangles;
    int numVertices;
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
        numVertices = vertices.Count;
        UpdateMesh();

        MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
        if (meshCollider != null)
        {
            meshCollider.convex = false;
            meshCollider.isTrigger = false;
        }
    }

    void CreateShape()
    {
        string filePath = "/Users/keshavashivapuramharish/Desktop/Capstone/MeshGenerator/Assets/Mesh/meshVertices.txt";
        vertices = LoadVerticesFromFile(filePath);

        filePath = "/Users/keshavashivapuramharish/Desktop/Capstone/MeshGenerator/Assets/Mesh/MeshTriangles.txt";
        triangles = LoadTrianglesFromFile(filePath);
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
}
