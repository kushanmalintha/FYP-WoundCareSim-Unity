using UnityEngine;
using Oculus.Interaction;
using Newtonsoft.Json.Linq;

public class SelectDressingAction : MonoBehaviour
{
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
        if (evt.Type != PointerEventType.Select) return;

        if (actionSent) return;

        SendAction();
        actionSent = true;
    }

    void SendAction()
    {
        JObject data = new JObject();
        data["action_type"] = "action_select_dressing";

        BackendConnectionManager.Instance.SendEvent(
            "action_performed",
            data
        );

        Debug.Log("Sent action: action_select_dressing");
    }
}
