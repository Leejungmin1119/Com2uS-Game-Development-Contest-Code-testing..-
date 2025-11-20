using System;
using UnityEngine;

public class PlayerJump : MonoBehaviour
{

    //직접 참조
    [SerializeField] private PlayerMoveStatsData moveStats;
    [SerializeField] public PlayerMoveController playerController {get;private set;}
    [SerializeField] private PlayerMove playerMove;

    //입력값
    private bool _JumpPressed;
    private bool _JumpReleased;

    [Header("플레이어 옵션 체크")]
    public bool isJumping;
    public bool isFastFalling;
    public bool isFalling;
    public bool isPastApexThresHold;

    //점프
    public float VerticalVelocity;//점프 속도값
    private float fastFallTime;
    private float fastFallReleaseSpeed;
    public int numberOfJumpsUsed;

    //벽슬라이딩
    [SerializeField] private bool isWallSliding;
    [SerializeField] private bool isWallSliderFalling;

    //벽점프
    public bool WallJumpMoveStats;
    public bool isWallJumping;
    [SerializeField] private float WallJumpTime;
    [SerializeField] private bool isWallJumpFastFalling;
    [SerializeField] private bool isWallJumpFalling;
    private float WallJumpFastaFallTime;
    private float WallJumpFastaFallReaseSpeed;

    public float wallJumpPostBufferTimer;
    private float wallJumpApexPoint;
    private float timePastWallJumpApexThreshold;
    private bool isPastWallJumpApexThreHold;


    // 점프 정점
    private float apexPoint;
    private float TimePastApexThresHold;

    // 점프 타이머
    private float jumpBufferTimer;
    private bool jumpRelseasedDuringBuffer;

    //코요테
    private float coyoteTimer;


    public void Start()
    {
        moveStats = PlayerMovementManager.instance.moveStats;
        playerController = GetComponent<PlayerMoveController>();
        playerMove = GetComponent<PlayerMove>();
    }

    void Update()
    {
        // 입력
        if(InputManager.JumpWasPressed) _JumpPressed = true;
        if(InputManager.JumpWasReleased) _JumpReleased = true;

        JumpCountTimers(Time.deltaTime);// 시간체크

    }
    void FixedUpdate()
    {
        //스탯 초기화 함수
        LandCheck();
        JumpCheck();//점프 상태 확인
        WallSliderCheck();// 벽슬라이드 상태 확인
        WallJumpCheck();// 벽 점프 상태 확인

        //****** 점프
        Fall(Time.fixedDeltaTime); // 추락 함수
        Jump(Time.fixedDeltaTime); // 점프 실행 함수

        //****** 벽


        WallSlide(Time.fixedDeltaTime);// 벽타기 함수
        WallJump(Time.fixedDeltaTime); // 벽 점프 실행 함수




    }
    public void ResetJumpValues()
    {
        isJumping = false;
        isFalling = false;
        isFastFalling = false;
        fastFallTime = 0f;
        isPastApexThresHold = false;
    }
    public void ResetWallJumpValues()
    {
        isWallSliding = false;
        isWallSliderFalling = false;
        WallJumpMoveStats = false;
        isWallJumping = false;
        isWallJumpFastFalling = false;
        isWallJumpFalling = false;
        isPastApexThresHold = false;

        WallJumpFastaFallTime = 0f;
        WallJumpTime = 0f;


    }
    private void JumpCountTimers(float timestep)
    {

        // 시간 체크
        jumpBufferTimer -= timestep;

        if (!playerController.isGrounded())
        {
            coyoteTimer -= timestep;
        }
        else
        {
            coyoteTimer = moveStats.JumpBufferTime;
        }

        if(!ShouldApplyPostWallJumpBuffer())
        {
            wallJumpPostBufferTimer -= timestep;
        }
    }

