using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

public class QuestionManager : MonoBehaviour
{
    public TMP_Text questionText;
    public ToggleGroup toggleGroup;
    public List<Toggle> answerToggles;
    public AudioSource audioSource;
    [SerializeField] private WebSocketHandler webSocketHandler;

    private List<MCQQuestion> questions;

    public Button submitButton;
    public bool isActivateStepsLogic = false; // to activate the steps logic

    private int currentQuestionIndex = 0;

    void Start()
    {
        StartCoroutine(LoadQuestions());
        foreach (var toggle in answerToggles)
        {
            toggle.onValueChanged.AddListener((_) => PlayClickSound());
        }
    }

    IEnumerator LoadQuestions()
    {
        string fileName = "questions.json";
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        string json = "";

#if UNITY_ANDROID && !UNITY_EDITOR
        UnityWebRequest www = UnityWebRequest.Get(path);
        yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        if (www.result != UnityWebRequest.Result.Success)
#else
        if (www.isNetworkError || www.isHttpError)
#endif
        {
            Debug.LogError("Failed to load JSON on Android: " + www.error);
            yield break;
        }

        json = www.downloadHandler.text;
#else
        if (File.Exists(path))
        {
            json = File.ReadAllText(path);
        }
        else
        {
            Debug.LogError("JSON file not found at path: " + path);
            yield break;
        }
#endif

        // Parse and store questions
        questions = new List<MCQQuestion>(JsonHelper.FromJson<MCQQuestion>(json));
        DisplayQuestion(currentQuestionIndex);
    }

    void DisplayQuestion(int index)
    {
        if (index < 0 || index >= questions.Count)
        {
            Debug.Log("All questions done!");
            return;
        }

        MCQQuestion q = questions[index];
        questionText.text = q.question;

        for (int i = 0; i < answerToggles.Count; i++)
        {
            Toggle toggle = answerToggles[i];
            toggle.isOn = false;
            toggle.gameObject.SetActive(i < q.options.Length);
            toggle.GetComponentInChildren<Text>().text = q.options[i];
        }
    }

    public void SubmitAnswer()
    {
        int selectedIndex = -1;

        for (int i = 0; i < answerToggles.Count; i++)
        {
            if (answerToggles[i].isOn)
            {
                selectedIndex = i;
                break;
            }
        }

        if (selectedIndex == -1)
        {
            Debug.Log("No answer selected");
            return;
        }

        if (selectedIndex == questions[currentQuestionIndex].correctOptionIndex)
        {
            Debug.Log("✅ Correct Answer!");
        }
        else
        {
            Debug.Log("❌ Wrong Answer!");
        }

        webSocketHandler.SendSampleMCQData(currentQuestionIndex + 1, answerToggles[selectedIndex].GetComponentInChildren<Text>().text);
        submitButton.interactable = false;
        submitButton.GetComponentInChildren<TextMeshProUGUI>().text = "Please wait for the response...";

        currentQuestionIndex++;
        if (currentQuestionIndex >= questions.Count)
        {
            Debug.Log("Quiz Finished!");
            isActivateStepsLogic = true;
            return;
        }

        DisplayQuestion(currentQuestionIndex);
    }

    public void PlayClickSound()
    {
        if (audioSource != null)
            audioSource.Play();
    }
}
