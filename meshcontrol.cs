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
            Debug.LogError("No MeshFilter found on this GameObject.");
            return;
        }

        if (medicine2 == null)
        {
            Debug.LogError("Medicine2 mesh not assigned.");
            return;
        }

        // Assign medicine2 mesh as a new instance to modify
        meshInstance = Instantiate(medicine2);
        meshFilter.mesh = meshInstance;

        // Example modification: move vertices up by 0.1f
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
