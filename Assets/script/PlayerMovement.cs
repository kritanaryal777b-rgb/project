using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_XR_ENABLED
using UnityEngine.XR;
#endif

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity   = -9.81f;

    [Header("Attack Animation")]
    [SerializeField] private string attackClip   = "Armature|Sword_Attack";
    [SerializeField] private float  attackLen    = 0.7f;

    [HideInInspector] public Vector2 externalMoveInput = Vector2.zero;

    private CharacterController controller;
    private Animator  anim;
    private Vector3   velocity;
    private bool      isGrounded;
    private Vector2   moveInput;
    private bool      jumpInput;
    private Transform modelTransform;
    private string    currentClip = "";
    private bool      isAttacking = false;

    private Vector3 camFwd   = Vector3.forward;
    private Vector3 camRight = Vector3.right;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
            controller = gameObject.AddComponent<CharacterController>();

        Transform child = transform.Find("UAL1_Standard");
        if (child == null)
        {
            Debug.LogError("PlayerMovement: 'UAL1_Standard' not found.");
            return;
        }

        modelTransform = child;
        anim = GetComponentInChildren<Animator>();
    }

    void LateUpdate()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        Vector3 fwd = mainCam.transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude > 0.001f)
        {
            camFwd   = fwd.normalized;
            camRight = mainCam.transform.right;
            camRight.y = 0f;
            camRight   = camRight.normalized;
        }
    }

    void Update()
    {
#if UNITY_XR_ENABLED
        XRInput();
#elif UNITY_ANDROID || UNITY_IOS
        moveInput = externalMoveInput;
#else
        KeyboardInput();
#endif

        MovePlayer();
        UpdateAnim();
    }

    private void KeyboardInput()
    {
        var keyboard = Keyboard.current;
        float h = 0f, v = 0f;

        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)    v += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)  v -= 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)  h -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) h += 1f;

            if (keyboard.spaceKey.wasPressedThisFrame)
                jumpInput = true;
        }

        moveInput = new Vector2(h, v);
    }

    private void XRInput()
    {
#if UNITY_XR_ENABLED
        UnityEngine.XR.InputDevice left =
            UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);

        if (left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 stick))
            moveInput = stick;

        // B button to jump
        UnityEngine.XR.InputDevice right =
            UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
        if (right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool bPressed) && bPressed)
            jumpInput = true;
#endif
    }

    private void MovePlayer()
    {
        isGrounded = controller.isGrounded;

        Vector3 moveDir = (camFwd * moveInput.y + camRight * moveInput.x).normalized;

        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        if (jumpInput && isGrounded)
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        jumpInput = false;

        velocity.y += gravity * Time.deltaTime;

        controller.Move(moveDir * moveSpeed * Time.deltaTime + velocity * Time.deltaTime);

        // rotate model to face move direction
        if (moveDir != Vector3.zero && modelTransform != null)
        {
            Quaternion targetRot = Quaternion.LookRotation(-moveDir);
            modelTransform.rotation = Quaternion.Slerp(
                modelTransform.rotation, targetRot, Time.deltaTime * 12f);
        }
    }

    private void UpdateAnim()
    {
        if (anim == null || isAttacking) return;

        string clipName = moveInput.magnitude > 0.1f
            ? "Armature|Jog_Fwd_Loop"
            : "Armature|Idle_Loop";

        if (clipName != currentClip)
        {
            currentClip = clipName;
            anim.CrossFade(clipName, 0.1f);
        }
    }

    public void TriggerAttack()
    {
        if (anim == null) return;
        isAttacking = true;
        anim.CrossFade(attackClip, 0.05f);
        currentClip = attackClip;
        CancelInvoke(nameof(ClearAttack));
        Invoke(nameof(ClearAttack), attackLen);
    }

    private void ClearAttack()
    {
        isAttacking = false;
        currentClip = "";
    }

    // mobile jump, bypasses InputAction.CallbackContext
    public void SetMobileJump()
    {
        jumpInput = true;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started) jumpInput = true;
    }
}