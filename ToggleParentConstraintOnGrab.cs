

using UnityEngine;
using Oculus.Interaction;
using UnityEngine.Animations;

public class ToggleParentConstraintOnGrab : MonoBehaviour
{
    [SerializeField] private ParentConstraint parentConstraint;
    [SerializeField] private InteractableUnityEventWrapper interactableEvents;
    [SerializeField] private Collider trolleyBoundary; // ✅ Reference to trolley's boundary (set in Inspector)

    private bool isGrabbed = false;
    private bool isInsideBoundary = false;

    private void Start()
    {
        if (parentConstraint == null)
            parentConstraint = GetComponent<ParentConstraint>();

        interactableEvents.WhenSelect.AddListener(OnGrab);
        interactableEvents.WhenUnselect.AddListener(OnRelease);
    }

    private void OnGrab()
    {
        isGrabbed = true;
        DisableConstraint();
        if (parentConstraint != null)
        {
            // Freeze all translation and rotation axes
            parentConstraint.translationAxis = Axis.X | Axis.Y | Axis.Z;
            parentConstraint.rotationAxis = Axis.X | Axis.Y | Axis.Z;

            Debug.Log($"[{name}] OnGrab: All axes frozen (translation & rotation).");
        }
    }

    private void OnRelease()
    {
        isGrabbed = false;

        // Only re-enable constraint if inside the boundary
        if (isInsideBoundary)
            EnableConstraint();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == trolleyBoundary)
        {
            isInsideBoundary = true;

            // Only follow parent if not grabbed
            if (!isGrabbed)
                EnableConstraint();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == trolleyBoundary)
        {
            isInsideBoundary = false;

            DisableConstraint(); // stop following when outside
        }
    }

    private void EnableConstraint()
    {
        if (parentConstraint != null)
            parentConstraint.enabled = true;
    }

    private void DisableConstraint()
    {
        if (parentConstraint != null)
            parentConstraint.enabled = false;
    }
}
