using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SwordCutter : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;

    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = gameObject.AddComponent<XRGrabInteractable>();
        }

        // Configure the grab interactable
        grabInteractable.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        grabInteractable.throwOnDetach = true;
    }

    void Update()
    {
        // You can add additional sword-specific behavior here if needed
    }
}