    private void LandCheck()
    {
        if ((isJumping || isFalling || isWallJumpFalling || isWallJumping || isWallSliderFalling || isWallSliding || PlayerMove.isDashFastFalling) && playerController.isGrounded() && VerticalVelocity <= 0f)
        {
            ResetJumpValues();
            ResetWallJumpValues();
            playerMove.ResetDashes();
            playerMove.ResetDashValues();

            numberOfJumpsUsed = 0;

            VerticalVelocity = Physics2D.gravity.y;

            if (PlayerMove.isDashFastFalling )
            {
                playerMove.ResetDashValues();
                return;
            }

            playerMove.ResetDashValues();
        }
    }
    private void JumpCheck()
    {
        // ***** 점프 상태 확인 *****//
        // 점프키는 어느 상태에서든 누를수 있음! 따라서 여러 변수들이 합쳐서
        // 현제 플레이어의 상태를 다양하게 확인하고 정밀하게 속도조절하는 방식이다.


        if (_JumpPressed)// 1. 점프 버튼이 눌린 순간
        {

            //!!!!! 예외 : 벽점프 버퍼가 돌아가고 있을때 동시 실행 버그 해결 !!!!!//
            if (isWallSliderFalling && wallJumpPostBufferTimer >= 0f)
            {
                return;
            }
            //!!!!! 예외 : 벽슬라이드 중이거나 벽을 터치중일때 또한 중복 점프 방지를 통한 예외처리 //
            else if (isWallSliding || (playerController.IsTouchingWall(playerMove.isFilp) && !playerController.isGrounded()))
            {
                return;
            }
            jumpBufferTimer = moveStats.JumpBufferTime;
            jumpRelseasedDuringBuffer = false;
            //즉시 초기화
            _JumpPressed = false;
        }
        if (_JumpReleased)//2. 점프 버튼이 떼어진 순간의 함수
        {
            // 3. 낮은 점프 or 일반 점프 상황에 현제 상황 기록
            if (jumpBufferTimer > 0f)//
            {
                jumpRelseasedDuringBuffer = true;// 낮은 점프키 상태
            }
            if (isJumping && VerticalVelocity > 0f)
            {
                // 4. 일반 점프또한 정점위치 or 상승위치에 따라 다르게 체크

                if (isPastApexThresHold)
                {
                    isFalling = true;
                    isPastApexThresHold = false;
                    fastFallTime = moveStats.JumpCutTime;
                    VerticalVelocity = 0f;
                }
                else
                {
                    isFastFalling = true;
                    fastFallReleaseSpeed = VerticalVelocity;
                }

            }
            _JumpReleased =false;
        }

        //***** 플레이어 상태 결정 *****//

        // 앞선 로직에서 플레이어의 상태를 기록하고 그 기록들을 합쳐서 플레이어가 가져야 될 속도를 기록
        // 1. 낮은 점프 및 코요테 타임 점프
        // (점프 버퍼가 눌리기전에 누름 && 점프하기 전 && (땅 || 코요테 타임))

        if (jumpBufferTimer > 0f && !isJumping && (playerController.isGrounded() || coyoteTimer > 0f))
        {
            InitiateJump(1);

            if (jumpRelseasedDuringBuffer)//낮은 점프
            {
                isFastFalling = true;
                fastFallReleaseSpeed = VerticalVelocity;
            }
        }

        //2. 일반 이단점프 , 하강일때의 이단 점프 사용
        else if (jumpBufferTimer > 0f && (isJumping || isWallJumping || isWallSliderFalling || PlayerMove.isDashFastFalling
        || playerMove.isAirDashing ) && !playerController.IsTouchingWall(playerMove.isFilp)
        && numberOfJumpsUsed < moveStats.JumpsAllowed)
        {
            isFastFalling = false;
            InitiateJump(1);//점프 차감

            if(PlayerMove.isDashFastFalling)
            {
                PlayerMove.isDashFastFalling = false;
            }
        }
        else if (jumpBufferTimer > 0f && isFalling && !isWallSliderFalling && numberOfJumpsUsed < moveStats.JumpsAllowed - 1)
        {
            InitiateJump(2);// 점프 2회 차감
            isFastFalling = false;
        }
    }
    private void InitiateJump(int numberOfJump)
    {
        //***** 속도 기록 *****//
        // 1.점프중이 아니라고 표시되면 점프중으로 바꾸기(1단점프)
        if (!isJumping)
        {
            isJumping = true;
        }
        ResetWallJumpValues();

        // 2.점프한후 변수 초기화(무한 점프 금지)
        jumpBufferTimer = 0f;
        numberOfJumpsUsed += numberOfJump;
        //점프할 양의 속도 기록
        VerticalVelocity = moveStats.InitialJumpVelocity;

    }
    private void Jump(float timestep)
    {
        //***** 점프 중력적용 *****//

        // 1. 점프 중일때의 속도 적용
        if (isJumping)
        {

            // 2.머리에 닿으면 바로 하강
            if (playerController.BumpedHead())
            {
                isFastFalling = true;
            }

            //*********************상승 중 처리************************//

            // 점프로 상승중일때 실행(공중에 체공중일때도 이 함수 실행)
            if (VerticalVelocity >= 0f)
            {
                // 3. 정점에 도달했는지를 달성도로 나타내어(0~1) 만약 달성 되면 설정된값으로 유지
                apexPoint = Mathf.InverseLerp(moveStats.InitialJumpVelocity, 0f, VerticalVelocity);

                if (apexPoint > moveStats.ApexHold)
                {
                    if (!isPastApexThresHold)
                    {
                        isPastApexThresHold = true;
                        TimePastApexThresHold = 0f;
                    }

                    if (isPastApexThresHold)
                    {
                        TimePastApexThresHold += timestep;

                        if (TimePastApexThresHold < moveStats.ApexHangTime)
                        {
                            VerticalVelocity = 0f;
                        }
                        else
                        {
                            VerticalVelocity = -0.01f;
                        }

                    }
                }
                else if (!isFastFalling)
                {

                    // 4. 정점에 도달하기 전이라면 기본적인 중력적용 -> 속도 감소
                    VerticalVelocity += moveStats.Gravity * timestep;
                    // 예외처리
                    if (isPastApexThresHold)
                    {
                        isPastApexThresHold = false;
                    }
                }
            }
            //**************************하강 중 처리***************************//

            // 5. 하강 중일때 하지만 빠르게 떨어지기 전 설정
            else if (!isFastFalling)
            {
                // 기본 중력보다는 좀 더 빠르게 떨어지게 설정
                VerticalVelocity += moveStats.Gravity * moveStats.GravityOfFalling * timestep;
            }
            if (VerticalVelocity < 0f)
            {
                if (!isFalling)
                {
                    isFalling = true;
                }
            }

        }
        //6. 점프컷 설정 (빠른 하강, 점프키를 일찍 놓을시 실행) || 벽타기가 종료되었을 경우 실행
        if (isFastFalling)
        {
            if (fastFallTime >= moveStats.JumpCutTime)
            {
                // 추가 중력을 둬서 빠르게 하강
                VerticalVelocity += moveStats.Gravity * moveStats.GravityOfFalling * timestep;
            }
            else if (fastFallTime < moveStats.JumpCutTime)
            {
                //이전에 하강할 순간의 속도를 저장해서 중력을 적용하여 속도 조정
                VerticalVelocity = Mathf.Lerp(fastFallReleaseSpeed, 0f, (fastFallTime / moveStats.JumpCutTime));
            }

            fastFallTime += timestep;
        }

    }
    private void Fall(float timestep)
    {
        //***** 점프없을때의 하강 *****//

        // 1. 점프 하지 않고 그냥 하강할때 실행 or 벽점프 후 실행
        if (!playerController.isGrounded() && !isJumping && !isWallSliding && !isWallJumping && playerMove.isDashing && PlayerMove.isDashFastFalling)
        {
            if (!isFalling)
            {
                isFalling = true;
            }

            VerticalVelocity += moveStats.Gravity * timestep;
        }
    }

