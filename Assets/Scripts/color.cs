//using UnityEngine;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;

//public class ColorChanger : MonoBehaviour
//{
//    public Color mainColor;
//    public Color cutColor;
//    private Color[] colors;

//    void Start()
//    {
//        GenerateColoredMesh();
//        MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
//        if (meshCollider != null)
//        {
//            meshCollider.convex = true;
//            meshCollider.isTrigger = true;
//        }
//    }

//    void GenerateColoredMesh()
//    {
//        Mesh mesh = new Mesh();
//        // Define vertices
//        Vector3[] vertices = new Vector3[]
//        {
//            // First Triangle
//            new Vector3(-1, 0, 0),
//            new Vector3(1, 0, 0),
//            new Vector3(0, 0, -1),

//            // Second Triangle
//            new Vector3(-1, 0, 0),
//            new Vector3(0, 0, 1),
//            new Vector3(1, 0, 0),

//            // Third Triangle
//            new Vector3(1, 0, 0),
//            new Vector3(0, 0, 1),
//            new Vector3(1, 0, 1)
//        };

//        List<Vector3> verticesList = vertices.ToList();

//        // Define triangle indices
//        int[] triangles = new int[]
//        {
//            0, 1, 2, // First Triangle
//            3, 4, 5, // Second Triangle
//            6, 7, 8  // Third Triangle
//        };

//        List<int> trianglesList = triangles.ToList();

//        // Initialize colors array and set each vertex to orange
//        colors = new Color[triangles.Length];
//        for (int i = 0; i < colors.Length; i++)
//        {
//            colors[i] = cutColor;
//        }

//        List<Color> colorList = colors.ToList();

//        mesh.vertices = verticesList.ToArray();
//        mesh.triangles = trianglesList.ToArray();
//        mesh.colors = colorList.ToArray();

//        // Assign the mesh to the MeshFilter component
//        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
//        meshFilter.mesh = mesh;

//        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
//        Material mat = new Material(Shader.Find("Custom/VertexColorShader"));
//        renderer.material = mat;
//    }
//}



using UnityEngine;

public class ColorChanger : MonoBehaviour
{
    public Color mainColor;
    public Color cutColor;// Set alpha to 0.5 for transparency

    void Start()
    {
        GenerateColoredMesh();
    }

    void GenerateColoredMesh()
    {
        Mesh mesh = new Mesh();

        // Define vertices
        Vector3[] vertices = new Vector3[]
        {
            // First Triangle
            new Vector3(-1, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 0, -1),

            // Second Triangle
            new Vector3(-1, 0, 0),
            new Vector3(0, 0, 1),
            new Vector3(1, 0, 0),

            // Third Triangle
            new Vector3(1, 0, 0),
            new Vector3(0, 0, 1),
            new Vector3(1, 0, 1)
        };

        // Define triangle indices
        int[] triangles = new int[]
        {
            0, 1, 2,
            3, 4, 5,
            6, 7, 8
        };

        // Assign vertices and triangles to mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        // Apply vertex colors
        Color[] colors = new Color[vertices.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = mainColor;
        }
        mesh.colors = colors;

        // Assign the mesh to the MeshFilter component
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        // Create a material with the custom transparent shader
        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
        Material mat = new Material(Shader.Find("Custom/VertexColorTransparentShader"));
        mat.SetColor("_Color", mainColor); // Set transparency color
        renderer.material = mat;
    }
}