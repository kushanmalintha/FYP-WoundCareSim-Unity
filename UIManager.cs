using UnityEngine;
using TMPro;

[System.Serializable]
public class EvaluationResponse
{
    public int marks;
    public string name;
}

/// <summary>
/// UIManager — networking reset.
/// Backend evaluation REST calls removed. ShowEvaluationUI now logs a placeholder.
/// </summary>
public class UIManager : MonoBehaviour
{
    public GameObject evaluationPanel;
    public GameObject loadingSpinner;
    public GameObject resultsPanel;
    [SerializeField] private TMP_Text textMarks;

    public void ShowEvaluationUI()
    {
        // Backend calls removed - placeholder
        Debug.Log("Backend call removed - placeholder (UIManager.ShowEvaluationUI)");
        evaluationPanel.SetActive(true);
        loadingSpinner.SetActive(false);
    }

    public void HideEvaluationUI()
    {
        evaluationPanel.SetActive(false);
    }
}