    //////////////////////////////////////////////////////////////
    //벽 슬라이딩 구현
    //////////////////////////////////////////////////////////////
    private void WallSliderCheck()
    {
        //***** 벽 체크 *****//
        // 1. 벽에 붙기위한 전제조건 확인
        if (!playerController.isGrounded() && playerController.IsTouchingWall(playerMove.isFilp) && !playerMove.isDashing)
        {
            if (VerticalVelocity < 0f && !isWallSliding)
            {

                ResetJumpValues();
                ResetWallJumpValues();
                playerMove.ResetDashValues();

                if (moveStats.ResetDashOnWallSlide)
                {
                    playerMove.ResetDashes();
                }

                isWallSliding = true; // 슬라이딩 가능
                isWallSliderFalling = false;//떨어지는 중인지 확인


                //2. 벽 점프가 가능하다는 것이 확인되면 점프 초기화
                if (moveStats.ResetJumpWallSlide)
                {
                    numberOfJumpsUsed = 0;
                }
            }
        }
        // 3. 벽타기를 하고 이제 떨어졌는지 확인(#!Player.isTouchWall 핵심)
        else if (isWallSliding && !playerController.isGrounded() && !playerController.IsTouchingWall(playerMove.isFilp) && !isWallSliderFalling)
        {
            isWallSliderFalling = true;
            //4. 점프 횟수 재 충전
            StopWallSlide();
        }
    }

