using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stitch : MonoBehaviour
{
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
    // Start is called before the first frame update
    void Start()
    {
        call = false;
        scr = mesh3.GetComponent<Cut>();
        sw = sword.GetComponent<Sword>();
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
    }

    void GenerateColoredMesh()
    {
        mesh = new Mesh();
        verts = scr.vertices2;
        for(int i = 0;i<verts.Count; i++)
        {
            Debug.Log(verts[i]);
            meshTriangles.Add(i);
            meshColors.Add(cutColor);
        }
        mesh.vertices = verts.ToArray();
        mesh.triangles = meshTriangles.ToArray();
        mesh.colors = meshColors.ToArray();

        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
        Material mat = new Material(Shader.Find("Custom/VertexColorTransparentShader"));
        renderer.material = mat;
    }
}
