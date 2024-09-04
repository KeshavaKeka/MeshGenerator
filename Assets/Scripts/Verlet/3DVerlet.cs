using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Verlet3D : MonoBehaviour
{
    public string objFilePath = "Assets/Resources/cylinder.obj";
    public float cutDistance = 0.2f;
    public Vector3 nodeSize = new Vector3(0.01f, 0.01f, 0.01f);
    public float lineThickness = 0.001f;
    public GameObject swordPrefab;
    public Transform swordSpawnPoint;

    private GameObject sword;
    public Material nodeMaterial;
    public Material edgeMaterial;
    public Material triangleMaterial;

    private List<GameObject> spheres;
    private List<Particle> particles;
    private List<Connector> connectors;
    private List<Triangle> triangles;

    void Start()
    {
        sword = Instantiate(swordPrefab, swordSpawnPoint.position, Quaternion.identity);

        spheres = new List<GameObject>();
        particles = new List<Particle>();
        connectors = new List<Connector>();
        triangles = new List<Triangle>();

        LoadObjFile(objFilePath);

        // Add a collider to detect cuts
        BoxCollider collider = gameObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;

        // Output information to separate files
        OutputVertices("vertices.txt");
        OutputEdges("edges.txt");
        OutputTriangles("triangles.txt");
    }

    void LoadObjFile(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath);
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangleIndices = new List<int>();

        foreach (string line in lines)
        {
            string[] parts = line.Split(' ');
            if (parts[0] == "v")
            {
                float x = float.Parse(parts[1]);
                float y = float.Parse(parts[2]);
                float z = float.Parse(parts[3]);
                vertices.Add(new Vector3(x, y, z));
            }
            else if (parts[0] == "f")
            {
                for (int i = 1; i <= 3; i++)
                {
                    int vertexIndex = int.Parse(parts[i].Split('/')[0]) - 1;
                    triangleIndices.Add(vertexIndex);
                }
            }
        }

        // Create particles and spheres
        for (int i = 0; i < vertices.Count; i++)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var mat = sphere.GetComponent<Renderer>();
            mat.material = nodeMaterial;
            sphere.transform.position = vertices[i];
            sphere.transform.localScale = nodeSize;

            Particle point = new Particle
            {
                pinnedPos = vertices[i],
                pos = vertices[i],
                oldPos = vertices[i],
                gravity = 0f, // Disable gravity
                pinned = false // No pinned vertices
            };

            spheres.Add(sphere);
            particles.Add(point);
        }

        // Create connectors and triangles
        for (int i = 0; i < triangleIndices.Count; i += 3)
        {
            int index0 = triangleIndices[i];
            int index1 = triangleIndices[i + 1];
            int index2 = triangleIndices[i + 2];

            CreateConnector(spheres[index0], spheres[index1], particles[index0], particles[index1]);
            CreateConnector(spheres[index1], spheres[index2], particles[index1], particles[index2]);
            CreateConnector(spheres[index2], spheres[index0], particles[index2], particles[index0]);

            CreateTriangle(spheres[index0], spheres[index1], spheres[index2]);
        }
    }

    void CreateConnector(GameObject p0, GameObject p1, Particle point0, Particle point1)
    {
        LineRenderer line = new GameObject("Line").AddComponent<LineRenderer>();
        Connector connector = new Connector
        {
            p0 = p0,
            p1 = p1,
            point0 = point0,
            point1 = point1,
            lineRender = line
        };
        connector.lineRender.material = edgeMaterial;
        connectors.Add(connector);
    }

    void CreateTriangle(GameObject p0, GameObject p1, GameObject p2)
    {
        GameObject triangleObj = new GameObject("Triangle");

        // Set the layer for the triangle
        triangleObj.layer = LayerMask.NameToLayer("VerletMesh");

        MeshFilter meshFilter = triangleObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = triangleObj.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = triangleObj.AddComponent<MeshCollider>();

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] { p0.transform.position, p1.transform.position, p2.transform.position };
        mesh.triangles = new int[] { 0, 1, 2 };
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshRenderer.material = triangleMaterial;
        meshCollider.sharedMesh = mesh;

        Triangle triangle = new Triangle
        {
            p0 = p0,
            p1 = p1,
            p2 = p2,
            meshFilter = meshFilter,
            meshRenderer = meshRenderer,
            meshCollider = meshCollider
        };

        triangles.Add(triangle);
    }

    private void OnTriggerEnter(Collider other)
    {
        SwordCutter sword = other.GetComponent<SwordCutter>();
        if (sword != null)
        {
            Vector3 hitPoint = sword.transform.position;
            Cut(hitPoint);
        }
    }

    public void Cut(Vector3 hitPoint)
    {
        Debug.Log("Cut");
        for (int i = connectors.Count - 1; i >= 0; i--)
        {
            Vector3 closestPoint = ClosestPointOnLineSegment(connectors[i].point0.pos, connectors[i].point1.pos, hitPoint);
            float distToLine = Vector3.Distance(hitPoint, closestPoint);

            if (distToLine <= cutDistance)
            {
                RemoveConnector(connectors[i]);
                connectors.RemoveAt(i);
            }
        }
    }

    Vector3 ClosestPointOnLineSegment(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
    {
        Vector3 line = lineEnd - lineStart;
        float lineLength = line.magnitude;
        line.Normalize();

        Vector3 v = point - lineStart;
        float d = Vector3.Dot(v, line);
        d = Mathf.Clamp(d, 0f, lineLength);

        return lineStart + line * d;
    }

    void RemoveConnector(Connector connector)
    {
        Destroy(connector.lineRender.gameObject);

        for (int i = triangles.Count - 1; i >= 0; i--)
        {
            Triangle triangle = triangles[i];
            if (ContainsConnector(triangle, connector))
            {
                Destroy(triangle.meshRenderer.gameObject);
                triangles.RemoveAt(i);
            }
        }
    }

    bool ContainsConnector(Triangle triangle, Connector connector)
    {
        return (triangle.p0 == connector.p0 && triangle.p1 == connector.p1) ||
               (triangle.p1 == connector.p0 && triangle.p2 == connector.p1) ||
               (triangle.p2 == connector.p0 && triangle.p0 == connector.p1) ||
               (triangle.p0 == connector.p1 && triangle.p1 == connector.p0) ||
               (triangle.p1 == connector.p1 && triangle.p2 == connector.p0) ||
               (triangle.p2 == connector.p1 && triangle.p0 == connector.p0);
    }

    void FixedUpdate()
    {
        CheckSwordCollision();
        for (int p = 0; p < particles.Count; p++)
        {
            Particle point = particles[p];
            if (!point.pinned)
            {
                Vector3 vel = (point.pos - point.oldPos) * point.friction;
                point.oldPos = point.pos;
                point.pos += vel;
                point.pos += Vector3.down * point.gravity * Time.fixedDeltaTime;
            }
        }

        for (int i = 0; i < connectors.Count; i++)
        {
            float dist = Vector3.Distance(connectors[i].point0.pos, connectors[i].point1.pos);
            float startDistance = Vector3.Distance(connectors[i].point0.pinnedPos, connectors[i].point1.pinnedPos);
            float error = Mathf.Abs(dist - startDistance);

            Vector3 changeDir = (dist > startDistance) ?
                (connectors[i].point0.pos - connectors[i].point1.pos).normalized :
                (connectors[i].point1.pos - connectors[i].point0.pos).normalized;

            Vector3 changeAmount = changeDir * error;
            if (!connectors[i].point0.pinned)
                connectors[i].point0.pos -= changeAmount * 0.5f;
            if (!connectors[i].point1.pinned)
                connectors[i].point1.pos += changeAmount * 0.5f;
        }

        for (int p = 0; p < particles.Count; p++)
        {
            spheres[p].transform.position = particles[p].pos;
        }

        for (int i = 0; i < connectors.Count; i++)
        {
            Vector3[] points = new Vector3[] {
                connectors[i].p0.transform.position,
                connectors[i].p1.transform.position
            };
            connectors[i].lineRender.SetPositions(points);
            connectors[i].lineRender.startWidth = lineThickness;
            connectors[i].lineRender.endWidth = lineThickness;
        }

        for (int i = 0; i < triangles.Count; i++)
        {
            Triangle triangle = triangles[i];
            Vector3[] vertices = new Vector3[] {
                triangle.p0.transform.position,
                triangle.p1.transform.position,
                triangle.p2.transform.position
            };
            triangle.meshFilter.mesh.vertices = vertices;
            triangle.meshFilter.mesh.RecalculateNormals();
        }
    }

    void CheckSwordCollision()
    {
        Vector3 swordTip = sword.transform.position + sword.transform.forward * (sword.transform.localScale.z / 2);
        Vector3 swordBase = sword.transform.position - sword.transform.forward * (sword.transform.localScale.z / 2);

        for (int i = connectors.Count - 1; i >= 0; i--)
        {
            Vector3 closestPoint = ClosestPointOnLineSegment(swordBase, swordTip, connectors[i].point0.pos);
            float distToPoint0 = Vector3.Distance(closestPoint, connectors[i].point0.pos);
            float distToPoint1 = Vector3.Distance(closestPoint, connectors[i].point1.pos);

            if (distToPoint0 <= cutDistance || distToPoint1 <= cutDistance)
            {
                RemoveConnector(connectors[i]);
                connectors.RemoveAt(i);
            }
        }
    }

    void OutputVertices(string fileName)
    {
        using (StreamWriter writer = new StreamWriter(fileName))
        {
            for (int i = 0; i < particles.Count; i++)
            {
                writer.WriteLine($"Vertex {i}: {particles[i].pos}");
            }
        }
    }

    void OutputEdges(string fileName)
    {
        using (StreamWriter writer = new StreamWriter(fileName))
        {
            for (int i = 0; i < connectors.Count; i++)
            {
                writer.WriteLine($"Edge {i}: {connectors[i].point0.pos} - {connectors[i].point1.pos}");
            }
        }
    }

    void OutputTriangles(string fileName)
    {
        using (StreamWriter writer = new StreamWriter(fileName))
        {
            for (int i = 0; i < triangles.Count; i++)
            {
                writer.WriteLine($"Triangle {i}: {triangles[i].p0.transform.position}, {triangles[i].p1.transform.position}, {triangles[i].p2.transform.position}");
            }
        }
    }

    public class Connector
    {
        public LineRenderer lineRender;
        public GameObject p0;
        public GameObject p1;
        public Particle point0;
        public Particle point1;
    }

    public class Triangle
    {
        public GameObject p0;
        public GameObject p1;
        public GameObject p2;
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
        public MeshCollider meshCollider;
    }

    public class Particle
    {
        public bool pinned = false;
        public Vector3 pinnedPos;
        public Vector3 pos;
        public Vector3 oldPos;
        public float gravity = 0f;
        public float friction = 0.99f;
    }
}