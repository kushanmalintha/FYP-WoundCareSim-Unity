using UnityEngine;

public class CleaningStepController : MonoBehaviour
{
    public static CleaningStepController Instance { get; private set; }

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
        if (StepFlowController.Instance != null &&
            StepFlowController.Instance.CurrentStep == Step.CleaningAndDressing)
        {
            if (OVRInput.GetDown(OVRInput.Button.Two)) // B button
            {
                StartCoroutine(
                    BackendConnectionManager.Instance.CompleteStep("cleaning_and_dressing")
                );
            }
        }
    }
}
