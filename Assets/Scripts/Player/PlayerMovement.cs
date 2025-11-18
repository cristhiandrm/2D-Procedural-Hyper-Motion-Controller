using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerMovement : MonoBehaviour
{
    public event Action OnAttack;

    public bool IsGrounded { get; private set; }
    public float MoveInput { get; private set; }
    public bool IsFacingRight { get; private set; } = true;

    private Rigidbody2D rb;
    private PlayerControls playerControls;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction attackAction; 

    [Header("Movimiento Horizontal")]
    [SerializeField] private float moveSpeed = 10f;
    [Header("Salto")]
    [SerializeField] private float jumpForce = 16f;
    [SerializeField] private int maxJumps = 2;
    private int jumpCount;
    [Header("Detección de Suelo")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.2f;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerControls = new PlayerControls();
        moveAction = playerControls.Gameplay.Move;
        jumpAction = playerControls.Gameplay.Jump;
        attackAction = playerControls.Gameplay.Attack;
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        attackAction.Enable();

        jumpAction.performed += HandleJump;
        attackAction.performed += HandleAttack;
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        attackAction.Disable();

        jumpAction.performed -= HandleJump;
        attackAction.performed -= HandleAttack;
    }


    void Start()
    {
        jumpCount = maxJumps;
    }

    void Update()
    {
        MoveInput = moveAction.ReadValue<float>();
        IsGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (IsGrounded && rb.linearVelocity.y <= 0)
        {
            jumpCount = maxJumps;
        }

        if (MoveInput > 0 && !IsFacingRight) Flip();
        else if (MoveInput < 0 && IsFacingRight) Flip();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(MoveInput * moveSpeed, rb.linearVelocity.y);
    }

    private void Flip()
    {
        IsFacingRight = !IsFacingRight;
        transform.Rotate(0f, 180f, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }

    private void HandleJump(InputAction.CallbackContext context)
    {
        if (jumpCount > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
            jumpCount--;
            IsGrounded = false;
        }
    }

    private void HandleAttack(InputAction.CallbackContext context)
    {
        OnAttack?.Invoke();
    }
}