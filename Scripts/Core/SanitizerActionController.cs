using UnityEngine;
using Oculus.Interaction;
using Newtonsoft.Json.Linq;

public class SanitizerActionController : MonoBehaviour
{
    private Grabbable grabbable;

    private int useCount = 0;

    void Start()
    {
        grabbable = GetComponent<Grabbable>();
        grabbable.WhenPointerEventRaised += HandlePointerEvent;
    }

    private void HandlePointerEvent(PointerEvent evt)
    {
        if (evt.Type != PointerEventType.Select) return;

        string actionType = null;

        if (useCount == 0)
        {
            actionType = "action_initial_hand_hygiene";
        }
        else if (useCount == 1)
        {
            actionType = "action_hand_hygiene_after_cleaning";
        }
        else
        {
            Debug.Log("Sanitizer already used twice.");
            return;
        }

        useCount++;

        SendAction(actionType);
    }

    void SendAction(string action)
    {
        JObject data = new JObject();
        data["action_type"] = action;

        BackendConnectionManager.Instance.SendEvent(
            "action_performed",
            data
        );

        Debug.Log("Sent action: " + action);
    }
}
