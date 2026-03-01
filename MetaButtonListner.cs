using UnityEngine;
using UnityEngine.InputSystem;

public class MetaButtonListener : MonoBehaviour
{
    void Update()
    {
        if (Gamepad.current == null) return;

        if (Gamepad.current.buttonSouth.wasPressedThisFrame) // A
            Debug.Log("A button pressed");

        if (Gamepad.current.buttonEast.wasPressedThisFrame) // B
            Debug.Log("B button pressed");

        if (Gamepad.current.buttonNorth.wasPressedThisFrame) // Y
            Debug.Log("Y button pressed");
    }
}