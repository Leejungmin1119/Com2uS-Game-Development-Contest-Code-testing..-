using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;

public class Player : MonoBehaviour
{
    public static Player instance;
    public PlayerMove playermove;
    public PlayerJump playerJump;
    [SerializeField] public PlayerMoveStats moveStats;



    [Header("플레이어 옵션 체크")]
    [SerializeField] static public bool isGround;
    [SerializeField] static public bool isBump;
    [SerializeField] static public bool isFilp;
    [SerializeField] static public bool isTouchWall;

    [SerializeField] static public bool isDashing;
    [SerializeField] static public bool isAirDashing;

    public GameObject Player_Object;

    // 클래스 선언
    [SerializeField] private Collider2D Player_BodyCollider;
    [SerializeField] private Collider2D Player_footCollider;
    private Animator Player_Animator;

    // 레이캐스트
    private RaycastHit2D groundHit;
    private RaycastHit2D bumpedHeat;
    private RaycastHit2D wallHit;
    private RaycastHit2D lastWallHit;
    public Rigidbody2D Player_rigidBody;

    private SpriteRenderer Player_Sprite;


    void Awake()
    {
        //******참조
        Player_Object = this.gameObject;
        Player_Animator = GetComponent<Animator>();
        Player_BodyCollider = GetComponent<Collider2D>();
        Player_rigidBody = GetComponent<Rigidbody2D>();
        Player_Sprite = GetComponent<SpriteRenderer>();

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {


        Player_Animator.SetFloat("isRun", Mathf.Abs(InputManager.Movement.x));// 달리기 애니매이션 설정
    }
    void LateUpdate()
    {
        //******웅크리기
        if (Input.GetKeyDown(KeyCode.S)) { Player_Animator.SetTrigger("isCrouch"); }
        if (Input.GetKeyUp(KeyCode.S)) { Player_Animator.SetTrigger("Stand"); }
    }


    void FixedUpdate()
    {
        Turn(InputManager.Movement);
        //******지형 탐색
        CollisionChecks();

        ApplyVelocity();

    }

    public void Turn(Vector2 moveInput)
    {
        //좌우 방향 전환(flip이용)
        if (moveInput.x != 0)
        {
            Player_Sprite.flipX = moveInput.x < 0;
            isFilp = true;
        }
        else
        {
            isFilp = false;
        }
    }

    ///********************************///
    /// Colliger Cheking **************///
    ///********************************///
    private void IsGrounded()
    {
        //***** 땅 지형 체킹 *****//

        // 1.내 발밑에 있는 오브젝트가 땅인지  박스를 만들고 RayCast로 판별
        Vector2 boxCastOrigin = Player_footCollider.bounds.center;
        Vector2 boxCastSize = new Vector2(Player_footCollider.bounds.size.x * 0.9f, moveStats.GroundDetectionRayLenght);

        // 2.발밑 해당 콜리더의 값을 반환 및 적용
        // 매개변수(시작점,크기,영점,방향,길이,체크할레이어)
        groundHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.down, moveStats.GroundDetectionRayLenght, moveStats.GroundLayer);

        if (groundHit.collider != null)
        {
            isGround = true;
        }
        else
        {
            isGround = false;
        }
    }
    private void IsHeadBump()
    {
        //***** 머리가 천장 체킹 *****//

        // 땅 지형과 동일(발사 방향, 시작 위치만 다름)

        Vector2 boxCastOrigin = new Vector2(Player_BodyCollider.bounds.center.x, Player_BodyCollider.bounds.max.y);
        Vector2 boxCastSize = new Vector2(Player_BodyCollider.bounds.size.x * 0.9f, moveStats.GroundDetectionRayLenght);

        // 매개변수(시작점,크기,영점,방향,길이,체크할레이어)
        bumpedHeat = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.up, moveStats.GroundDetectionRayLenght, moveStats.GroundLayer);

        if (bumpedHeat.collider != null)
        {
            isBump = true;
        }
        else
        {
            isBump = false;
        }
    }

    private void IsTouchWall()
    {
        float EndPoint = 0f;

        if (isFilp)
        {
            EndPoint = Player_BodyCollider.bounds.max.x;
        }
        else { EndPoint = Player_BodyCollider.bounds.min.x; }

        float adjustedHeight = Player_BodyCollider.bounds.size.y * moveStats.WallDectectionRayHeight;

        Vector2 boxCastOrigin = new Vector2(EndPoint, Player_BodyCollider.bounds.center.y);
        Vector2 boxCastSize = new Vector2(Player_BodyCollider.bounds.size.x * 0.9f, adjustedHeight);

        wallHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.right, moveStats.WallDectectionRayLength, moveStats.GroundLayer);
        if (wallHit.collider != null)
        {
            lastWallHit = wallHit;
            isTouchWall = true;
        }
        else { isTouchWall = false; }

    }

    public float OutWallJump()
    {
        int dirMultiplier = 0;
        Vector2 hitPoint = lastWallHit.collider.ClosestPoint(Player_BodyCollider.bounds.center);

        if (hitPoint.x > transform.position.x)
        {
            dirMultiplier = -1;
        }
        else { dirMultiplier = 1; }

        return (Mathf.Abs(moveStats.WallJumpDirection.x) * dirMultiplier);
    }


    private void OnDrawGizmos()
    {
        if (moveStats == null || Player_footCollider == null) return;

        // IsGrounded()와 동일하게 '발 위치'를 시작점으로 설정합니다.
        Vector2 boxCastOrigin = Player_footCollider.bounds.center;
        Vector2 boxCastSize = new Vector2(Player_footCollider.bounds.size.x * 0.9f, moveStats.GroundDetectionRayLenght);

        float rayLength = moveStats.GroundDetectionRayLenght;

        // 감지 성공 여부에 따라 색상 설정
        Color rayColor = isGround ? Color.green : Color.red;
        Gizmos.color = rayColor;

        // BoxCast의 끝 지점을 계산합니다. (시작점 + 발사 방향 * 길이)
        Vector2 boxCastEndPoint = boxCastOrigin + Vector2.down * rayLength;

        // 1. 박스 캐스트의 전체 감지 영역을 그립니다.
        Gizmos.DrawWireCube(boxCastEndPoint, boxCastSize);

        // 2. 발사 시작점도 시각화하면 좋습니다.
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(boxCastOrigin, boxCastSize * 1.5f); // 시작점은 좀 더 크게 표시

        // 3. Scene 뷰 상단의 Gizmos 버튼이 켜져 있는지 확인하세요!
    }

    private void CollisionChecks()
    {
        // 여러 부분의 지형이 어떤 것인지 판별
        // 매개변수 : 시작점,크기,영점,방향,길이,체크할레이어
        IsGrounded();
        IsHeadBump();
        IsTouchWall();
    }

    public void ApplyVelocity()
    {
        if (!isDashing)
        {
            PlayerJump.VerticalVelocity = Mathf.Clamp(PlayerJump.VerticalVelocity, -moveStats.MaxFallSpeed, 50f);
        }
        else
        {
            PlayerJump.VerticalVelocity = Mathf.Clamp(PlayerJump.VerticalVelocity, -50f, 50f);
        }

        Player_rigidBody.linearVelocity = new Vector2(Player.instance.playermove.movevelocity, PlayerJump.VerticalVelocity);
    }


}
