using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestionManager : MonoBehaviour
{
    public static QuestionManager Instance { get; private set; }

    public TMP_Text questionText;
    public ToggleGroup toggleGroup;
    public List<Toggle> answerToggles;
    public Button submitButton;

    private List<JObject> questions;
    private int currentQuestionIndex;
    private bool isInitialized;
    private bool isWaitingForBackend;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (StepFlowController.Instance == null) return;

        if (StepFlowController.Instance.CurrentStep == Step.Assessment)
        {
            if (!isInitialized)
            {
                InitializeAssessment();
            }
        }
        else
        {
            if (isInitialized)
            {
                isInitialized = false;
                currentQuestionIndex = 0;
                isWaitingForBackend = false;
                
                // Disable UI elements
                if (questionText != null) questionText.text = "";
                foreach (var toggle in answerToggles)
                {
                    if (toggle != null) toggle.gameObject.SetActive(false);
                }
                if (submitButton != null) submitButton.gameObject.SetActive(false);
            }
        }
    }

    private void InitializeAssessment()
    {
        if (BackendConnectionManager.Instance == null || BackendConnectionManager.Instance.SessionMetadata == null)
        {
            Debug.LogError("SessionMetadata not found.");
            return;
        }

        JToken assessmentQuestionsToken = BackendConnectionManager.Instance.SessionMetadata["assessment_questions"];
        if (assessmentQuestionsToken == null)
        {
            Debug.LogError("assessment_questions not found in SessionMetadata.");
            return;
        }

        questions = new List<JObject>();
        foreach (JToken token in assessmentQuestionsToken)
        {
            questions.Add(token as JObject);
        }

        currentQuestionIndex = 0;
        isInitialized = true;
        isWaitingForBackend = false;

        DisplayQuestion(0);
    }

    private void DisplayQuestion(int index)
    {
        if (questions == null) return;

        if (index >= questions.Count)
        {
            if (submitButton != null) submitButton.gameObject.SetActive(false);
            Debug.Log("All questions answered");
            return;
        }

        if (submitButton != null) submitButton.gameObject.SetActive(true);

        JObject q = questions[index];
        if (questionText != null)
        {
            questionText.text = q["question"]?.ToString();
        }

        JArray options = q["options"] as JArray;
        if (options != null)
        {
            for (int i = 0; i < answerToggles.Count; i++)
            {
                if (i < options.Count)
                {
                    answerToggles[i].gameObject.SetActive(true);
                    answerToggles[i].isOn = false;

                    Text textComponent = answerToggles[i].GetComponentInChildren<Text>();
                    if (textComponent != null)
                    {
                        textComponent.text = options[i].ToString();
                    }
                    else
                    {
                        TMP_Text tmpTextComponent = answerToggles[i].GetComponentInChildren<TMP_Text>();
                        if (tmpTextComponent != null)
                        {
                            tmpTextComponent.text = options[i].ToString();
                        }
                    }
                }
                else
                {
                    answerToggles[i].gameObject.SetActive(false);
                }
            }
        }

        // Reset toggle selection
        if (toggleGroup != null)
        {
            toggleGroup.SetAllTogglesOff();
        }

        if (submitButton != null) submitButton.interactable = true;
    }

    public void SubmitAnswer()
    {
        if (isWaitingForBackend) return;

        int selectedIndex = -1;
        for (int i = 0; i < answerToggles.Count; i++)
        {
            if (answerToggles[i].isOn && answerToggles[i].gameObject.activeSelf)
            {
                selectedIndex = i;
                break;
            }
        }

        if (selectedIndex == -1) return;

        string selectedOptionText = "";
        
        Text textComponent = answerToggles[selectedIndex].GetComponentInChildren<Text>();
        if (textComponent != null)
        {
            selectedOptionText = textComponent.text;
        }
        else
        {
            TMP_Text tmpTextComponent = answerToggles[selectedIndex].GetComponentInChildren<TMP_Text>();
            if (tmpTextComponent != null)
            {
                selectedOptionText = tmpTextComponent.text;
            }
        }

        if (BackendConnectionManager.Instance != null && questions != null && currentQuestionIndex < questions.Count)
        {
            _ = BackendConnectionManager.Instance.SendEvent(
                "mcq_answer",
                JObject.FromObject(new
                {
                    question_id = questions[currentQuestionIndex]["id"]?.ToString(),
                    answer = selectedOptionText
                })
            );

            isWaitingForBackend = true;
            if (submitButton != null) submitButton.interactable = false;
        }
    }

    public void HandleAnswerResult(JObject data)
    {
        isWaitingForBackend = false;

        bool isCorrect = data["is_correct"]?.Value<bool>() ?? false;
        string explanation = data["explanation"]?.ToString();

        Debug.Log($"Explanation: {explanation}");

        currentQuestionIndex++;

        if (currentQuestionIndex < questions.Count)
        {
            DisplayQuestion(currentQuestionIndex);
        }
        else
        {
            Debug.Log("All questions answered. Completing assessment.");
            if (BackendConnectionManager.Instance != null)
            {
                _ = BackendConnectionManager.Instance.SendEvent(
                    "step_complete",
                    JObject.FromObject(new { step = "assessment" })
                );
            }
        }
    }

    public void HandleAssessmentSummary(JObject data)
    {
        if (data == null) return;

        JObject mcqResult = data["mcq_result"] as JObject;

        if (mcqResult != null)
        {
            string totalQuestions = mcqResult["total_questions"]?.ToString();
            string correctCount = mcqResult["correct_count"]?.ToString();
            string score = mcqResult["score"]?.ToString();

            Debug.Log($"Assessment Summary - Total: {totalQuestions}, Correct: {correctCount}, Score: {score}");
        }

        string summaryText = data["summary_text"]?.ToString();
        Debug.Log($"Summary Text: {summaryText}");

        isInitialized = false;
    }
}
