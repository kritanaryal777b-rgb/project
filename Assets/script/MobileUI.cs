using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

#if UNITY_ANDROID || UNITY_IOS
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif

public class MobileUI : MonoBehaviour
{
#if UNITY_ANDROID || UNITY_IOS
    private PlayerPickup   pickup;
    private PlayerMovement movement;

    private Vector2       joyInput  = Vector2.zero;
    private Vector2       joyCenter = Vector2.zero;
    private bool          joyActive = false;
    private RectTransform joyKnob;
    private RectTransform joyOuter;
    private float         joyRadius = 90f;

    void Start()
    {
        EnhancedTouchSupport.Enable();

        pickup   = FindObjectOfType<PlayerPickup>();
        movement = FindObjectOfType<PlayerMovement>();

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        MakeJoystick();
        MakePickupBtn();
        MakeThrowBtn();
        MakeJumpBtn();
    }

    void OnDestroy()
    {
        EnhancedTouchSupport.Disable();
    }

    void MakeJoystick()
    {
        GameObject outer = MakeImage("JoystickOuter", transform,
            new Color(1f, 1f, 1f, 0.15f),
            new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(120, 80), new Vector2(220, 220));
        joyOuter = outer.GetComponent<RectTransform>();

        GameObject knob = MakeImage("JoystickKnob", outer.transform,
            new Color(1f, 1f, 1f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(90, 90));
        joyKnob = knob.GetComponent<RectTransform>();

        // transparent overlay covering left half for touch input
        GameObject leftZone = MakeImage("LeftTouchZone", transform,
            new Color(0, 0, 0, 0),
            new Vector2(0, 0), new Vector2(0.5f, 1f),
            Vector2.zero, Vector2.zero);
        RectTransform lzRT = leftZone.GetComponent<RectTransform>();
        lzRT.anchorMin = new Vector2(0, 0);
        lzRT.anchorMax = new Vector2(0.5f, 1f);
        lzRT.offsetMin = Vector2.zero;
        lzRT.offsetMax = Vector2.zero;

        EventTrigger et = leftZone.AddComponent<EventTrigger>();
        AddTrigger(et, EventTriggerType.PointerDown, JoyDown);
        AddTrigger(et, EventTriggerType.Drag,        JoyDrag);
        AddTrigger(et, EventTriggerType.PointerUp,   JoyUp);
    }

    void JoyDown(BaseEventData data)
    {
        PointerEventData ped = (PointerEventData)data;
        joyActive = true;
        joyCenter = ped.position;

        // move joystick visual to finger pos
        if (joyOuter != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform as RectTransform, ped.position,
                null, out Vector2 localPos);
            joyOuter.anchoredPosition = localPos;
        }

        if (joyKnob != null)
            joyKnob.anchoredPosition = Vector2.zero;
    }

    void JoyDrag(BaseEventData data)
    {
        if (!joyActive) return;
        PointerEventData ped = (PointerEventData)data;

        Vector2 delta = ped.position - joyCenter;
        if (delta.magnitude > joyRadius)
            delta = delta.normalized * joyRadius;

        joyInput = delta / joyRadius;

        if (joyKnob != null)
            joyKnob.anchoredPosition = delta;
    }

    void JoyUp(BaseEventData data)
    {
        joyActive = false;
        joyInput  = Vector2.zero;
        if (joyKnob != null)
            joyKnob.anchoredPosition = Vector2.zero;
    }

    void Update()
    {
        if (movement != null)
            movement.externalMoveInput = joyInput;
    }

    void MakePickupBtn()
    {
        GameObject btn = MakeButton("PICK UP", new Vector2(1, 0), new Vector2(-80, 500), new Vector2(180, 180));
        btn.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (pickup != null) pickup.mobilePickupPressed = true;
        });
    }

    void MakeThrowBtn()
    {
        GameObject btn = MakeButton("THROW", new Vector2(1, 0), new Vector2(-80, 80), new Vector2(180, 180));

        EventTrigger et = btn.AddComponent<EventTrigger>();
        AddTrigger(et, EventTriggerType.PointerDown, _ =>
        {
            if (pickup != null) pickup.mobileThrowHeld = true;
        });
        AddTrigger(et, EventTriggerType.PointerUp, _ =>
        {
            if (pickup != null)
            {
                pickup.mobileThrowHeld     = false;
                pickup.mobileThrowReleased = true;
            }
        });
    }

    void MakeJumpBtn()
    {
        GameObject btn = MakeButton("JUMP", new Vector2(1, 0), new Vector2(-80, 290), new Vector2(180, 180));
        btn.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (movement != null) movement.SetMobileJump();
        });
    }

    GameObject MakeButton(string label, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        GameObject go = MakeImage("Btn_" + label, transform,
            new Color(0.1f, 0.1f, 0.1f, 0.55f), anchor, anchor, pos, size);

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 0.7f);
        cb.pressedColor     = new Color(0.05f, 0.05f, 0.05f, 0.8f);
        btn.colors = cb;

        GameObject txt = new GameObject("Label");
        txt.transform.SetParent(go.transform, false);
        TextMeshProUGUI tmp = txt.AddComponent<TextMeshProUGUI>();
        tmp.text          = label;
        tmp.fontSize      = 34;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        RectTransform trt = txt.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        return go;
    }

    GameObject MakeImage(string name, Transform parent, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.color = color;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = anchorMin;
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;

        return go;
    }

    void AddTrigger(EventTrigger et, EventTriggerType type,
        UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(action);
        et.triggers.Add(entry);
    }
#endif
}