using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stitch : MonoBehaviour
{
    int ca;
    private Dictionary<int, Vector3> triangleCenters;
    public GameObject interactableObject;
    private bool call;
    Mesh mesh;
    public Color cutColor;
    public Color mainColor;
    public GameObject mesh3;
    public GameObject sword;
    bool stit;
    List<Vector3> verts;
    List<int> meshTriangles = new List<int>();
    List<Color> meshColors = new List<Color>();
    Cut scr;
    Sword sw;
    private int[] triangles;
    private Color[] colors;
    private Vector3[] vertices;
    // Start is called before the first frame update
    void Start()
    {
        call = false;
        scr = mesh3.GetComponent<Cut>();
        sw = sword.GetComponent<Sword>();
        verts = new List<Vector3>();
        ca = 0;
    }

    // Update is called once per frame
    void Update()
    {
        stit = sw.stit;
        if(stit && !call)
        {
            GenerateColoredMesh();
            call = true;
        }
        else if(stit == false && call == true)
        {
            call = false;
            scr.vertices2.Clear();
        }
    }

    void GenerateColoredMesh()
    {
        ca+=1;
        mesh = new Mesh();
        int ver = verts.Count;
        for(int i = 0;i<scr.vertices2.Count;i++)
        {
            verts.Add(scr.vertices2[i]);
        }
        //verts = scr.vertices2;
        for(int i = ver;i<verts.Count; i++)
        {
            Debug.Log(verts[i]);
            meshTriangles.Add(i);
            meshColors.Add(cutColor);
        }
        mesh.vertices = verts.ToArray();
        mesh.triangles = meshTriangles.ToArray();
        mesh.colors = meshColors.ToArray();

        CacheTriangleCenters();

        if(ca == 1)
        {
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
        Material mat = new Material(Shader.Find("Custom/VertexColorTransparentShader"));
        renderer.material = mat;

        MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        meshCollider.convex = true;
        meshCollider.isTrigger = true;
        }
        else
        {
            //recalculate mesh;
            mesh.RecalculateNormals();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Check if the colliding object is the interactable object
        if (other.gameObject == interactableObject)
        {
            Vector3 contactPoint = other.ClosestPoint(transform.position);
            ChangeNearbyTriangleColor(contactPoint, mainColor);
        }
    }

    void CacheTriangleCenters()
    {
        triangleCenters = new Dictionary<int, Vector3>();
        triangles = meshTriangles.ToArray();
        vertices = verts.ToArray();
        colors = meshColors.ToArray();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            // Calculate the center of each triangle
            Vector3 center = (vertices[triangles[i]] + vertices[triangles[i + 1]] + vertices[triangles[i + 2]]) / 3;
            triangleCenters.Add(i, center);
        }
    }

    void ChangeNearbyTriangleColor(Vector3 contactPoint, Color newColor)
    {
        HashSet<int> updatedTriangles = new HashSet<int>();

        foreach (var entry in triangleCenters)
        {
            int triangleIndex = entry.Key;
            Vector3 triangleCenter = entry.Value;

            // Check if triangle center is within threshold distance from contact point
            if (Vector3.Distance(triangleCenter, contactPoint) < 0.5f)
            {
                // Only update if this triangle hasn't been colored yet
                if (!updatedTriangles.Contains(triangleIndex))
                {
                    colors[triangles[triangleIndex]] = newColor;
                    colors[triangles[triangleIndex + 1]] = newColor;
                    colors[triangles[triangleIndex + 2]] = newColor;

                    updatedTriangles.Add(triangleIndex);
                }
            }
        }

        // Apply updated colors to the mesh
        mesh.colors = colors;
    }
}
