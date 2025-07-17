using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{  
    private Rigidbody2D body;
    private BoxCollider2D boxCollider;
    [SerializeField]private Animator animator;
    [SerializeField] private float speed;
    [SerializeField] private float fallMultiplier;
    [SerializeField] private float jumpForce;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask trampolineLayer;
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private float coyoteTime;
    [SerializeField] private float maxFallSpeed;
    private float timeAfterLeavingGround;
    public float castDistance;
    public float castDistancelw;
    public float castDistancerw;
    public bool canMove = true;
    public Vector2 boxSize;
    public Vector2 boxSize1;
    public Vector2 boxSize2;
    PlayerRespawn playerRespawn;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private int jumps;
    public int overJumps = 0; 
    public bool canDash = false;
    public int nDashes = 0;
    private int currentOverJumps = 0;
    AudioManager audioManager;
    public bool OpenGatesGuide = false;
    public GameObject GatesGuide;
    
    private Vector2 dashStart;
    private int dashDirection = 1;
    private bool isDashing = false;
    public float dashDistance = 3f;
    public float dashSpeed = 20f;

    // MOVMENT METHODS
    public static bool MoveLeftKey => Input.GetKey(Keybinds.GetKey("MoveLeft")); 
    public static bool MoveRightKey => Input.GetKey(Keybinds.GetKey("MoveRight"));
    public static bool JumpKeyUp => Keybinds.GetKeyUp("Jump");
    public static bool JumpKeyDown => Keybinds.GetKeyDown("Jump");
    public static bool DashKey => Keybinds.GetKeyDown("Dash");
    public static bool OpenGatesGuideKeyUp => Input.GetKey(Keybinds.GetKey("OpenGatesGuide"));
    public static bool OpenGatesGuideKeyDown => Input.GetKeyDown(Keybinds.GetKey("OpenGatesGuide"));
    
    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        playerRespawn = GetComponent<PlayerRespawn>();
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
        playerRespawn.currentCheckpoint = transform;
        Keybinds.LoadDefaultsIfEmpty();
        LoadItems();
    }
    void FixedUpdate()
    {
        if (body.linearVelocity.y < 0) 
        {
            body.linearVelocity += Vector2.down * (fallMultiplier * Time.fixedDeltaTime);
        }
        if (isDashing)
        {
            body.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
            trailRenderer.emitting = true;
            float dashedDistance = Mathf.Abs(transform.position.x - dashStart.x);
            if (dashedDistance >= dashDistance)
            {
                body.linearVelocity = Vector2.zero;
                isDashing = false;
                trailRenderer.emitting = false;
            }
        }
    }
    private void Update()
    {
        float horizontalInput = 0f;
        if (MoveLeftKey) horizontalInput -= 1f;
        if (MoveRightKey) horizontalInput += 1f;
        if (!canMove) horizontalInput = 0f;

        // Flip the player sprite based on the direction they are moving
        if(horizontalInput > 0)
        {
            transform.localScale = new Vector3(1, 1, -6);
        }
        else if(horizontalInput < 0)
        {
            transform.localScale = new Vector3(-1, 1, -6);
        }
        // Animation -- run animation
        animator.SetBool("run", horizontalInput != 0 && isGrounded());

        animator.SetBool("jump", !isGrounded());

        if(OpenGatesGuideKeyDown && OpenGatesGuide)
        {
            GatesGuide.SetActive(true);
        }
        if (OpenGatesGuideKeyUp && OpenGatesGuide)
        {
            GatesGuide.SetActive(false);
        }

        if (DashKey && canDash && nDashes > 0)
        {
            dashDirection = transform.localScale.x > 0 ? 1 : -1;
            dashStart = transform.position;
            isDashing = true;
            trailRenderer.emitting = true;
            nDashes=0;
            audioManager.playSFX(audioManager.dashSound);
        }
        if(onLeftWall() || onRightWall())
        {
            if (isDashing)
            {
                isDashing = false;
                trailRenderer.emitting = false;
                body.linearVelocity = Vector2.zero;
            }
        }
        // Movement mechanic -- move left or right
        float targetSpeed = 0;

        if (!(onLeftWall() || onRightWall()) || (horizontalInput > 0 && !onRightWall()) || (horizontalInput < 0 && !onLeftWall()))
        {
            if (horizontalInput > 0) targetSpeed = speed;
            else if (horizontalInput < 0) targetSpeed = -speed;
        }

        body.linearVelocity = new Vector2(targetSpeed, body.linearVelocity.y);

        //Coyote time
        
        if(isGrounded() || onTrampoline()) 
        {
            timeAfterLeavingGround = 0f;
            jumps = 1;
            nDashes = 1;
            currentOverJumps = overJumps; 
        }
        else
        {
            timeAfterLeavingGround += Time.deltaTime;
            if (timeAfterLeavingGround > coyoteTime)
            {
                jumps = 0;
            }
        }

        
        // Jump mechanic -- jump if the player is grounded
        if(JumpKeyDown && canJump())
        {
            body.gravityScale = 6.5f;
            body.linearVelocity = new Vector2(horizontalInput * speed, jumpForce);

            if(jumps > 0) jumps--;
            else if (currentOverJumps > 0) currentOverJumps--;
        }
        if (JumpKeyUp)
        {
            body.gravityScale = 20f;
            jumps--;
        }
        
        if(body.linearVelocity.y < -maxFallSpeed)
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, -maxFallSpeed);
        }
        if (onTrampoline())
            {
                body.gravityScale = 20f;
                body.linearVelocity = new Vector2(body.linearVelocity.x, Math.Max(body.linearVelocity.y, jumpForce * 2f));
            } 
        }
    private bool isGrounded()
    {
        return Physics2D.BoxCast(transform.position, boxSize, 0, -transform.up, castDistance, groundLayer) || 
        Physics2D.BoxCast(transform.position, boxCollider.size, 0, -transform.up, castDistance, platformLayer);
    }
    private bool onLeftWall()
    {
        return Physics2D.BoxCast(boxCollider.bounds.center, boxSize1, 0, -transform.right, castDistancelw, groundLayer);
    }
    private bool onRightWall()
    {
        return Physics2D.BoxCast(boxCollider.bounds.center, boxSize2, 0, transform.right, castDistancerw, groundLayer);
    }
    private bool onTrampoline()
    {
        return Physics2D.BoxCast(transform.position, boxSize, 0, Vector2.down, castDistance, trampolineLayer) ||
        Physics2D.BoxCast(boxCollider.bounds.center, boxSize2, 0, transform.right, castDistancerw, trampolineLayer) ||
        Physics2D.BoxCast(boxCollider.bounds.center, boxSize2, 0, -transform.right, castDistancerw, trampolineLayer);
    }
    private void LoadItems()
    {
        if (SceneManager.GetActiveScene().buildIndex >= 3)
        {
            overJumps = 1;
        }
        if (SceneManager.GetActiveScene().buildIndex >= 4)
        {
            canDash = true;
        }
    }
    private bool canJump()
    {
        return jumps + currentOverJumps > 0 && canMove;   
    }

    public void stopMovement()
    {
        body.linearVelocity = Vector2.zero;
        trailRenderer.emitting = false;
        animator.SetBool("run", false);
        animator.SetBool("jump", false);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "NextLevel")
        {
            audioManager.playSFX(audioManager.portalSound);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }        
    }
    // private void OnDrawGizmos()
    // {
    //     // Floor
    //     Gizmos.color = Color.green;
    //     Gizmos.DrawWireCube(transform.position - transform.up * castDistance, boxSize);

    //     // Left wall
    //     Gizmos.color = Color.red;
    //     Gizmos.DrawWireCube(boxCollider.bounds.center - transform.right * castDistancelw, boxSize1);

    //     // Right wall
    //     Gizmos.color = Color.blue;
    //     Gizmos.DrawWireCube(boxCollider.bounds.center + transform.right * castDistancerw, boxSize2);
    // }
}
