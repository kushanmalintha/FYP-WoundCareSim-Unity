using UnityEngine;
using System.Collections;
using UnityEngine.Animations;

public class AutoDockToPoint : MonoBehaviour
{
    [SerializeField] private Transform defaultDockingPoint; // Assign in Inspector
    [SerializeField] private ParentConstraint parentConstraint;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        // LogParentConstraintFreezeStatus();
        StartCoroutine(DockAfterDelay());
    }

    private IEnumerator DockAfterDelay()
    {
        yield return new WaitForSeconds(0.05f); // Optional: wait a frame for physics stability
        DockToDefaultPoint();
    }

    private void DockToDefaultPoint()
    {
        if (defaultDockingPoint == null)
        {
            Debug.LogWarning($"{name}: No docking point assigned.");
            return;
        }

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true; // Temporarily disable physics
            rb.MovePosition(defaultDockingPoint.position);
            rb.MoveRotation(defaultDockingPoint.rotation);
            rb.isKinematic = false; // Reactivate physics if needed
            transform.position = defaultDockingPoint.position;
            transform.rotation = defaultDockingPoint.rotation;
        }
        else
        {
            transform.position = defaultDockingPoint.position;
            transform.rotation = defaultDockingPoint.rotation;
        }

        Debug.Log($"{name} docked to {defaultDockingPoint.name}");
    }
    private void LogParentConstraintFreezeStatus()
    {
        if (parentConstraint == null)
        {
            Debug.LogWarning($"[{name}] ParentConstraint is null.");
            return;
        }

        var tAxis = parentConstraint.translationAxis;
        var rAxis = parentConstraint.rotationAxis;

        Debug.Log($"[{name}] Translation Freeze Status: X={tAxis.HasFlag(Axis.X)}, Y={tAxis.HasFlag(Axis.Y)}, Z={tAxis.HasFlag(Axis.Z)}");
        Debug.Log($"[{name}] Rotation Freeze Status: X={rAxis.HasFlag(Axis.X)}, Y={rAxis.HasFlag(Axis.Y)}, Z={rAxis.HasFlag(Axis.Z)}");
    }
}
