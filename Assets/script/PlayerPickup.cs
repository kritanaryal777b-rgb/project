using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_XR_ENABLED
using UnityEngine.XR;
#endif

public class PlayerPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickupRange = 4.0f;
    [SerializeField] private LayerMask pickupLayers = ~0;

    [Header("Throw Settings")]
    [SerializeField] private float minThrowForce  = 8f;
    [SerializeField] private float maxThrowForce  = 16f;
    [SerializeField] private float maxChargeTime  = 1.2f;
    [SerializeField] private Transform holdPointOverride;
    private Transform holdPoint;
    [SerializeField] private Vector3 holdRotationOffset = Vector3.zero;

    [Header("Arc Settings")]
    [SerializeField] private int   arcPoints  = 60;
    [SerializeField] private float arcTimeStep = 0.05f;

    [Header("Throw Direction")]
    [SerializeField] private float minThrowUpward = 0.4f;

    [Header("Re-pickup Guard")]
    [SerializeField] private float pickupCooldown = 0.2f;

    [Header("Attack Launch Delay")]
    [SerializeField] private float launchDelay = 0.4f;

    [HideInInspector] public bool mobilePickupPressed  = false;
    [HideInInspector] public bool mobileThrowHeld      = false;
    [HideInInspector] public bool mobileThrowReleased  = false;

    private Pickupable   heldItem;
    private Pickupable   hovered;
    private LineRenderer lineRenderer;

    private bool  isCharging  = false;
    private float chargeStart = 0f;
    private float currentCharge = 0f;

    private float pickupTime = 0f;
    private float dropTime   = -999f;

    private PlayerMovement playerMovement;

    private float   pForce       = 0f;
    private Vector3 pDir         = Vector3.forward;
    private bool    throwPending = false;

#if UNITY_XR_ENABLED
    private bool vrGripHeld      = false;
    private bool vrGripLastFrame = false;
#endif

    void Start()
    {
        lineRenderer = gameObject.GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.startWidth    = 0.06f;
        lineRenderer.endWidth      = 0.01f;
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;

        Material lineMat = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.sharedMaterial = lineMat;

        holdPoint = transform.Find("UAL1_Standard/HoldPoint");
        if (holdPoint == null)
        {
            holdPoint = holdPointOverride;
            if (holdPoint == null)
                Debug.LogWarning("PlayerPickup: HoldPoint not found.");
        }

        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        CheckHover();

#if UNITY_XR_ENABLED
        XRInput(mainCam);
#elif UNITY_ANDROID || UNITY_IOS
        MobileInput(mainCam);
#else
        PCInput(mainCam);
#endif

        if (heldItem != null)
        {
            heldItem.transform.localRotation = Quaternion.Euler(holdRotationOffset);
            UpdateCharge();

            if (isCharging && !throwPending)
                DrawArc(mainCam);
            else
                lineRenderer.positionCount = 0;
        }
        else
        {
            lineRenderer.positionCount = 0;
            isCharging = false;
        }
    }

    private void CheckHover()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, pickupRange, pickupLayers, QueryTriggerInteraction.Ignore);

        float closestD = float.MaxValue;
        Pickupable closest = null;

        foreach (var col in colliders)
        {
            Pickupable p = col.GetComponent<Pickupable>()
                        ?? col.GetComponentInParent<Pickupable>();

            if (p != null && p != heldItem)
            {
                float dist = Vector3.Distance(transform.position, p.transform.position);
                if (dist < closestD)
                {
                    closestD = dist;
                    closest = p;
                }
            }
        }

        if (hovered != closest)
        {
            if (hovered != null) hovered.SetGlow(false);
            if (closest != null) closest.SetGlow(true);
            hovered = closest;
        }
    }

    private void PCInput(Camera mainCam)
    {
#if !UNITY_XR_ENABLED && (UNITY_STANDALONE || UNITY_EDITOR)
        var keyboard = Keyboard.current;
        var mouse    = Mouse.current;

        bool interactPressed = keyboard != null && keyboard.eKey.wasPressedThisFrame;
        bool throwHeld       = mouse != null && mouse.leftButton.isPressed;
        bool throwReleased   = mouse != null && mouse.leftButton.wasReleasedThisFrame;

        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

        if (heldItem == null && hovered != null && interactPressed
            && Time.time - dropTime > pickupCooldown)
        {
            PickupItem(hovered, mainCam);
            return;
        }

        // drop without throwing
        if (heldItem != null && interactPressed && !throwPending)
        {
            DropItem(0f, mainCam);
            isCharging    = false;
            currentCharge = 0f;
            return;
        }

        if (heldItem != null && !throwPending
            && Cursor.lockState == CursorLockMode.Locked
            && Time.time - pickupTime > pickupCooldown)
        {
            if (throwHeld && !isCharging)
            {
                isCharging  = true;
                chargeStart = Time.time;
            }

            if (throwReleased && isCharging)
                ReleaseThrow(mainCam);
        }
#endif
    }

    private void MobileInput(Camera mainCam)
    {
        bool interactPressed = mobilePickupPressed;
        bool throwHeld       = mobileThrowHeld;
        bool throwReleased   = mobileThrowReleased;

        // reset one-frame flags
        mobilePickupPressed = false;
        mobileThrowReleased = false;

        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

        if (heldItem == null && hovered != null && interactPressed
            && Time.time - dropTime > pickupCooldown)
        {
            PickupItem(hovered, mainCam);
            return;
        }

        if (heldItem != null && interactPressed && !throwPending)
        {
            DropItem(0f, mainCam);
            isCharging    = false;
            currentCharge = 0f;
            return;
        }

        if (heldItem != null && !throwPending && Time.time - pickupTime > pickupCooldown)
        {
            if (throwHeld && !isCharging)
            {
                isCharging  = true;
                chargeStart = Time.time;
            }

            if (throwReleased && isCharging)
                ReleaseThrow(mainCam);
        }
    }

    private void XRInput(Camera mainCam)
    {
#if UNITY_XR_ENABLED
        UnityEngine.XR.InputDevice right =
            UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);

        right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool gripNow);

        bool gripDown = gripNow && !vrGripLastFrame;
        bool gripUp   = !gripNow && vrGripLastFrame;
        vrGripLastFrame = gripNow;

        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

        if (heldItem == null && hovered != null && gripDown
            && Time.time - dropTime > pickupCooldown)
        {
            PickupItem(hovered, mainCam);
            return;
        }

        // grip release = throw, use controller vel
        if (heldItem != null && gripUp && !throwPending
            && Time.time - pickupTime > pickupCooldown)
        {
            right.TryGetFeatureValue(
                UnityEngine.XR.CommonUsages.deviceVelocity, out Vector3 vel);

            float speed = Mathf.Clamp(vel.magnitude, 0f, maxThrowForce);
            pForce = Mathf.Max(speed, minThrowForce);
            pDir   = vel.sqrMagnitude > 0.01f ? vel.normalized : mainCam.transform.forward;

            throwPending = true;
            playerMovement?.TriggerAttack();
            Invoke(nameof(DoThrow), launchDelay);
            return;
        }

        // trigger = drop
        right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool triggerDown);
        if (heldItem != null && triggerDown && !throwPending)
            DropItem(0f, mainCam);
