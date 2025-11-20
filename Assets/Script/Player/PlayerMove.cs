using UnityEngine;

public class PlayerMove : MonoBehaviour
{

    // 직접 참조 필드 선언
    [SerializeField] PlayerMovementManager movementmanager;
    [SerializeField] PlayerMoveStatsData moveStats;
    [SerializeField] PlayerMoveController playerController;
    [SerializeField] PlayerJump playerJump;


    // 속도
    public float HorizonVelocity;
    //입력값
    private Vector2 _MoveInput;
    private bool _RunHeld;
    private bool _DashPressed;
    //대쉬 상태 변수
    public bool isDashing;
    public bool isAirDashing;

    //대쉬 타이머
    private float dashOnGroundTimer;
    private float dashTimer;

    //대쉬 사용횟수
    private int numberofDashesUsed;

    //방향
    private Vector2 dashDirection;
    public bool isFilp {get;private set;}
    //대쉬 하강 함수
    [SerializeField] static public bool isDashFastFalling;
    [SerializeField] private float dashFasatFallTime;
    [SerializeField] private float dashFasatFallReleaseSpeed;


    private SpriteRenderer Player_Sprite;
    void Start()
    {
        moveStats = PlayerMovementManager.instance.moveStats;
        movementmanager = GetComponent<PlayerMovementManager>();
        playerController = GetComponent<PlayerMoveController>();
        playerJump = GetComponent<PlayerJump>();

        Player_Sprite = GetComponent<SpriteRenderer>();
    }
    void Update()
    {
        //입력값 전달
        _MoveInput = InputManager.Movement;
        _RunHeld = InputManager.RunIsHeld;
        if(InputManager.DashWasPressed) _DashPressed = true;

        DashCheck();

        //player 의 input 값 리셋

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //대쉬
        Dash(Time.fixedDeltaTime);

        if(playerController.isGrounded())
        {
            dashOnGroundTimer -= Time.deltaTime;
        }

        //이동
        HandleHorizontalMovement(Time.fixedDeltaTime);


    }

    private void Turn(Vector2 moveInput)
    {
        //좌우 방향 전환(flip이용)
        if (moveInput.x != 0)
        {
            if(moveInput.x >0)
            {
                isFilp = true;
                Player_Sprite.flipX = false;
            }
            else if(moveInput.x < 0)
            {
                isFilp = false;
                Player_Sprite.flipX = true;
            }
        }

    }

    private void HandleHorizontalMovement(float TimeStep)
    {

        //***** 이동 구현 *****//
        // !! 기본적으로 키들은 InputManager를 통하여 키 입력값 저장(유지보수 차원, 가독성 증가)
        // 추가적으로 입력값들을 실시간으로 전달받아서
        if(!isDashing)
        {
            Turn(_MoveInput);//회전 체크
            //속도 초기값
            float targetVelocityX = 0f;

            // 1. 최소 입력시간을 설정하여 입력 감지확인
            if(Mathf.Abs(_MoveInput.x) >= moveStats.MoveThreshold) // 최소 입력시간 체크
            {
                float moveDirection = Mathf.Sign(_MoveInput.x);
                targetVelocityX = _RunHeld ? moveDirection * moveStats.MaxRunSpeed : moveDirection * moveStats.MaxWalkSpeed;
            }

            // 2. 플레이어가 지금 땅인지 하늘인지에 따라서 가속,감속 적용
            float acceleration = playerController.isGrounded() ? moveStats.GroundAcceleration : moveStats.AirAcceleration;
            float dcceleration = playerController.isGrounded() ? moveStats.GroundDcceleration : moveStats.AirDcceleration;

            // 벽점프 확인 후 맞으면 그에 맞는 가속도 적용
            if(playerJump.WallJumpMoveStats)
            {
                acceleration = moveStats.WallJumpAcceleration;
                dcceleration = moveStats.WallJumpDcceleration;
            }

            // 2. 속도 값 기록
            if(Mathf.Abs(_MoveInput.x) >= moveStats.MoveThreshold)
            {
                HorizonVelocity = Mathf.Lerp(HorizonVelocity,targetVelocityX,acceleration * TimeStep);
            }
            else
            {
                HorizonVelocity = Mathf.Lerp(HorizonVelocity,targetVelocityX,dcceleration * TimeStep);
            }

            // 실제 플레이어 매니저에 속도값 조정

        }

    }
    public void ResetDashValues()
    {
        isDashFastFalling = false;
        dashOnGroundTimer = -0.01f;

    }
    public void ResetDashes()
    {
        numberofDashesUsed = 0;
    }

    /// <summary>
    /// 지금 현제 플레이어가 어느공간에서 대쉬를 한지 파악, 대쉬 상태 갱신
    /// </summary>
    private void DashCheck()
    {

        //***** 대쉬 체크 *****//
        // 1. 대쉬키를 눌렸는지 확인
        if (_DashPressed)
        {

            //지상 대쉬
            if (playerController.isGrounded() && dashOnGroundTimer < 0 && !isDashing) // 대쉬를 사용하기 전인지도 체크
            {
                InitiateDash();
            }

            //공중 대쉬
            else if (!playerController.isGrounded() && !isDashing && numberofDashesUsed < moveStats.DashSedAllowed)
            {
                isAirDashing = true;
                InitiateDash();


                if (playerJump.wallJumpPostBufferTimer > 0f)
                {
                    // 2. 대쉬로 점프 횟수가 감소하는 것을 막기 위해 다시충전
                    playerJump.numberOfJumpsUsed--;
                    if (playerJump.numberOfJumpsUsed < 0f)
                    {
                        playerJump.numberOfJumpsUsed = 0;
                    }
                }
            }

            _DashPressed = false;
        }
    }

