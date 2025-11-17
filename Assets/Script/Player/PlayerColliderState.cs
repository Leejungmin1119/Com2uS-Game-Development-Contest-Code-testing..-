using UnityEngine;

public class PlayerColliderState : MonoBehaviour
{
    //직접 참조
    PlayerMoveStatsData moveStats;
    PlayerColliderCheck playerColliderCheck;
    PlayerMovementManager playerMovementManager;

    //콜라이더 상태 변수
    [SerializeField] public bool isGround;
    [SerializeField] public bool isBump;
    [SerializeField] public bool isTouchWall;

    void Start()
    {
        moveStats = PlayerMovementManager.instance.moveStats;
        playerColliderCheck = GetComponent<PlayerColliderCheck>();
        playerMovementManager = GetComponent<PlayerMovementManager>();
    }

    private void FixedUpdate()
    {
        CollisionChecks();
    }

    /// <summary>
    /// 콜라이더 접촉 상태를 관리하는 함수
    /// </summary>
    private void CollisionChecks()
    {
        // 여러 부분의 지형이 어떤 것인지 판별
        // 매개변수 : 시작점,크기,영점,방향,길이,체크할레이어
        playerColliderCheck.IsGrounded(moveStats);
        playerColliderCheck.IsHeadBump(moveStats);
        playerColliderCheck.IsTouchWall(moveStats,playerMovementManager.isFilp);
    }
}
