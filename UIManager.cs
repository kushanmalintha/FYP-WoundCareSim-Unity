using UnityEngine;
using TMPro;

[System.Serializable]

public class EvaluationResponse
{
    public int marks;
    public string name;
}

public class UIManager : MonoBehaviour
{
    public GameObject evaluationPanel;
    public GameObject loadingSpinner;
    public GameObject resultsPanel;
    [SerializeField] private TMP_Text textMarks;
    [SerializeField] private RequestHandler requestHandler;

    public void ShowEvaluationUI()
    {
        // send evaluation request 
        string json = "{\"matrix\":[true,true,true,false,false,false]}";
        requestHandler.SendApiRequest("/evaluate/patient-conversation", json);
        requestHandler.SendApiRequestAndGetJson("/evaluate/mcq", OnSuccessCallback, OnErrorCallback);
        evaluationPanel.SetActive(true);
        loadingSpinner.SetActive(true);

    }

    public void HideEvaluationUI()
    {
        evaluationPanel.SetActive(false);
    }

    private void OnSuccessCallback(string jsonResponse)
    {
        EvaluationResponse response = JsonUtility.FromJson<EvaluationResponse>(jsonResponse);
        Debug.Log($"Marks: {response.marks}");
        textMarks.text = $"Marks: {response.marks}";

    }

    private void OnErrorCallback(string error)
    {
        Debug.LogError($"❌ Error Callback: {error}");
    }
}
