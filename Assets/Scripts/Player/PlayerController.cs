using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Рух")]
    [SerializeField, Range(1f, 20f)]  private float maxSpeed = 8f;
    [SerializeField, Range(5f, 200f)] private float acceleration = 70f;
    [SerializeField, Range(5f, 200f)] private float deceleration = 90f;

    [Header("Земля")]
    [SerializeField] private Transform groundCheck;
    [SerializeField, Range(0.05f, 0.5f)] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Стрибок")]
    [SerializeField, Range(0.5f, 8f)] private float jumpHeight = 3f;
    [SerializeField, Range(0.5f, 8f)] private float baseGravity = 3f;

    [Header("Прийоми тюнінгу")]
    [SerializeField, Range(0f, 0.4f)] private float coyoteTime = 0.12f;
    [SerializeField, Range(0f, 0.4f)] private float jumpBufferTime = 0.15f;

    [Header("Змінна висота стрибка")]
    [SerializeField, Range(1f, 6f)] private float fallMultiplier = 2.2f;
    [SerializeField, Range(1f, 8f)] private float lowJumpMultiplier = 3.5f;

    [Header("Вигляд")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private Rigidbody2D body;
    private PlayerControls controls;
    private float moveInput;
    private bool isGrounded;
    private float coyoteCounter;
    private float jumpBufferCounter;

    private float JumpVelocity =>
        Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y) * baseGravity * jumpHeight);

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        controls = new PlayerControls();

        // Якщо поля порожні — знайдемо на цьому ж об'єкті
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) Debug.LogWarning("PlayerController: Animator не знайдено на Player!");
    }

    private void OnEnable()  => controls.Player.Enable();
    private void OnDisable() => controls.Player.Disable();
    private void OnDestroy() => controls.Dispose();

    private void Update()
    {
        isGrounded = CheckGround();

        bool canRefill = isGrounded && body.linearVelocity.y <= 0.01f;
        coyoteCounter = canRefill ? coyoteTime : coyoteCounter - Time.deltaTime;

        jumpBufferCounter = controls.Player.Jump.WasPressedThisFrame()
            ? jumpBufferTime
            : jumpBufferCounter - Time.deltaTime;

        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            Jump();
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }

        moveInput = controls.Player.Move.ReadValue<Vector2>().x;

        UpdateVisuals();
    }

    private void FixedUpdate()
    {
        float target = moveInput * maxSpeed;
        float rate = Mathf.Abs(target) > 0.01f ? acceleration : deceleration;

        float x = Mathf.MoveTowards(body.linearVelocity.x, target,
                                    rate * Time.fixedDeltaTime);
        body.linearVelocity = new Vector2(x, body.linearVelocity.y);

        if (body.linearVelocity.y < 0f)
            body.gravityScale = baseGravity * fallMultiplier;
        else if (body.linearVelocity.y > 0f && !controls.Player.Jump.IsPressed())
            body.gravityScale = baseGravity * lowJumpMultiplier;
        else
            body.gravityScale = baseGravity;
    }

    private void Jump()
    {
        body.gravityScale = baseGravity;
        body.linearVelocity = new Vector2(body.linearVelocity.x, JumpVelocity);
    }

    private void UpdateVisuals()
    {
        // Джерело істини — фактична швидкість тіла, а не натиснута клавіша
        if (animator != null)
            animator.SetFloat(SpeedHash, Mathf.Abs(body.linearVelocity.x));

        // flipX, а НЕ мінусовий масштаб: інакше GroundCheck переїде
        if (spriteRenderer != null && Mathf.Abs(moveInput) > 0.01f)
            spriteRenderer.flipX = moveInput < 0f;
    }

    private bool CheckGround() =>
        Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Application.isPlaying && isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}