    /// <summary>
    /// 플레이어가 입력한 방향을 찾고 상태 변경
    /// </summary>
    private void InitiateDash()
    {
        //***** 대쉬 기록*****//
        dashDirection = _MoveInput;// 플레이어가 누른 방향

        Vector2 closestDirection = Vector2.zero;

        //1. 플레이어의 대쉬 거리를 얻고 그의 맞는 방향을 찾아서 저장

        //거리값 얻기
        float minDistance = Vector2.Distance(dashDirection, moveStats.DashDirections[0]);

        //방향얻기
        for (int i = 0; i < moveStats.DashDirections.Length; i++)
        {
            // 2. 지정된 대쉬 방향키를 찾으면 종료
            if (dashDirection == moveStats.DashDirections[i])
            {
                closestDirection = dashDirection;
                break;
            }
            //3. 만약 찾지 못했다면 가장 가까운 값을 찾고 대각선이면 보정치 주기
            float distance = Vector2.Distance(dashDirection, moveStats.DashDirections[i]);

            bool isDiagonal = Mathf.Abs(moveStats.DashDirections[i].x) == 1 && Mathf.Abs(moveStats.DashDirections[i].y) == 1;

            if (isDiagonal)
            {
                distance -= moveStats.DashDiagonallyBias;//대각선 보정
            }

            //거리가 더 짧으면 갱신
            else if (distance < minDistance)
            {
                minDistance = distance;
                closestDirection = moveStats.DashDirections[i];
            }

        }

        // 아무방향도 주지 않을때의 뱡향 값
        if (closestDirection == Vector2.zero)
        {
            if (!Player_Sprite.flipX)
            {
                closestDirection = Vector2.right;
            }
            else { closestDirection = Vector2.left; }
        }


        //4. 최종 플레이어 대쉬 상태 갱신 및 그에 맞는 속도값 기록
        dashDirection = closestDirection;
        numberofDashesUsed++;
        isDashing = true;
        dashTimer = 0f;
        dashOnGroundTimer = moveStats.TimeBtwDashesOnGround;

        //대쉬 외에 다른 상태값들은 초기화
        playerJump.ResetJumpValues();
        playerJump.ResetWallJumpValues();
        playerJump.StopWallSlide();
    }

    /// <summary>
    /// 이때까지의 플레이어의 상태를 통해서 실제 대쉬 방향 및 속도를 갱신하는 함수
    /// 마지막으로 갱신후 벡터값을 최종 apply함수에 전달.
    /// </summary>
    private void Dash(float timestep)
    {
        if (isDashing)
        {

            //1. 대쉬 실행 타이머가 초과하면(대쉬가 끝나면) 대쉬 상태값 초기화
            dashTimer += timestep;
            if (dashTimer >= moveStats.DashTime)
            {
                if (playerController.isGrounded())
                {
                    ResetDashes();
                }

                isAirDashing = false;
                isDashing = false;

                //2. 만약 땅에 대쉬하고 나서 공중에 있는 상태라면 대쉬 하강상태 활성화
                if (!playerJump.isJumping && !playerJump.isWallJumping)
                {
                    dashFasatFallTime = 0f;
                    dashFasatFallReleaseSpeed = playerJump.VerticalVelocity;

                    if (!playerController.isGrounded())
                    {
                        isDashFastFalling = true;
                    }
                }

                return;
            }
            HorizonVelocity = moveStats.DashSpeed * dashDirection.x;

            //지금 대쉬가 실행되고 있을때 공중에 있다면 플레이어의 y축도 증가 하거나 고정(수평대쉬 ,공중기준)
            if (dashDirection.y != 0f || isAirDashing)
            {
                playerJump.VerticalVelocity = moveStats.DashSpeed * dashDirection.y;
            }
            else if(!playerJump.isJumping && dashDirection.y == 0f)
            {
                playerJump.VerticalVelocity = -0.001f;
            }
        }

        //3. 대쉬 하강상태면 지면까지 중력 적용
        else if(isDashFastFalling)
        {
            if (playerJump.VerticalVelocity > 0f)
            {
                //DashTimeForUpwardsCancel되기전까지 속도 0으로 보간 조정
                if (dashFasatFallTime < moveStats.DashTimeForUpwardsCancel)
                {
                    playerJump.VerticalVelocity = Mathf.Lerp(dashFasatFallReleaseSpeed, 0f, (dashFasatFallTime / moveStats.DashTimeForUpwardsCancel));
                }
                //보간 후 빠른 중력 적용
                else if (dashFasatFallTime >= moveStats.DashTimeForUpwardsCancel)
                {
                    playerJump.VerticalVelocity += moveStats.Gravity * moveStats.DashGravityOnRelseasMultiplier * timestep;
                }

                dashFasatFallTime += timestep;
            }

            //속도가 0이하일때 빠른 중력 적용
            else
            {
                playerJump.VerticalVelocity += moveStats.Gravity * moveStats.DashGravityOnRelseasMultiplier * timestep;
            }
        }
    }
}