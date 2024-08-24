using UnityEngine;

public class SwordCutter : MonoBehaviour
{
    public Transform rayOrigin;
    public float rayLength = 1.0f;
    public float cutFrequency = 0.1f; // Cut every 0.1 seconds
    private float lastCutTime = 0f;

    private void Update()
    {
        if (Time.time - lastCutTime >= cutFrequency)
        {
            PerformCut();
            lastCutTime = Time.time;
        }
    }

    private void PerformCut()
    {
        // Use OverlapBox to detect the Verlet3D objects within the trigger area
        Collider[] colliders = Physics.OverlapBox(rayOrigin.position, rayOrigin.localScale / 2, rayOrigin.rotation);

        foreach (Collider collider in colliders)
        {
            Verlet3D verlet = collider.GetComponentInParent<Verlet3D>();
            if (verlet != null)
            {
                Vector3 hitPoint = collider.ClosestPoint(rayOrigin.position);
                verlet.Cut(hitPoint);
            }
        }

        Debug.DrawRay(rayOrigin.position, rayOrigin.up * rayLength, Color.red, cutFrequency);
    }
}
