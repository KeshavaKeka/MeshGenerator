using System.Collections.Generic;
using UnityEngine;

public class Verlet3D : MonoBehaviour
{
    public int rows = 10;
    public int columns = 10;
    public float spacing = 0.5f;
    public float cutDistance = 0.2f;
    public Vector3 nodeSize = new Vector3(0.01f, 0.01f, 0.01f);
    public float lineThickness = 0.001f;
    public GameObject swordPrefab; // Assign this in the Unity Inspector
    public Transform swordSpawnPoint;
    public float offset = 1f;

    public Material nodeMaterial;
    public Material edgeMaterial;
    public Material triangleMaterial;
    private GameObject sword;

    private List<GameObject> spheres;
    private List<Particle> particles;
    private List<Connector> connectors;
    private List<Triangle> triangles;
    

    void Start()
    {
        sword = Instantiate(swordPrefab, swordSpawnPoint.position, Quaternion.identity);
        Vector3 spawnParticlePos = new Vector3(0, 0, 0);

        spheres = new List<GameObject>();
        particles = new List<Particle>();
        connectors = new List<Connector>();
        triangles = new List<Triangle>();

        for (int y = 0; y <= rows; y++)
        {
            for (int x = 0; x <= columns; x++)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                var mat = sphere.GetComponent<Renderer>();
                mat.material = nodeMaterial;
                sphere.transform.position = new Vector3(0, spawnParticlePos.y, spawnParticlePos.z);
                sphere.transform.localScale = nodeSize;

                Particle point = new Particle();
                point.pinnedPos = new Vector3(0, spawnParticlePos.y, spawnParticlePos.z);
                point.pos = point.pinnedPos;
                point.oldPos = point.pinnedPos;

                if (x != 0)
                {
                    CreateConnector(sphere, spheres[spheres.Count - 1], point, particles[particles.Count - 1]);
                }

                if (y != 0)
                {
                    CreateConnector(sphere, spheres[(y - 1) * (columns + 1) + x], point, particles[(y - 1) * (columns + 1) + x]);
                }

                // // DO NOT UNCOMMENT - CREATES DIAGONAL CONNECTOR WITH SAME LENGHT AS SIDE - NOT HANDLED AS OF NOW 💀💀💀
                // if (x != 0 && y != 0)
                // {
                //     CreateConnector(sphere, spheres[(y - 1) * (columns + 1) + x - 1], point, particles[(y - 1) * (columns + 1) + x - ]);
                // }

                if (x != 0 && y != 0)
                {
                    CreateTriangle(sphere, spheres[spheres.Count - 1], spheres[(y - 1) * (columns + 1) + x - 1]);
                    CreateTriangle(sphere, spheres[(y - 1) * (columns + 1) + x], spheres[(y - 1) * (columns + 1) + x - 1]);
                }

                if (y == 0)
                {
                    point.pinned = true;
                }

                spawnParticlePos.z -= spacing;

                spheres.Add(sphere);
                particles.Add(point);
            }

            spawnParticlePos.z = 0;
            spawnParticlePos.y -= spacing;
        }

