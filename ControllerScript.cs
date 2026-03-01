using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerScript : MonoBehaviour
{
    public Camera sceneCamera;

    public GameObject patientAgent;
    public GameObject nurseAgent;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float step;

    // to control Audio
    [SerializeField] private AudioUIController audioUIController;

    void Start()
    {
        // Place cube in front of camera
        transform.position = sceneCamera.transform.position + sceneCamera.transform.forward * 3.0f;
    }

    void Update()
    {
        step = 5.0f * Time.deltaTime;

        // Align cube if trigger held
        if (OVRInput.Get(OVRInput.RawButton.RIndexTrigger)) centerCube();

        if (OVRInput.Get(OVRInput.RawButton.RThumbstickLeft)) transform.Rotate(0, 5.0f * step, 0);
        if (OVRInput.Get(OVRInput.RawButton.RThumbstickRight)) transform.Rotate(0, -5.0f * step, 0);

        // ✅ Agent Switching Section

        // if (OVRInput.GetDown(OVRInput.Button.One)) // A Button
        // {
        //     ActivateAgent(patientAgent);
        //     Debug.Log("A Button Pressed – Patient Agent Activated");
        // }

        // if (OVRInput.GetUp(OVRInput.Button.One)) // A Button Released
        // {
        //     StartCoroutine(TriggerVibration(0.5f, 0.5f, 0.2f, OVRInput.Controller.RTouch)); // Right controller vibration
        //     Debug.Log("A Button Released – Voice script sent.");
        // }

        if (OVRInput.GetDown(OVRInput.Button.Two)) // B Button - Nurse Agent
        {
            ActivateAgent(nurseAgent);
            Debug.Log("B Button Pressed – Nurse Agent Activated");
            audioUIController.OnRecordButtonDown(); // Start recording when B button is pressed
        }

        if (OVRInput.GetUp(OVRInput.Button.Two)) // B Button Released
        {
            StartCoroutine(TriggerVibration(0.5f, 0.5f, 0.2f, OVRInput.Controller.RTouch)); // Right controller vibration
            Debug.Log("B Button Released – Voice script sent.");
            audioUIController.OnRecordButtonUp("PATIENT");
        }
        if (OVRInput.GetDown(OVRInput.Button.Four)) // X Button (right controller)
        {
            // StartCoroutine(TriggerVibration(0.5f, 0.5f, 0.2f, OVRInput.Controller.LTouch)); // Right controller vibration
            Debug.Log("Y Button Pressed – Patient Agent Activated");
            audioUIController.OnRecordButtonDown();
        }

        if (OVRInput.GetUp(OVRInput.Button.Four)) // Y Button (left controller)
        {
            StartCoroutine(TriggerVibration(0.5f, 0.5f, 0.2f, OVRInput.Controller.LTouch)); // Left controller vibration
            Debug.Log("Y Button released – Voice script sent.");
            audioUIController.OnRecordButtonUp("NURSE");
        }
    }

    IEnumerator TriggerVibration(float amplitude, float frequency, float duration, OVRInput.Controller controller)
    {
        OVRInput.SetControllerVibration(amplitude, frequency, controller);
        yield return new WaitForSeconds(duration);
        OVRInput.SetControllerVibration(0, 0, controller); // Stop vibration
    }
    void centerCube()
    {
        targetPosition = sceneCamera.transform.position + sceneCamera.transform.forward * 3.0f;
        targetRotation = Quaternion.LookRotation(transform.position - sceneCamera.transform.position);

        transform.position = Vector3.Lerp(transform.position, targetPosition, step);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, step);
    }

    void ActivateAgent(GameObject agentToActivate)
    {
        if (patientAgent != null)
            patientAgent.SetActive(agentToActivate == patientAgent);

        if (nurseAgent != null)
            nurseAgent.SetActive(agentToActivate == nurseAgent);
    }
    // IEnumerator TriggerVibration(float amplitude, float frequency, float duration)
    // {
    //     OVRInput.SetControllerVibration(amplitude, frequency, OVRInput.Controller.RTouch);
    //     yield return new WaitForSeconds(duration);
    //     OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch); // Stop vibration
    // }
}
