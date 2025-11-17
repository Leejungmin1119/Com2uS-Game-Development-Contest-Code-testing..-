using UnityEngine;

public class PlayerMove : MonoBehaviour
{

    // 직접 참조 필드 선언
    PlayerMovementManager movementmanager;
    PlayerMoveStatsData moveStats;
    PlayerColliderState playerState;
    PlayerJump playerJump;

    //대쉬 상태 변수
    public bool isDashing;
    public bool isAirDashing;

    //대쉬 타이머
    private float dashOnGroundTimer;
    private float dashTimer;

    //대쉬 사용횟수
    private int numberofDashesUsed;

    //방향?
    private Vector2 dashDirection;

    //대쉬 하강 함수
    [SerializeField] static public bool isDashFastFalling;
    [SerializeField] private float dashFasatFallTime;
    [SerializeField] private float dashFasatFallReleaseSpeed;
    public float movevelocity;

    void Start()
    {
        moveStats = PlayerMovementManager.instance.moveStats;
        movementmanager = GetComponent<PlayerMovementManager>();
        playerState = GetComponent<PlayerColliderState>();
        playerJump = GetComponent<PlayerJump>();
    }
    void Update()
    {
        DashCheck();
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        Dash();

        if(playerState.isGround)
        {
            dashOnGroundTimer -= Time.deltaTime;
        }

        if (playerState.isGround == true)
        {
            Move(moveStats.GroundAcceleration, moveStats.GroundDcceleration, InputManager.Movement);
        }
        else
        {
            if (playerJump.WallJumpMoveStats)
            {
                Move(moveStats.WallJumpAcceleration,moveStats.WallJumpDcceleration, InputManager.Movement);
            }
            else
            {
                Move(moveStats.AirAcceleration,moveStats.AirDcceleration, InputManager.Movement);
            }

        }

        movementmanager.ApplyVelocity();

    }

    private void Move(float acceleration, float dcceleration, Vector2 moveInput)
    {


        //***** 이동 구현 (심화) *****//
        // !! 기본적으로 키들은 InputManager를 통하여 키 입력값 저장(유지보수 차원, 가독성 증가)
        if(!isDashing)
        {
            if (moveInput != Vector2.zero)
            {
                //1. 달리기 키와 기본키 를 구분후 다르게 속도값 저장

                float targetvelocity = 0f;

                if (InputManager.RunIsHeld)
                {
                    targetvelocity = moveInput.x * moveStats.MaxRunSpeed;
                }
                else
                {
                    targetvelocity = moveInput.x * moveStats.MaxWalkSpeed;
                }
                // 2. 저장한 속도를 바로 올리지 않고 가속값을 곱하여 적용
                movevelocity = Mathf.Lerp(movevelocity, targetvelocity, acceleration * Time.fixedDeltaTime); // 부드럽게 이동을 적용하기 위해서 Lerp 활용
            }
            else if (moveInput == Vector2.zero)//3. 이동키 입력 x 일시 감속 적용
            {
                movevelocity = Mathf.Lerp(movevelocity, 0f, dcceleration * Time.fixedDeltaTime);
            }
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
        if (InputManager.DashWasPressed)
        {
            //지상 대쉬
            if (playerState.isGround && dashOnGroundTimer < 0 && !isDashing) // 대쉬를 사용하기 전인지도 체크
            {
                InitiateDash();
            }

            //공중 대쉬
            else if (!playerState.isGround && !isDashing && numberofDashesUsed < moveStats.DashSedAllowed)
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
        }
    }

    /// <summary>
    /// 플레이어가 입력한 방향을 찾고 상태 변경
    /// </summary>
    private void InitiateDash()
    {
        //***** 대쉬 기록*****//
        dashDirection = InputManager.Movement;// 플레이어가 누른 방향

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
            if (InputManager.Movement.x > 0)
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

        playerJump.ResetJumpValues();
        playerJump.ResetWallJumpValues();
        playerJump.ResetWallSlide();
    }

    /// <summary>
    /// 이때까지의 플레이어의 상태를 통해서 실제 대쉬 방향 및 속도를 갱신하는 함수
    /// 마지막으로 갱신후 벡터값을 최종 apply함수에 전달.
    /// </summary>
    private void Dash()
    {
        if (isDashing)
        {

            //1. 대쉬 실행 타이머가 초과하면(대쉬가 끝나면) 대쉬 상태값 초기화
            dashTimer += Time.fixedDeltaTime;
            if (dashTimer >= moveStats.DashTime)
            {
                if (playerState.isGround)
                {
                    ResetDashes();
                }

                isAirDashing = false;
                isDashing = false;

                //2. 만약 땅에 대쉬하고 나서 공중에 있는 상태라면 대쉬 하강상태 활성화
                if (!playerJump.isJumping && !playerJump.isWallJumping)
                {
                    dashFasatFallTime = 0f;
                    dashFasatFallReleaseSpeed = PlayerJump.VerticalVelocity;

                    if (!playerState.isGround)
                    {
                        isDashFastFalling = true;
                    }
                }

                return;
            }
            movevelocity = moveStats.DashSpeed * dashDirection.x;

            if (dashDirection.y != 0f || isAirDashing)
            {
                PlayerJump.VerticalVelocity = moveStats.DashSpeed * dashDirection.y;

            }
        }

        //3. 대쉬 하강상태면 지면까지 중력 적용
        else if(isDashFastFalling)
        {
            if (PlayerJump.VerticalVelocity > 0f)
            {
                //DashTimeForUpwardsCancel되기전까지 속도 0으로 보간 조정
                if (dashFasatFallTime < moveStats.DashTimeForUpwardsCancel)
                {
                    PlayerJump.VerticalVelocity = Mathf.Lerp(dashFasatFallReleaseSpeed, 0f, (dashFasatFallTime / moveStats.DashTimeForUpwardsCancel));
                }
                //보간 이후 빠른 중력 적용
                else if (dashFasatFallTime >= moveStats.DashTimeForUpwardsCancel)
                {
                    PlayerJump.VerticalVelocity += moveStats.Gravity * moveStats.DashGravityOnRelseasMultiplier * Time.fixedDeltaTime;
                }

                dashFasatFallTime += Time.fixedDeltaTime;
            }

            else
            {
                PlayerJump.VerticalVelocity += moveStats.Gravity * moveStats.DashGravityOnRelseasMultiplier * Time.fixedDeltaTime;
            }
        }
    }

}