    /// <summary>
    /// 슬라이드 -> 다시 낙하상태로 상태전환시키는 함수
    /// </summary>
    public void StopWallSlide()
    {
        //***** 점프 사용
        if(isWallSliding)
        {
            isWallSliding = false; // 낙하상태로 전환되었으니 슬라이드 상태 체크 헤제
        }
    }

    private void WallSlide(float timestep)
    {
        //***** 벽슬라이딩 ******//
        if (isWallSliding) // 벽에 붙어있는것이 확인되면 벽슬라이딩 실행
        {
            VerticalVelocity = Mathf.Lerp(VerticalVelocity, -moveStats.WallSlideSpeed, moveStats.WallSlideDecelerationSpeed * timestep);
        }
    }

    //////////////////////////////////////////////////////////////
    //벽 점프 구현
    //////////////////////////////////////////////////////////////

    // 벽점프가 가능한지 확인하는 함수
    private bool ShouldApplyPostWallJumpBuffer()
    {
        // 벽에 붙어있으면서 벽슬라이드 가능상태여야함.
        if (!playerController.isGrounded() && (playerController.IsTouchingWall(playerMove.isFilp) || isWallSliding))
        {
            return true;
        }
        else { return false; }
    }

    private void WallJumpCheck()
    {
        //***** 벽 점프 구현 *****//

        //!! 벽점프와 그냥 점프를 추가적인 플레이어 상태를 체크하여 구분하여야 함.

        // 1. 벽점프가 가능한지 체크 -> 가능하면 벽점프 버퍼 실행
        if (ShouldApplyPostWallJumpBuffer())
        {
            wallJumpPostBufferTimer = moveStats.WallJumpPostBufferTime;
        }

        // 2. 벽 점프키를 때었다면 정점인지 낮은 벽점프인지 확인하고 상태 할당.
        if (_JumpReleased && !isWallSliding && !playerController.IsTouchingWall(playerMove.isFilp) && isWallJumping)
        {
            if (VerticalVelocity > 0f)
            {
                //정점에서 뗀거면?
                if (isPastWallJumpApexThreHold)
                {
                    isPastWallJumpApexThreHold = false;
                    isWallJumpFastFalling = false;
                    WallJumpFastaFallTime = moveStats.DashTimeForUpwardsCancel;
                }//이른 점프키를 때었다면?
                else
                {
                    isWallJumpFastFalling = true;
                    WallJumpFastaFallReaseSpeed = VerticalVelocity;
                }
            }
            _JumpReleased = false;
        }

        // 3. 벽점프가 가능한 순간에 점프키를 누르면 벽점프의 속도값 저장
        if (_JumpPressed && wallJumpPostBufferTimer > 0f)
        {
            InitialWallJump();
            _JumpPressed = false;

        }
    }