#endif
    }

    private void ReleaseThrow(Camera mainCam)
    {
        pForce       = Mathf.Lerp(minThrowForce, maxThrowForce, currentCharge);
        pDir         = ThrowDir(mainCam);
        throwPending = true;
        isCharging   = false;
        currentCharge = 0f;

        playerMovement?.TriggerAttack();
        Invoke(nameof(DoThrow), launchDelay);
    }

    private void DoThrow()
    {
        // no ball, skip
        if (heldItem == null) { throwPending = false; return; }

        heldItem.shootPosition    = heldItem.transform.position;
        heldItem.transform.parent = null;
        heldItem.OnDrop(pDir * pForce);

        if (pForce > 0.01f && GameManager.Instance != null)
            GameManager.Instance.RegisterThrow();

        heldItem     = null;
        throwPending = false;
        dropTime     = Time.time;
        lineRenderer.positionCount = 0;
    }

    private void UpdateCharge()
    {
        if (!isCharging) return;
        currentCharge = Mathf.Clamp01((Time.time - chargeStart) / maxChargeTime);
    }

    private void PickupItem(Pickupable item, Camera mainCam)
    {
        heldItem = item;
        heldItem.SetGlow(false);

        if (hovered == heldItem) hovered = null;

        heldItem.OnPickup();
        heldItem.transform.parent        = holdPoint != null ? holdPoint : mainCam.transform;
        heldItem.transform.localPosition = Vector3.zero;
        heldItem.transform.localRotation = Quaternion.Euler(holdRotationOffset);
        pickupTime = Time.time;
    }

    private void DropItem(float force, Camera mainCam)
    {
        if (heldItem == null) return;

        heldItem.shootPosition    = heldItem.transform.position;
        heldItem.transform.parent = null;
        heldItem.OnDrop(ThrowDir(mainCam) * force);

        if (force > 0.01f && GameManager.Instance != null)
            GameManager.Instance.RegisterThrow();

        heldItem = null;
        dropTime = Time.time;
        lineRenderer.positionCount = 0;
    }

    private Vector3 ThrowDir(Camera mainCam)
    {
        Vector3 dir = mainCam.transform.forward;
        dir.y = Mathf.Max(dir.y, minThrowUpward);
        dir.Normalize();
        return dir;
    }

    private void DrawArc(Camera mainCam)
    {
        if (lineRenderer == null || heldItem == null) return;

        float dispForce = isCharging
            ? Mathf.Lerp(minThrowForce, maxThrowForce, currentCharge)
            : minThrowForce;

        Vector3 startPos = heldItem.transform.position;
        Vector3 dir = mainCam.transform.forward;
        dir.y = Mathf.Max(dir.y, minThrowUpward);
        dir.Normalize();

        Vector3 startVel = dir * dispForce;
        lineRenderer.positionCount = arcPoints;

        // color shifts red as charge increases
        Color startColor = Color.Lerp(
            new Color(1f, 1f, 1f, 0.9f),
            new Color(1f, 0.2f, 0.1f, 0.9f),
            currentCharge);
        lineRenderer.startColor = startColor;
        lineRenderer.endColor   = new Color(startColor.r, startColor.g, startColor.b, 0f);

        for (int i = 0; i < arcPoints; i++)
        {
            float t = i * arcTimeStep;
            Vector3 point = startPos + startVel * t + 0.5f * Physics.gravity * t * t;
            lineRenderer.SetPosition(i, point);
        }
    }
}