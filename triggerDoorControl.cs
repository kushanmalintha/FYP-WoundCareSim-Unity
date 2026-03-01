using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class triggerDoorControl : MonoBehaviour
{
    [SerializeField] private Animator mydoor = null;
    [SerializeField] private bool openTrigger = false;
    [SerializeField] private bool closeTrigger = false;
    [SerializeField] private string targetTag = "Player"; // Tag to check for triggering
    
    private bool doorOpen = false;
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger has the correct tag
        if (other.CompareTag(targetTag))
        {
            if (openTrigger && !doorOpen)
            {
                OpenDoor();
            }
            else if (closeTrigger && doorOpen)
            {
                CloseDoor();
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        // Check if the object exiting the trigger has the correct tag
        if (other.CompareTag(targetTag))
        {
            if (openTrigger && doorOpen)
            {
                CloseDoor();
            }
        }
    }
    
    private void OpenDoor()
    {
        if (mydoor != null)
        {
            mydoor.SetBool("Open", true);
            doorOpen = true;
            Debug.Log("Door opened");
        }
    }
    
    private void CloseDoor()
    {
        if (mydoor != null)
        {
            mydoor.SetBool("Open", false);
            doorOpen = false;
            Debug.Log("Door closed");
        }
    }
}
