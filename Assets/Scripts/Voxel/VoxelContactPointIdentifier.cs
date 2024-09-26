using System.Collections.Generic;
using UnityEngine;

public class VoxelContactPointIdentifier : MonoBehaviour
{
    public int resolutionX = 52; // Voxel grid resolution in X
    public int resolutionY = 52; // Voxel grid resolution in Y
    public int resolutionZ = 52; // Voxel grid resolution in Z

    private Dictionary<int, List<Vector3>> contactPoints = new Dictionary<int, List<Vector3>>();    

    private Vector3 boundsMin;
    private Vector3 boundsMax;

    void Start()
    {
        // Get bounds from the collider
        Collider collider = GetComponent<Collider>();
        boundsMin = collider.bounds.min;
        boundsMax = collider.bounds.max;
    }

    private int GetHash(Vector3 localPoint)
    {
        int xIndex = Mathf.FloorToInt(localPoint.x);
        int yIndex = Mathf.FloorToInt(localPoint.y);
        int zIndex = Mathf.FloorToInt(localPoint.z);
        return xIndex + yIndex * resolutionX + zIndex * resolutionX * resolutionY; // Hashing function
    }

    public void RegisterContactPoint(Vector3 worldPoint)
    {
        Vector3 localPoint = new Vector3(
            Mathf.Floor(resolutionX * ((worldPoint.x - boundsMin.x) / (boundsMax.x - boundsMin.x))),
            Mathf.Floor(resolutionY * ((worldPoint.y - boundsMin.y) / (boundsMax.y - boundsMin.y))),
            Mathf.Floor(resolutionZ * ((worldPoint.z - boundsMin.z) / (boundsMax.z - boundsMin.z)))
        );

        int hash = GetHash(localPoint);

        if (!contactPoints.ContainsKey(hash))
        {
            contactPoints[hash] = new List<Vector3>();
        }

        if (!contactPoints[hash].Contains(worldPoint)) // Prevent duplicate points
        {
            contactPoints[hash].Add(worldPoint);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            RegisterContactPoint(contact.point);
        }
    }

    // Optional: Visualize contact points for debugging
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        foreach (var kvp in contactPoints)
        {
            foreach (var point in kvp.Value)
            {
                Gizmos.DrawSphere(point, 0.1f);
            }
        }
    }
}
