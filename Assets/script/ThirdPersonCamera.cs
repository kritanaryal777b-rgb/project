using UnityEngine;
using TouchPhase = UnityEngine.TouchPhase;

#if !UNITY_XR_ENABLED && (UNITY_STANDALONE || UNITY_EDITOR)
using UnityEngine.InputSystem;
#endif

public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float distance = 4f;
    [SerializeField] private float height = 1.2f;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float touchSensitivity = 0.15f;
    [SerializeField] private float minPitch = -20f;
    [SerializeField] private float maxPitch = 60f;
    [SerializeField] private float colRadius = 0.2f;
    [SerializeField] private LayerMask colLayers = ~0;

    private float yaw = 0f;
    private float pitch = 10f;
    private float smoothDist;
    private Transform playerRoot;

    void Start()
    {
#if UNITY_XR_ENABLED
        // vr handles cam itself
        enabled = false;
        return;
#endif

        if (cameraTransform == null)
            cameraTransform = Camera.main?.transform;

        playerRoot = transform.parent;

        if (playerRoot != null)
            yaw = playerRoot.eulerAngles.y;

        smoothDist = distance;

#if UNITY_STANDALONE || UNITY_EDITOR
        // lock cursor on start
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
#endif
    }

    void Update()
    {
#if UNITY_XR_ENABLED
        return;
#endif

#if UNITY_ANDROID || UNITY_IOS
        TouchLook();
#else
        MouseLook();
#endif

        if (playerRoot != null)
            transform.position = playerRoot.position;
    }

    private void TouchLook()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.position.x < Screen.width * 0.5f) continue; // left = joystick
            if (touch.phase != TouchPhase.Moved) continue;

            yaw   += touch.deltaPosition.x * touchSensitivity;
            pitch -= touch.deltaPosition.y * touchSensitivity;
            pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }

    private void MouseLook()
    {
#if !UNITY_XR_ENABLED && (UNITY_STANDALONE || UNITY_EDITOR)
        var keyboard = Keyboard.current;
        var mouse    = Mouse.current;

        // esc unlocks
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        // right click to relock
        if (mouse != null && mouse.rightButton.wasPressedThisFrame
            && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        if (Cursor.lockState == CursorLockMode.Locked && mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue() * 0.1f;
            yaw   += delta.x * mouseSensitivity;
            pitch -= delta.y * mouseSensitivity;
            pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
#endif
    }

    void LateUpdate()
    {
#if UNITY_XR_ENABLED
        return;
#endif
        if (cameraTransform == null) return;

        Quaternion camRot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivotPos = playerRoot != null
            ? playerRoot.position + Vector3.up * height
            : transform.position  + Vector3.up * height;

        Vector3 offset = camRot * new Vector3(0f, 0f, -distance);

        // collision check, pull cam in if blocked
        RaycastHit[] hits = Physics.SphereCastAll(
            pivotPos, colRadius, offset.normalized, distance, colLayers);

        float targetDist = distance;
        foreach (var hit in hits)
        {
            if (hit.transform.root == transform.root) continue;
            if (hit.distance < targetDist)
                targetDist = hit.distance;
        }

        smoothDist = Mathf.Lerp(smoothDist, targetDist, Time.deltaTime * 15f);

        Vector3 finalOffset = camRot * new Vector3(0f, 0f, -smoothDist);
        cameraTransform.position = pivotPos + finalOffset;
        cameraTransform.LookAt(pivotPos);
    }
}