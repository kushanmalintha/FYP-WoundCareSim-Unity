using UnityEngine;
using Oculus.Interaction;
using Newtonsoft.Json.Linq;

public class GrabActionSender : MonoBehaviour
{
    public string actionType = "action_initial_hand_hygiene";

    private Grabbable grabbable;
    private bool actionSent = false;

    void Start()
    {
        grabbable = GetComponent<Grabbable>();

        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised += HandlePointerEvent;
        }
    }

    private void HandlePointerEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Select)
        {
            if (actionSent) return;

            Debug.Log("Grab detected: " + actionType);

            SendAction();

            actionSent = true;
        }
    }

    void SendAction()
    {
        JObject data = new JObject();
        data["action_type"] = actionType;

        BackendConnectionManager.Instance.SendEvent(
            "action_performed",
            data
        );
    }
}