        // Add a collider to detect cuts
        BoxCollider collider = gameObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
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
        MeshCollider meshCollider = triangleObj.AddComponent<MeshCollider>(); // Add MeshCollider

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] { p0.transform.position, p1.transform.position, p2.transform.position };
        mesh.triangles = new int[] { 0, 1, 2 };
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshRenderer.material = triangleMaterial;

        // Assign the mesh to the MeshCollider
        meshCollider.sharedMesh = mesh;

        Triangle triangle = new Triangle
        {
            p0 = p0,
            p1 = p1,
            p2 = p2,
            meshFilter = meshFilter,
            meshRenderer = meshRenderer,
            meshCollider = meshCollider // Store the MeshCollider reference if needed
        };

        triangles.Add(triangle);
    }

    private void OnTriggerEnter(Collider other)
    {
        SwordCutter sword = other.GetComponent<SwordCutter>();
        if (sword != null)
        {
            Vector3 hitPoint = sword.transform.position; // Modify if necessary to get the exact hit point
            Cut(hitPoint);
        }
    }

    public void Cut(Vector3 hitPoint)
    {
        print("Cut");
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

        var startDistance = spacing;
        for (int i = 0; i < connectors.Count; i++)
        {
            float dist = Vector3.Distance(connectors[i].point0.pos, connectors[i].point1.pos);
            float error = Mathf.Abs(dist - startDistance);

            Vector3 changeDir;
            if (dist > startDistance)
            {
                changeDir = (connectors[i].point0.pos - connectors[i].point1.pos).normalized;
            }
            else
            {
                changeDir = (connectors[i].point1.pos - connectors[i].point0.pos).normalized;
            }

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

    // void CheckSwordCollision()
    // {
    //     // Get the sword's position and rotation
    //     Vector3 swordTip = sword.transform.position + sword.transform.forward * (sword.transform.localScale.z / 2);
    //     Vector3 swordBase = sword.transform.position - sword.transform.forward * (sword.transform.localScale.z / 2);

    //     for (int i = connectors.Count - 1; i >= 0; i--)
    //     {
    //         Vector3 closestPoint = ClosestPointOnLineSegment(swordBase, swordTip, connectors[i].point0.pos);
    //         float distToPoint0 = Vector3.Distance(closestPoint, connectors[i].point0.pos);
    //         float distToPoint1 = Vector3.Distance(closestPoint, connectors[i].point1.pos);

    //         // Check if the sword is close enough to either end of the connector
    //         if (distToPoint0 <= cutDistance || distToPoint1 <= cutDistance)
    //         {
    //             RemoveConnector(connectors[i]);
    //             connectors.RemoveAt(i);
    //         }
    //     }
    // }

void CheckSwordCollision()
{
    // Get the sword's position and rotation
    Vector3 swordTip = sword.transform.position + sword.transform.forward * (sword.transform.localScale.z / 2);
    Vector3 swordBase = sword.transform.position - sword.transform.forward * (sword.transform.localScale.z / 2);

    for (int i = connectors.Count - 1; i >= 0; i--)
    {
        Vector3 closestPoint = ClosestPointOnLineSegment(swordBase, swordTip, connectors[i].point0.pos);
        float distToPoint0 = Vector3.Distance(closestPoint, connectors[i].point0.pos);
        float distToPoint1 = Vector3.Distance(closestPoint, connectors[i].point1.pos);

        // Check if the sword is close enough to either end of the connector
        if (distToPoint0 <= cutDistance || distToPoint1 <= cutDistance)
        {
            Vector3 midpoint = (connectors[i].point0.pos + connectors[i].point1.pos) / 2;
            Debug.Log("Connector removed: " + connectors[i].point0.pos + " - " + connectors[i].point1.pos + ". Midpoint: " + midpoint);

            Vector3 dir1 = (connectors[i].point0.pos - midpoint).normalized;
            Vector3 dir2 = (connectors[i].point1.pos - midpoint).normalized;

            GameObject node1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            node1.transform.position = midpoint + dir1 * offset;
            node1.transform.localScale = nodeSize;
            node1.GetComponent<Renderer>().material = nodeMaterial;

            GameObject node2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            node2.transform.position = midpoint + dir2 * offset;
            node2.transform.localScale = nodeSize;
            node2.GetComponent<Renderer>().material = nodeMaterial;

            Particle point1 = new Particle();
            point1.pos = node1.transform.position;
            point1.oldPos = node1.transform.position;
            point1.pinnedPos = node1.transform.position;

            Particle point2 = new Particle();
            point2.pos = node2.transform.position;
            point2.oldPos = node2.transform.position;
            point2.pinnedPos = node2.transform.position;

            // CreateConnector(connectors[i].p0, node1, connectors[i].point0, point1);
            // CreateConnector(connectors[i].p1, node2, connectors[i].point1, point2);

            spheres.Add(node1);
            spheres.Add(node2);
            particles.Add(point1);
            particles.Add(point2);

            RemoveConnector(connectors[i]);
            connectors.RemoveAt(i);
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
        // public float gravity = 0.24f;
        public float gravity = 0f;

        public float friction = 0.99f;
    }
}
