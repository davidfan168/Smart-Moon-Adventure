using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum Direction {Left, Right }
public enum PlayerState { Jump, Fall, Coyote, Grounded };

public class Player : MonoBehaviour
{
    // Player handles its own accelerations, but it assigns velocity to rigidbody2d
    [Header("Jump")]
    public float jumpBuffer;
    public float coyoteTime;
    public float maxFallSpeed;
    
    public float jumpGravity;
    public float fallGravity;
    public float maxHeight;

    public float heightEpsilon;
    
    private float initialJumpVelocity;

    [Header("Movement")] 
    public float horizontalSpeed;

    private new Rigidbody2D rigidbody2D;

    [Header("State Machine")]
    public PlayerState initialState;
    
    private State currentState;
    private Dictionary<PlayerState, State> allStates = new Dictionary<PlayerState, State>();
    
    void Start()
    {
        // Caching
        rigidbody2D = gameObject.GetComponent<Rigidbody2D>();
        
        // Initializing variables
        initialJumpVelocity = Mathf.Sqrt(jumpGravity * maxHeight);
        rigidbody2D.gravityScale = 0;
        
        // Initializing state machine
        InitStates();

        EnterState(initialState);
    }
    
    void Update()
    {
        currentState.Tick();
    }
    
    
    //public functions start here
    public void ChangeState(PlayerState newState)
    {
        currentState.Exit();
        
        Debug.Log(newState);
        EnterState(newState);
    }

    //helper functions start here
    private void InitStates()
    {
        JumpState jump = ScriptableObject.CreateInstance<JumpState>();
        jump.Initialize(this, gameObject);
        jump.horizontalSpeed = this.horizontalSpeed;
        jump.heightEpsilon = this.heightEpsilon;
        jump.gravity = this.jumpGravity;
        jump.initialJumpVelocity = this.initialJumpVelocity;
        allStates.Add(PlayerState.Jump, jump);
        
        FallState fall = ScriptableObject.CreateInstance<FallState>();
        fall.Initialize(this, gameObject);
        fall.horizontalSpeed = this.horizontalSpeed;
        fall.heightEpsilon = this.heightEpsilon;
        fall.jumpBuffer = this.jumpBuffer;
        fall.maxFallSpeed = this.maxFallSpeed;
        fall.gravity = this.fallGravity;
        allStates.Add(PlayerState.Fall, fall);
        
        CoyoteState coyote = ScriptableObject.CreateInstance<CoyoteState>();
        coyote.Initialize(this, gameObject);
        coyote.horizontalSpeed = this.horizontalSpeed;
        coyote.heightEpsilon = this.heightEpsilon;
        coyote.jumpBuffer = this.jumpBuffer;
        coyote.maxFallSpeed = this.maxFallSpeed;
        coyote.gravity = this.fallGravity;
        coyote.coyoteTime = this.coyoteTime;
        allStates.Add(PlayerState.Coyote, coyote);

        GroundedState grounded = ScriptableObject.CreateInstance<GroundedState>();
        grounded.Initialize(this, gameObject);
        grounded.horizontalSpeed = this.horizontalSpeed;
        grounded.heightEpsilon = this.heightEpsilon;
        allStates.Add(PlayerState.Grounded, grounded);
    }

    private State getState(PlayerState s)
    {
        return allStates[s];
    }

    private void EnterState(PlayerState s)
    {
        currentState = getState(s);
        
        currentState.Enter();
    }
}

//Good tutorial from https://www.raywenderlich.com/6034380-state-pattern-using-unity
public abstract class State : ScriptableObject
{
    public float heightEpsilon;
    public float horizontalSpeed;

    protected Player stateMachine;
    protected GameObject player;
    protected Rigidbody2D rigidbody2D;
    protected BoxCollider2D collider2D;
    protected Animator animator;
    
    protected LayerMask platformLayerMask;
    
    public virtual void Initialize(Player stateMachine, GameObject player)
    {
        this.stateMachine = stateMachine;
        this.player = player;
        rigidbody2D = player.GetComponent<Rigidbody2D>();
        collider2D = player.GetComponent<BoxCollider2D>();
        animator = player.GetComponent<Animator>();

        platformLayerMask = LayerMask.GetMask("Platform");
    }

    public virtual void Enter()
    {
        
    }

    public virtual void Tick()
    {
        
    }

