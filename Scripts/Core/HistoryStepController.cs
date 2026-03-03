using UnityEngine;
using UnityEngine.InputSystem;
using Newtonsoft.Json.Linq;

public class HistoryStepController : MonoBehaviour
{
    public static HistoryStepController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (StepFlowController.Instance != null && StepFlowController.Instance.CurrentStep == Step.History)
        {
            if (OVRInput.GetDown(OVRInput.Button.One)) // A button
            {
                BackendConnectionManager.Instance.SendEvent(
                    "text_message",
                    JObject.FromObject(new { text = "Hello, I am the student nurse." })
                );
            }

            if (OVRInput.GetDown(OVRInput.Button.Three)) // X button
            {
                BackendConnectionManager.Instance.SendEvent(
                    "nurse_message",
                    JObject.FromObject(new { text = "Nurse, can you guide me on what to ask next?" })
                );
            }
        }
    }

    public void HandleNurseMessage(string text, string role)
    {
        if (StepFlowController.Instance == null || StepFlowController.Instance.CurrentStep != Step.History)
        {
            return;
        }

        if (role == "patient")
        {
            Debug.Log("PATIENT: " + text);
        }
        else if (role == "nurse" || role == "staff_nurse")
        {
            Debug.Log("NURSE: " + text);
        }
    }
}
