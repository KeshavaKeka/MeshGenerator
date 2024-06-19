using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class Meshcuts : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;

    int xSize = 20;
    int ySize = 10;

    int numCuts = 25;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        CreateShape();
        UpdateMesh();

        PrintVertices();

        // Move the mesh to the left by 0.75m
        MoveMesh(new Vector3(-0.75f, 0, 0));
    }

    void CreateShape()
    {
        vertices = new Vector3[(xSize + 1) * (ySize + 1)];

        for (int y = 0, i = 0; y <= ySize; y++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float xPos = (float)x / xSize;
                float yPos = (float)y / ySize;
                vertices[i] = new Vector3(xPos, yPos, 0);
                i++;
            }
        }

        int numQuads = xSize * ySize;
        triangles = new int[numQuads * 6];

        int vert = 0;
        int tris = 0;

        for (int y = 0; y < ySize; y++)
        {
            for (int x = 0; x < xSize; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + (xSize + 1) + 0;
                triangles[tris + 2] = vert + (xSize + 1) + 1;
                triangles[tris + 3] = vert + 0;
                triangles[tris + 4] = vert + (xSize + 1) + 1;
                triangles[tris + 5] = vert + 1;

                vert++;
                tris += 6;
            }
            vert++;
        }
    }

    void UpdateMesh()
    {
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals(); //Recalculate normals for smooth shading
    }

    void PrintVertices()
    {
        Debug.Log("Vertex Coordinates:");
        for (int i = 0; i < vertices.Length; i++)
        {
            Debug.Log("Vertex " + i + ": " + vertices[i]);
        }
    }

    void MoveMesh(Vector3 offset)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] += offset;
        }
        UpdateMesh();
    }

    void CreateCut(int numCuts)
    {
        if (numCuts <= 0)
        {
            Debug.LogWarning("Invalid number of cuts.");
            return;
        }

        // Track used edges to avoid duplicate cuts
        HashSet<string> usedEdges = new HashSet<string>();
        List<Vector3> newVertices = new List<Vector3>(vertices);
        List<int> newTriangles = new List<int>(triangles);

        System.Random rand = new System.Random();

        for (int i = 0; i < numCuts; i++)
        {
            int randomQuad = rand.Next(xSize * ySize);
            int triIndex = randomQuad * 6; // 6 indices per quad (2 triangles)

            // Define edges of the quad
            int[,] quadEdges = {
                { triangles[triIndex + 0], triangles[triIndex + 1] },
                { triangles[triIndex + 1], triangles[triIndex + 2] },
                { triangles[triIndex + 3], triangles[triIndex + 4] },
                { triangles[triIndex + 4], triangles[triIndex + 5] }
            };

            // Choose a random edge to cut
            int edgeToCut = rand.Next(quadEdges.GetLength(0));
            int v1 = quadEdges[edgeToCut, 0];
            int v2 = quadEdges[edgeToCut, 1];

            // Check if this edge was already cut
            string edgeKey = v1 < v2 ? $"{v1}-{v2}" : $"{v2}-{v1}";
            if (usedEdges.Contains(edgeKey))
            {
                i--; // Try another edge
                continue;
            }
            usedEdges.Add(edgeKey);

            // Duplicate the vertices
            Vector3 newV1 = newVertices[v1];
            Vector3 newV2 = newVertices[v2];
            int newV1Index = newVertices.Count;
            int newV2Index = newVertices.Count + 1;
            newVertices.Add(newV1);
            newVertices.Add(newV2);

            // Slightly offset the new vertices to create a visible gap
            Vector3 direction = (newV2 - newV1).normalized * 0.01f; // Adjust the gap size as needed
            newVertices[newV1Index] += direction;
            newVertices[newV2Index] -= direction;

            // Update triangles to use new vertices for the cut
            for (int j = 0; j < newTriangles.Count; j++)
            {
                if (newTriangles[j] == v1 && newTriangles[(j + 1) % 3 == 0 ? j - 2 : j + 1] == v2)
                {
                    newTriangles[j] = newV1Index;
                    newTriangles[(j + 1) % 3 == 0 ? j - 2 : j + 1] = newV2Index;
                }
                else if (newTriangles[j] == v2 && newTriangles[(j + 1) % 3 == 0 ? j - 2 : j + 1] == v1)
                {
                    newTriangles[j] = newV2Index;
                    newTriangles[(j + 1) % 3 == 0 ? j - 2 : j + 1] = newV1Index;
                }
            }
        }

        // Update mesh with new vertices and triangles
        vertices = newVertices.ToArray();
        triangles = newTriangles.ToArray();

        UpdateMesh();
    }

    // Update is called once per frame
    void Update()
    {
        // Check for 'R' key press
        if (Input.GetKeyDown(KeyCode.R))
        {
            CreateCut(numCuts);
        }
    }
}