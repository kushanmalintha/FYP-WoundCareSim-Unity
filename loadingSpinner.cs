using UnityEngine;

public class LoadingSpinner : MonoBehaviour
{
    public float rotationSpeed = 200f;

    void Update()
    {
        Debug.Log("Spinning");
        transform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
    }
}