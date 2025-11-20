using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PlayerMoveController : MonoBehaviour
{

    [SerializeField] private BoxCollider2D coll;
    [SerializeField] public RaycastCorners RayCastCorners;
    [SerializeField] private PlayerMoveStatsData moveStats;

    public const float CollisionPading = 0.015f;

    [Range(2,100)] public int NumOfHorizontalRays = 4; // 수평 레이캐스트 광선 갯수
    [Range(2,100)] public int NumOfVerticalRays = 4; // 수직 레이캐스트 광선 갯수
    private float _HorizontalRaySpace;
    private float _VerticalRaySpace;

    [Header("지형확인")]
    [SerializeField] public bool IsCollidingAbove { get; private set; }
    [SerializeField] public bool IsCollidingBelow { get; private set; }
    [SerializeField] public bool IsCollidingLeft { get; private set; }
    [SerializeField] public bool IsCollidingRight { get; private set; }


    public class RaycastCorners
    {
        public Vector2 BottomLeft;
        public Vector2 BottomRight;
        public Vector2 TopLeft;
        public Vector2 TopRight;
    }

    [SerializeField] private Rigidbody2D PlayerRigidBody;
    public void Start()
    {
        RayCastCorners = new RaycastCorners();
        coll = GetComponent<BoxCollider2D>();
        PlayerRigidBody = GetComponent<Rigidbody2D>();
        moveStats = PlayerMovementManager.instance.moveStats;

        CalculateRaySpacing();
    }

    public void Move(Vector2 velocity)
    {
        UpdateRaycastCorners();
        ResetCollisionStates();

        ResolveHoriztonalMovement(ref velocity);
        ResolveVerticalMovement(ref velocity);

        PlayerRigidBody.MovePosition(velocity + PlayerRigidBody.position);
    }

    /// <summary>
    /// Collision 상태 초기화
    /// </summary>
    private void ResetCollisionStates()
    {
        IsCollidingAbove = false;
        IsCollidingBelow = false;
        IsCollidingLeft = false;
        IsCollidingRight = false;

    }

    /// <summary>
    /// 플레이어의 4개의 모서리의 위치(점 x,y)를 업데이트를 하는 함수
    /// </summary>
    private void UpdateRaycastCorners()
    {
        //콜라이더 불러오기
        Bounds bounds = coll.bounds;
        bounds.Expand(CollisionPading * -2);// 경게 줄이기

        //2. 콜라이더 업데이트(좌하단,좌상단,우하단,우상단) 총 4개의 Raycast 값 업데이트
        RayCastCorners.BottomLeft = new Vector2(bounds.min.x,bounds.min.y);
        RayCastCorners.BottomRight = new Vector2(bounds.max.x,bounds.min.y);
        RayCastCorners.TopLeft = new Vector2(bounds.min.x,bounds.max.y);
        RayCastCorners.TopRight = new Vector2(bounds.max.x,bounds.max.y);
    }

    /// <summary>
    /// 플레이어 RayCast(탐지) 영역 간격을 조절하는 함수 , 기본값은 플레이어 크기값을 가짐.
    /// </summary>
    private void CalculateRaySpacing()
    {
        //콜라이더 불러오기
        Bounds bounds = coll.bounds;
        bounds.Expand(CollisionPading * -2);// 경게 줄이기(음수라서 확장이 아닌 영역 축소)

        // x 축 y 축 레이의 길이값
        _HorizontalRaySpace = bounds.size.y / (NumOfHorizontalRays -1);
        _VerticalRaySpace = bounds.size.x / (NumOfVerticalRays -1);
    }

    /// <summary>
    /// 수평에서의 실제 해당 위치의 데이터를 불러오고 거기에 물체가 접촉했는지 RayCast로 판단
    /// </summary>
    /// <param name="rayOrigin">삼향연산자로 좌우를 확인하여 풀레이어의 RayCast 시작점을 알려준다. </param>
    private void ResolveHoriztonalMovement(ref Vector2 velocity)
    {
        // 1. 방향과 레이길이 정하기
        float directionX = (velocity.x <= 0) ? -1f:1f;
        float rayLength = Mathf.Abs(velocity.x) + CollisionPading;


        for(int i=0;i < NumOfHorizontalRays;i++)
        {
            // 2. 속도를 통해서 방향을 정하고 RayCast 시작점 설정 및 레이 탐색
            Vector2 rayOrigin = (directionX == -1) ? RayCastCorners.BottomLeft : RayCastCorners.BottomRight;
            rayOrigin +=Vector2.up*(_HorizontalRaySpace *i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin,Vector2.right * directionX,rayLength,moveStats.GroundLayer);
            //시작점,방향,길이,탐색 오브젝트

            // 앞에 물체가 있을시 로직
            if(hit)
            {

                // 3. 충돌하기까지의 거리만큼 속도를 주고 거리가 줄어들때마다 rayLength 갱신한다.
                // 따라서 플레이어는 충돌하기 완전직전에 멈추게 설계가 됨.
                velocity.x = (hit.distance - CollisionPading) * directionX;
                rayLength = hit.distance;

                // 방향을 왼쪽 오른쪽으로 구분하여 기록
                if(directionX == -1)
                {
                    IsCollidingLeft = true;
                }

                else if(directionX == 1)
                {
                    IsCollidingRight = true;
                }
            }


            /// +4. RayCast 시각화 구현
            if(moveStats.DebugShowWallHit) // 시각화 활성화 확인
            {
                //똑같이 이전처럼 시작점 정하고 RayCast 발사해서 물체 탐지한다.
                float debugRayLength = rayLength; // 시각화 레이 설정
                Vector2 debugRayOrigin = rayOrigin; // 방향 설정

                bool didHit = Physics2D.Raycast(debugRayOrigin,Vector2.right * directionX,debugRayLength,moveStats.GroundLayer);

                // 만약 맞았다면 cyan색으로 안맞으면 레드로
                Color rayColor = didHit? Color.cyan:Color.red;
                Debug.DrawRay(debugRayOrigin,Vector2.right*directionX*debugRayLength,rayColor);//실제 시각화(이걸해야 실제로 보인다.)

            }
        }
    }

    /// <summary>
    /// 수직에서의 실제 해당 위치의 데이터를 불러오고 거기에 물체가 접촉했는지 RayCast로 판단
    /// </summary>
    /// <param name="rayOrigin">삼향연산자로 좌우를 확인하여 풀레이어의 RayCast 시작점을 알려준다. </param>
    private void ResolveVerticalMovement(ref Vector2 velocity)
    {
        // 1. 방향과 레이길이 정하기
        float directionY = (velocity.y <= 0) ? -1f:1f;
        float rayLength = Mathf.Abs(velocity.y) + CollisionPading +moveStats.ExtraRayDebugDistance;

        for(int i=0;i < NumOfVerticalRays;i++)
        {
            // 2. 속도를 통해서 위쪽인지 아랫쪽인지 방향을 정하고 RayCast 시작점 설정 및 레이 탐색
            Vector2 rayOrigin = (directionY == -1) ? RayCastCorners.BottomLeft : RayCastCorners.TopLeft;
            rayOrigin +=Vector2.right *(_VerticalRaySpace *i + velocity.x);
            //RayCast(시작점,방향,길이,탐색 오브젝트)
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin,Vector2.up * directionY,rayLength,moveStats.GroundLayer);

            // 위(또는 아래)에 물체가 있을시 로직
            if(hit)
            {

                // 3. 충돌하기까지의 거리만큼 속도를 주고 거리가 줄어들때마다 rayLength 갱신한다.
                // 따라서 플레이어는 충돌하기 완전직전에 멈추게 설계가 됨.
                velocity.y = (hit.distance - CollisionPading) * directionY;
                rayLength = hit.distance;

                // 방향을 위쪽 아랫쪽으로 구분하여 기록
                if(directionY == -1)
                {
                    IsCollidingBelow = true;
                }

                else if(directionY == 1)
                {
                    IsCollidingAbove = true;
                }
            }

            /// +4.RayCast 시각화 구현
            if(moveStats.DebugShowWallHit) // 아랫방향 레이 시각화
            {
                float debugRayLength = rayLength;

                Vector2 debugRayOrigin = rayOrigin;
                bool didHit = Physics2D.Raycast(debugRayOrigin,Vector2.down * directionY,debugRayLength,moveStats.GroundLayer);

                Color rayColor = didHit? Color.blue:Color.red;
                Debug.DrawRay(debugRayOrigin,Vector2.down*directionY*debugRayLength,rayColor);//시작점, 길이, 색깔
            }

            if(moveStats.DebugShowHeadRays) // 윗방향 레이 시각화
            {
                float debugRayLength = moveStats.ExtraRayDebugDistance;
                Vector2 debugRayOrigin = RayCastCorners.TopLeft + Vector2.right * (_VerticalRaySpace *i);
                bool didHit = Physics2D.Raycast(debugRayOrigin,Vector2.up,debugRayLength,moveStats.GroundLayer);
                Color rayColor = didHit ? Color.green : Color.magenta;

                if(i == 0 || i == NumOfVerticalRays -1)
                {
                    rayColor = didHit ? Color.green : Color.magenta;
                }

                Debug.DrawRay(debugRayOrigin,Vector2.up *debugRayLength,rayColor);
            }
        }
    }

    #region collider 상태값 반환 함수
    // 람다 기능 사용 (isGrounded()는 IsCollidingBelow의 값을 가진다.)
    public bool isGrounded() => IsCollidingBelow; // 아래가 땅이면 true;

    public bool BumpedHead() => IsCollidingAbove; // 위가 천장이면 true;

    //벽인지 확인(매개변수 : 방향)
    public bool IsTouchingWall(bool isFacingRight) => (isFacingRight && IsCollidingRight) || (!isFacingRight && IsCollidingLeft);

    // 벽이 어디에있는지 반환
    public int GetWallDirection()
    {
        if(IsCollidingLeft) return -1;
        if(IsCollidingRight) return 1;

        return 0;
    }

    #endregion

}
