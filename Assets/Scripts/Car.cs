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
    // Start is called before the first frame update
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

        // Ensure the file exists before attempting to read
        if (File.Exists(fileName))
        {
            // Open the file for reading
            using (StreamReader reader = new StreamReader(fileName))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // Split the line by commas
                    string[] values = line.Split(',');

                    if (values.Length == 3) // Ensure there are exactly 3 values
                    {
                        // Parse the string values to floats
                        if (float.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                            float.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
                            float.TryParse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
                        {
                            // Create a Vector3 from the parsed values and add to the list
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

            Debug.Log($"Loaded {vertices.Count} vertices from {fileName}");
        }
        else
        {
            Debug.LogError($"File not found: {fileName}");
        }

        return vertices;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
