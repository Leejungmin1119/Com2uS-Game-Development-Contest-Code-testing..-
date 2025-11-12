using System;
using UnityEditor.Callbacks;
using UnityEngine;

[CreateAssetMenu(menuName ="player MoveMent")]
public class PlayerMoveStats : ScriptableObject
{
    [Header("걷기 스탯")]
    [Range(0.25f, 100f)] public float MaxWalkSpeed;
    [Range(0.25f, 50f)] public float GroundAcceleration;
    [Range(0.25f, 50f)] public float GroundDcceleration;
    [Range(0.25f, 50f)] public float AirAcceleration;
    [Range(0.25f, 50f)] public float AirDcceleration;

    [Header("벽타기")]
    [Range(0.25f, 50f)] public float WallJumpAcceleration = 5f;
    [Range(0.25f, 50f)] public float WallJumpDcceleration = 5f;


    [Header("뛰기 스탯")]
    [Range(1f, 200f)] public float MaxRunSpeed;

    [Header("오브젝트 체크 범위 관련 변수")]

    [Header("RayCast 길이")]
    public LayerMask GroundLayer;
    public float GroundDetectionRayLenght = 0.02f;
    public float HeadDetectionRayLength = 0.02f;
    [Range(0f, 1f)] public float HeadWidth = 0.75f;

    [Header("벽 RayCast ")]

    public float WallDectectionRayLength = 0.125f;
    [Range(0.01f, 1f)] public float WallDectectionRayHeight = 0.9f; // 탐지 길이


    [Header("점프")]
    public float JumpHeight = 6.5f;//점프 높이
    [Range(1f, 1.1f)] public float JumpHeightCompensationFactor = 1.054f; // 점프 보정 , 미세한 점프차이를 구현하고 오차 보정
    [Range(0.01f, 5f)] public float GravityOfFalling = 2f; // 점프 컷 가중치
    public float MaxFallSpeed = 26f;

    [Range(1, 5)] public int JumpsAllowed = 2;

    [Header("점프컷 시간")]
    [Range(0f, 1f)] public float JumpCutTime = 0.027f;

    [Header("점프 최고점")]
    public float TimeTillJumpApex = 0.5f;//정점까지의 시간
    [Range(0.5f, 1f)] public float ApexHold = 0.97f;
    [Range(0.01f, 1f)] public float ApexHangTime = 0.97f;//정점에서 머무르는 시간

    [Header("점프 버퍼(Jump Buffer)")]
    [Range(0f, 1f)] public float JumpBufferTime = 0.125f;//점프 가능 시간(이시간이 다끝나면 점프 불가)

    [Header("공중 체공시간(jump Coyote Time)")]
    [Range(0f, 1f)] public float JmpCoyoteTime = 0.1f;

    [Header("벽 점프 라셋")]
    public bool ResetJumpWallSlide = true;

    [Header("벽 슬라이드")]
    [Min(0.01f)] public float WallSlideSpeed = 5f;
    [Range(0.25f, 50f)] public float WallSlideDecelerationSpeed = 50f;

    [Header("벽 점프")]
    public Vector2 WallJumpDirection = new Vector2(-20f, 6.5f);
    [Range(0f, 1f)] public float WallJumpPostBufferTime = 0.125f;
    [Range(0.01f, 5f)] public float WallJumpGravity = 1f;

    [Header("대쉬")]
    [Range(0f, 1f)] public float DashTime = 0.11f;
    [Range(1f, 200f)] public float DashSpeed = 40f;
    [Range(0f, 1f)] public float TimeBtwDashesOnGround = 0.225f;
    public bool ResetDashOnWallSlide = true;
    [Range(0, 5)] public int DashSedAllowed = 2;
    [Range(0f, 0.5f)] public float DashDiagonallyBias = 0.4f;

    [Header("Dash Cancel Time")]
    [Range(0.01f, 5f)] public float DashGravityOnRelseasMultiplier = 1f;
    [Range(0.02f, 0.3f)] public float DashTimeForUpwardsCancel = 0.027f;



    [Header("Debug")]
    public bool DebugShowIsGroundedBox;
    public bool DebugShowHeadBumpBox;

    [Header("시각화 Tool")]
    public bool ShowWalkJumpArc = false;
    public bool ShowRunJumpArc = false;
    public bool StopOnCollision = true;
    public bool DrawRight = true;
    [Range(5, 100)] public int ArcResolution = 20;
    [Range(0, 500)] public int VisualizationSteps = 90;

    public readonly Vector2[] DashDirections = new Vector2[]
    {
        new Vector2(0,0), // 없음
        new Vector2(1,0), // 오른쪽
        new Vector2(1,1).normalized, // 대각 위
        new Vector2(0,1), // 위
        new Vector2(-1,1).normalized, //대각 왼쪽
        new Vector2(-1,0), // 왼쪽
        new Vector2(-1,-1).normalized, // 대각 왼쪽아래
        new Vector2(0,-1), // 아래
        new Vector2(1,-1).normalized // 대각 오른쪽 아래
    };


    //점프 중력
    public float Gravity { get; private set; }
    public float InitialJumpVelocity { get; private set; }
    public float AdjustedJumpHeight { get; private set; }

    // 벽 중력
    public float WallGravity { get; private set; }
    public float WallInitialJumpVelocity { get; private set; }
    public float WallAdjustedJumpHeight { get; private set; }

    //***** 중력 구현 *****// ()
    private void OnValidate()
    {
        CalculateValues();

    }
    private void OnEnable()
    {
        CalculateValues();
    }

    private void CalculateValues()
    {
        // -(2 = 2 * 높이) / 시간^2 = 중력
        // 자세한것은 https://www.youtube.com/watch?v=hG9SzQxaCm8//을통해서 확인

        // 각 레벨에 따라 점프 가중치를 달리하고 그걸 적용
        // 높이가 높게 설정되면 중력 증가, 정점시간을 크게 설정하면 중력 감소
        AdjustedJumpHeight = JumpHeight * JumpHeightCompensationFactor;
        Gravity = -(2f * AdjustedJumpHeight) / Mathf.Pow(TimeTillJumpApex, 2f);
        InitialJumpVelocity = Mathf.Abs(Gravity) * TimeTillJumpApex;


        // 벽에 대한 중력 구현
        WallAdjustedJumpHeight = WallJumpDirection.y* JumpHeightCompensationFactor;
        WallGravity = -(2f * WallAdjustedJumpHeight) / Mathf.Pow(TimeTillJumpApex, 2f);
        WallInitialJumpVelocity = Mathf.Abs(WallGravity) * TimeTillJumpApex;


    }

}
