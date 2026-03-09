using UnityEngine;
using Oculus.Interaction;
using Newtonsoft.Json.Linq;

public class ArrangeMaterialsOnRelease : MonoBehaviour
{
    private Grabbable grabbable;
    private bool actionSent = false;

    void Start()
    {
        grabbable = GetComponent<Grabbable>();
        grabbable.WhenPointerEventRaised += HandlePointerEvent;
    }

    private void HandlePointerEvent(PointerEvent evt)
    {
        if (evt.Type != PointerEventType.Unselect) return;

        if (actionSent) return;

        SendAction();
        actionSent = true;
    }

    void SendAction()
    {
        JObject data = new JObject();
        data["action_type"] = "action_arrange_materials";

        BackendConnectionManager.Instance.SendEvent(
            "action_performed",
            data
        );

        Debug.Log("Sent action: action_arrange_materials");
    }
}
