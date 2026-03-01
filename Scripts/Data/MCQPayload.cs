using System;
using UnityEngine;

/// <summary>
/// Data structure for MCQ (Multiple Choice Question) payload
/// </summary>
[Serializable]
public class MCQPayload
{
    public int questionId;
    public string answer;
    public string question;

    public MCQPayload(int questionId, string answer, string question)
    {
        this.questionId = questionId;
        this.answer = answer;
        this.question = question;
    }

    /// <summary>
    /// Convert to JSON string
    /// </summary>
    /// <returns>JSON representation</returns>
    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    /// <summary>
    /// Create a new MCQPayload from JSON string
    /// </summary>
    /// <param name="json">JSON string</param>
    /// <returns>MCQPayload object</returns>
    public static MCQPayload FromJson(string json)
    {
        return JsonUtility.FromJson<MCQPayload>(json);
    }
}