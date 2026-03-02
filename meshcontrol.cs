using UnityEngine;

public class meshcontrol : MonoBehaviour
{
    public Mesh medicine2;  // Assign this in the Inspector with your medicine2 mesh asset

    private MeshFilter meshFilter;
    private Mesh meshInstance;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();

        if (meshFilter == null)
        {
            Debug.LogWarning("meshcontrol: No MeshFilter found on this GameObject. Skipping mesh modification.");
            return;
        }

        if (medicine2 == null)
        {
            Debug.LogWarning("meshcontrol: Medicine2 mesh not assigned in Inspector. Skipping mesh modification.");
            return;
        }

        if (!medicine2.isReadable)
        {
            Debug.LogWarning("meshcontrol: Mesh is not readable (Read/Write not enabled in import settings). Skipping mesh modification.");
            return;
        }

        // Assign medicine2 mesh as a new instance to modify
        meshInstance = Instantiate(medicine2);

        if (meshInstance == null || !meshInstance.isReadable)
        {
            Debug.LogWarning("meshcontrol: Instanced mesh is not readable. Skipping vertex modification.");
            return;
        }

        meshFilter.mesh = meshInstance;

        // Example modification: nudge vertices up by 0.01f
        Vector3[] vertices = meshInstance.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] += Vector3.up * 0.01f;
        }
        meshInstance.vertices = vertices;
        meshInstance.RecalculateBounds();
        meshInstance.RecalculateNormals();
    }
}