    private void InitialWallJump()
    {
        //***** 벽점프 속도 값 기록 ******//

        // 1. 벽 점프중 일떄의 상태로 변경
        if (!isWallJumping)
        {
            isWallJumping = true;
            WallJumpMoveStats = true;
        }

        StopWallSlide();
        ResetJumpValues();

        WallJumpTime = 0f;

        //2. 속도 기록
        VerticalVelocity = moveStats.WallInitialJumpVelocity;

        // 3. 컨트롤러에서 벽의 위치 체크 후 반대방향으로 속도값 저장
        playerMove.HorizonVelocity = Mathf.Abs(moveStats.WallJumpDirection.x) * -playerController.GetWallDirection();


    }

    private void WallJump(float timestep)
    {

        //***** 벽 점프 중력 적용 *****//

        if (isWallJumping)
        {

            //1. ?
            WallJumpTime += timestep;

            if (WallJumpTime >= moveStats.TimeTillJumpApex)
            {
                WallJumpMoveStats = false;
            }

            // 2.머리 박으면 강제 하강
            if (playerController.BumpedHead())
            {
                isWallJumpFastFalling = true;
                WallJumpMoveStats = false;
            }


            // 3. 만약 정점에 도달하면 잠깐 정점에 머물고 하강 준비 아니면 계속 중력적용
            if (VerticalVelocity >= 0f)
            {
                wallJumpApexPoint = Mathf.InverseLerp(moveStats.WallJumpDirection.y, 0f, VerticalVelocity);
                if (wallJumpApexPoint > moveStats.ApexHold)
                {

                    if (!isPastApexThresHold)
                    {
                        isPastApexThresHold = true;
                        timePastWallJumpApexThreshold = 0f;
                    }
                    if (isPastApexThresHold)
                    {

                        timePastWallJumpApexThreshold += timestep;
                        if (timePastWallJumpApexThreshold < moveStats.ApexHangTime)
                        {
                            VerticalVelocity = 0f;
                        }
                        else
                        {
                            VerticalVelocity = -0.01f;
                        }

                    }
                }
                else if (!isWallJumpFastFalling)
                {
                    VerticalVelocity += moveStats.WallGravity * timestep;

                    if (isPastApexThresHold)
                    {
                        isPastApexThresHold = false;
                    }
                }
            }

            //4. 하강할때 하지만 낮은 벽점프가 아닐때의 속도 적용
            else if (!isWallJumpFastFalling)
            {
                VerticalVelocity += moveStats.WallGravity * timestep;
            }
            if (VerticalVelocity < 0f)
            {
                if (!isWallJumpFalling)
                {
                    isWallJumpFalling = true;
                }
            }
        }

        //***** 낮은 벽점프의 중력 적용*****//

        // 5. 낮은 벽점프였다면 빠르게 속도 0으로 만든 후 추가 중력을 주어서 하강
        if (isWallJumpFastFalling || isWallSliderFalling)
        {
            if (WallJumpFastaFallTime > moveStats.JumpCutTime)
            {
                VerticalVelocity += moveStats.WallGravity * moveStats.WallJumpGravity * timestep;
            }
            else if (WallJumpFastaFallTime < moveStats.JumpCutTime)
            {
                VerticalVelocity = Mathf.Lerp(WallJumpFastaFallReaseSpeed, 0f, (WallJumpFastaFallTime / moveStats.JumpCutTime));
            }
            WallJumpFastaFallTime += timestep;

        }
    }
}