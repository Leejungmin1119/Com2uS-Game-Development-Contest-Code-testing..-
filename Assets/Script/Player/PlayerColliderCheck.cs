using UnityEngine;

public class PlayerColliderCheck : MonoBehaviour
{
    //직접 참조

    PlayerColliderState playerColliderState;
    // 레이캐스트
    private RaycastHit2D groundHit;
    private RaycastHit2D bumpedHeat;
    private RaycastHit2D wallHit;
    public RaycastHit2D lastWallHit;

    //플레이어의 콜라이더 영역
    [SerializeField] private Collider2D Player_BodyCollider;
    [SerializeField] private Collider2D Player_footCollider;


    void Start()
    {
        playerColliderState = GetComponent<PlayerColliderState>();
    }


    ///********************************///
    /// Colliger Cheking **************///
    ///********************************///

    #region  땅감지
    public void IsGrounded(PlayerMoveStatsData moveStats)
    {
        //***** 땅 지형 체킹 *****//

        // 1.내 발밑에 있는 오브젝트가 땅인지  박스를 만들고 RayCast로 판별
        Vector2 boxCastOrigin = Player_footCollider.bounds.center;
        Vector2 boxCastSize = new Vector2(Player_footCollider.bounds.size.x * 0.9f, moveStats.GroundDetectionRayLenght);

        // 2.발밑 해당 콜리더의 값을 반환 및 적용
        // 매개변수(시작점,크기,영점,방향,길이,체크할레이어)
        groundHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.down, moveStats.GroundDetectionRayLenght, moveStats.GroundLayer);

        if (groundHit.collider != null) {playerColliderState.isGround = true;}
        else {playerColliderState.isGround = false;}

    }
    #endregion

    #region 천장감지
    public void IsHeadBump(PlayerMoveStatsData moveStats)
    {
        //***** 머리가 천장 체킹 *****//

        // 땅 지형과 동일(발사 방향, 시작 위치만 다름)

        Vector2 boxCastOrigin = new Vector2(Player_BodyCollider.bounds.center.x, Player_BodyCollider.bounds.max.y);
        Vector2 boxCastSize = new Vector2(Player_BodyCollider.bounds.size.x * 0.9f, moveStats.GroundDetectionRayLenght);

        // 매개변수(시작점,크기,영점,방향,길이,체크할레이어)
        bumpedHeat = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.up, moveStats.GroundDetectionRayLenght, moveStats.GroundLayer);

        if (bumpedHeat.collider != null) {playerColliderState.isBump = true;}
        else {playerColliderState.isBump = false;}

    }

    #endregion


    #region 벽감지

    /// <summary>
    /// 플레이어의 몸통을 기준으로 플레이어가 벽에 닿아 있는지 확인
    /// </summary>
    /// <param name="EndPoint">플레이어의 회전에 따라서 벽을 감지할 시작점을 나타내는 변수</param>
    /// <param name="lastWallHit">최종적으로 플레이어가 벽에 닿아있는지의 상태가 저장되어 있는 변수</param>
    /// <returns></returns>
    public void IsTouchWall(PlayerMoveStatsData moveStats, bool isFilp)
    {
        float EndPoint;

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
            playerColliderState.isTouchWall = true;
        }
        else {playerColliderState.isTouchWall = false;}
    }

    #endregion

    #region 감지 시각화


    ///
    /// 주석 코드
    /*
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

    */

    #endregion


    #region 벽 튕기기

    public float OutWallJump(PlayerMoveStatsData moveStats)
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

    #endregion

}
