using UnityEngine;

public enum Step
{
    None,
    History,
    Assessment,
    CleaningAndDressing,
    Completed
}

public class StepFlowController : MonoBehaviour
{
    public static StepFlowController Instance { get; private set; }

    public Step CurrentStep { get; private set; } = Step.None;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetInitialStep()
    {
        UpdateStep(Step.History);
    }

    public void AdvanceTo(string backendStepName)
    {
        Step nextStep = CurrentStep;

        switch (backendStepName.ToLower())
        {
            case "assessment":
                nextStep = Step.Assessment;
                break;
            case "cleaning_and_dressing":
                nextStep = Step.CleaningAndDressing;
                break;
            case "completed":
                nextStep = Step.Completed;
                break;
            default:
                Debug.LogWarning($"[StepFlowController] Received unknown step name from backend: {backendStepName}");
                break;
        }

        if (nextStep != CurrentStep)
        {
            UpdateStep(nextStep);
        }
    }

    private void UpdateStep(Step newStep)
    {
        CurrentStep = newStep;
        Debug.Log($"[StepFlowController] Step changed to: {CurrentStep}");
    }
}