    public virtual void Exit()
    {

    }

    protected bool onGround()
    {
        RaycastHit2D raycastHit = Physics2D.BoxCast(collider2D.bounds.center, collider2D.bounds.size, 0f, Vector2.down, heightEpsilon, platformLayerMask);

#if UNITY_EDITOR
        /////Starts to draw the collision box
        Color rayColor;
        if (raycastHit.collider != null)
        {
            rayColor = Color.green;
        }
        else
        {
            rayColor = Color.red;
        }
        Debug.DrawRay(collider2D.bounds.center + new Vector3(collider2D.bounds.extents.x, 0), Vector2.down * (collider2D.bounds.extents.y + heightEpsilon), rayColor);
        Debug.DrawRay(collider2D.bounds.center - new Vector3(collider2D.bounds.extents.x, 0), Vector2.down * (collider2D.bounds.extents.y + heightEpsilon), rayColor);
        Debug.DrawRay(collider2D.bounds.center - new Vector3(collider2D.bounds.extents.x, collider2D.bounds.extents.y + heightEpsilon), Vector2.right * (2 * collider2D.bounds.extents.x), rayColor);
        /////end
#endif

        if (raycastHit.collider != null)
        {
            return true;
        }
        return false;
    }
}

public class AirborneState : State
{
    public float gravity;
    public float maxFallSpeed;

    public override void Enter()
    {
        base.Enter();
    }

    public override void Tick()
    {
        base.Tick();

        Move();
    }
    
    public override void Exit()
    {
        base.Exit();
    }

    public void Move()
    {
        float hVelocity = Input.GetAxisRaw("Horizontal") * horizontalSpeed;;
        float vVelocity = rigidbody2D.velocity.y - gravity * Time.deltaTime;
        if (vVelocity < -maxFallSpeed)
        {
            vVelocity = maxFallSpeed;
        }
        Vector2 velocity = new Vector2(hVelocity, vVelocity);
        rigidbody2D.velocity = velocity;
    }
}

public class JumpState : AirborneState
{
    public float initialJumpVelocity;
    
    public override void Enter()
    {
        base.Enter();
        rigidbody2D.velocity = new Vector2(rigidbody2D.velocity.x, initialJumpVelocity);
    }

    public override void Tick()
    {
        base.Tick();

        if (!Input.GetButton("Jump"))
        {
            stateMachine.ChangeState(PlayerState.Fall);
        }

        if (rigidbody2D.velocity.y <= 0.001)
        {
            stateMachine.ChangeState(PlayerState.Fall);
        }
    }
    
    public override void Exit()
    {
        base.Exit();
        rigidbody2D.velocity = new Vector2(rigidbody2D.velocity.x, 0);
    }
}

public class FallState : AirborneState
{
    public float jumpBuffer;

    private float jumpBufferRemaining;
    
    public override void Enter()
    {
        base.Enter();
    }

    public override void Tick()
    {
        base.Tick();

        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferRemaining = jumpBuffer;
        }
        
        if (onGround())
        {
            if (jumpBufferRemaining > 0)
            {
                stateMachine.ChangeState(PlayerState.Jump);
            }
            stateMachine.ChangeState(PlayerState.Grounded);
        }

        jumpBufferRemaining -= Time.deltaTime;
    }
    
    public override void Exit()
    {
        base.Exit();
    }
}

public class CoyoteState : FallState
{
    public float coyoteTime;
    
    public override void Enter()
    {
        base.Enter();
    }

    public override void Tick()
    {
        base.Tick();
        if (Input.GetButtonDown("Jump"))
        {
            stateMachine.ChangeState(PlayerState.Jump);
        }
    }
    
    public override void Exit()
    {
        base.Exit();
    }
}

public class GroundedState : State
{

    public override void Enter()
    {
        base.Enter();
    }

    public override void Tick()
    {
        base.Tick();

        rigidbody2D.velocity = Input.GetAxisRaw("Horizontal") * Vector2.right * horizontalSpeed;

        if (!onGround())
        {
            stateMachine.ChangeState(PlayerState.Coyote);
        }

        Debug.Assert(onGround());
        
        if(Input.GetButtonDown("Jump"))
        {
            stateMachine.ChangeState(PlayerState.Jump);
        }
    }
    
    public override void Exit()
    {
        base.Exit();
    }
}