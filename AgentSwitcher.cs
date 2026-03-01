// using UnityEngine;
// using UnityEngine.InputSystem;

// public class AgentSwitcher : MonoBehaviour
// {
//     public GameObject patientAgent;
//     public GameObject nurseAgent;

//     void Update()
//     {
//         var gamepad = Gamepad.current;
//         if (gamepad == null) return;

//         // Press A (activate Patient)
//         if (gamepad.buttonSouth.wasPressedThisFrame)
//         {
//             ActivateAgent(patientAgent);
//         }

//         // Press B (activate Nurse)
//         if (gamepad.buttonEast.wasPressedThisFrame)
//         {
//             ActivateAgent(nurseAgent);
//         }
//     }

//     void ActivateAgent(GameObject agentToActivate)
//     {
//         patientAgent.SetActive(agentToActivate == patientAgent);
//         nurseAgent.SetActive(agentToActivate == nurseAgent);

//         if (agentToActivate == patientAgent)
//             Debug.Log("Patient Agent Activated");
//         else if (agentToActivate == nurseAgent)
//             Debug.Log("Nurse Agent Activated");
//     }
// }

using UnityEngine;
using UnityEngine.InputSystem;

public class AgentSwitcher : MonoBehaviour
{
    public GameObject patientAgent;
    public GameObject nurseAgent;

    void Update()
    {
        var gamepad = Gamepad.current;

        if (gamepad == null)
        {
            Debug.Log("No gamepad detected.");
            return;
        }

        if (gamepad.buttonSouth.wasPressedThisFrame) // A button
        {
            Debug.Log("A button pressed");
            ActivateAgent(patientAgent);
        }

        if (gamepad.buttonEast.wasPressedThisFrame) // B button
        {
            Debug.Log("B button pressed");
            ActivateAgent(nurseAgent);
        }
    }

    void ActivateAgent(GameObject agentToActivate)
    {
        if (patientAgent == null || nurseAgent == null)
        {
            Debug.LogWarning("One or both agent references are missing!");
            return;
        }

        patientAgent.SetActive(agentToActivate == patientAgent);
        nurseAgent.SetActive(agentToActivate == nurseAgent);

        Debug.Log(agentToActivate.name + " Activated");
    }
}