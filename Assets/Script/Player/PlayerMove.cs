using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    //대쉬 값
    private bool isDashing;
    private bool isAirDashing;
    private float dashOnGroundTimer;
    private float dashTimer;
    private int numberofDashesUsed;
    private Vector2 dashDirection;
    static public bool isDashFastFalling;
    private float dashFasatFallTime;
    private float dashFasatFallReleaseSpeed;

    public float movevelocity;
    private Rigidbody2D Player_rigidBody;
    private SpriteRenderer Player_Sprite;
    void Awake()
    {
        Player_rigidBody = GetComponent<Rigidbody2D>();
        Player_Sprite = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        DashCheck();
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        Dash();
        if(Player.isGround)
        {
            dashOnGroundTimer -= Time.deltaTime;
        }

        if (Player.isGround == true)
        {
            Move(Player.instance.moveStats.GroundAcceleration, Player.instance.moveStats.GroundDcceleration, InputManager.Movement);
        }
        else
        {
            if (PlayerJump.WallJumpMoveStats)
            {
                Move(Player.instance.moveStats.WallJumpAcceleration, Player.instance.moveStats.WallJumpDcceleration, InputManager.Movement);
            }
            else
            {
                Move(Player.instance.moveStats.AirAcceleration, Player.instance.moveStats.AirDcceleration, InputManager.Movement);
            }

        }


        Player.instance.ApplyVelocity();

    }

    private void Move(float acceleration, float dcceleration, Vector2 moveInput)
    {


        //***** 이동 구현 (심화) *****//
        // !! 기본적으로 키들은 InputManager를 통하여 키 입력값 저장(유지보수 차원, 가독성 증가)
        if(!Player.isDashing)
        {
            if (moveInput != Vector2.zero)
            {
                //1. 달리기 키와 기본키 를 구분후 다르게 속도값 저장

                float targetvelocity = 0f;

                if (InputManager.RunIsHeld)
                {
                    targetvelocity = moveInput.x * Player.instance.moveStats.MaxRunSpeed;
                }
                else
                {
                    targetvelocity = moveInput.x * Player.instance.moveStats.MaxWalkSpeed;
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

    private void DashCheck()
    {

        //***** 대쉬 체크 *****//

        // 1. 대쉬키를 눌렸을때 땅에 있고 대쉬중이 아니였다면 실행
        if (InputManager.DashWasPressed)
        {
            //지상 대쉬
            if (Player.isGround && dashOnGroundTimer < 0 && !isDashing)
            {
                InitiateDash();
            }

            //공중 대쉬
            else if (!Player.isGround && !isDashing && numberofDashesUsed < Player.instance.moveStats.DashSedAllowed)
            {
                isAirDashing = true;
                InitiateDash();


                if (PlayerJump.wallJumpPostBufferTimer > 0f)
                {
                    // 2. 대쉬로 점프 횟수가 감소하는 것을 막기 위해 다시충전
                    PlayerJump.numberOfJumpsUsed--;
                    if (PlayerJump.numberOfJumpsUsed < 0f)
                    {
                        PlayerJump.numberOfJumpsUsed = 0;
                    }
                }
            }
        }
    }
    private void InitiateDash()
    {
        //***** 대쉬 기록*****//
        dashDirection = InputManager.Movement;

        Vector2 closestDirection = Vector2.zero;

        float minDistance = Vector2.Distance(dashDirection, Player.instance.moveStats.DashDirections[0]);
        for (int i = 0; i < Player.instance.moveStats.DashDirections.Length; i++)
        {
            if (dashDirection == Player.instance.moveStats.DashDirections[i])
            {
                closestDirection = dashDirection;
                break;
            }

            float distance = Vector2.Distance(dashDirection, Player.instance.moveStats.DashDirections[i]);

            bool isDiagonal = Mathf.Abs(Player.instance.moveStats.DashDirections[i].x) == 1 && Mathf.Abs(Player.instance.moveStats.DashDirections[i].y) == 1;

            if (isDiagonal)
            {
                distance -= Player.instance.moveStats.DashDiagonallyBias;
            }

            else if (distance < minDistance)
            {
                minDistance = distance;
                closestDirection = Player.instance.moveStats.DashDirections[i];
            }

        }

        // handle direction with No input
        if (closestDirection == Vector2.zero)
        {
            if (InputManager.Movement.x > 0)
            {
                closestDirection = Vector2.right;
            }
            else { closestDirection = Vector2.left; }

        }


        //적용
        dashDirection = closestDirection;
        numberofDashesUsed++;
        Player.isDashing = true;
        dashTimer = 0f;
        dashOnGroundTimer = Player.instance.moveStats.TimeBtwDashesOnGround;

        Player.instance.playerJump.ResetJumpValues();
        Player.instance.playerJump.ResetWallJumpValues();
        Player.instance.playerJump.ResetWallSlide();
    }

    private void Dash()
    {
        if (isDashing)
        {
            dashTimer += Time.fixedDeltaTime;
            if (dashTimer >= Player.instance.moveStats.DashTime)
            {
                if (Player.isGround)
                {
                    ResetDashes();
                }

                isAirDashing = false;
                isDashing = false;

                if (!PlayerJump.isJumping && !PlayerJump.isWallJumping)
                {
                    dashFasatFallTime = 0f;
                    dashFasatFallReleaseSpeed = PlayerJump.VerticalVelocity;

                    if (!Player.isGround)
                    {
                        isDashFastFalling = true;
                    }
                }

                return;
            }
            movevelocity = Player.instance.moveStats.DashSpeed * dashDirection.x;

            if (dashDirection.y != 0f || isAirDashing)
            {
                PlayerJump.VerticalVelocity = Player.instance.moveStats.DashSpeed * dashDirection.y;

            }
        }

        else if(isDashFastFalling)
        {
            if (PlayerJump.VerticalVelocity > 0f)
            {
                if (dashFasatFallTime < Player.instance.moveStats.DashTimeForUpwardsCancel)
                {
                    PlayerJump.VerticalVelocity = Mathf.Lerp(dashFasatFallReleaseSpeed, 0f, (dashFasatFallTime / Player.instance.moveStats.DashTimeForUpwardsCancel));
                }
                else if (dashFasatFallTime >= Player.instance.moveStats.DashTimeForUpwardsCancel)
                {
                    PlayerJump.VerticalVelocity += Player.instance.moveStats.Gravity * Player.instance.moveStats.DashGravityOnRelseasMultiplier * Time.fixedDeltaTime;
                }

                dashFasatFallTime += Time.fixedDeltaTime;
            }

            else
            {
                PlayerJump.VerticalVelocity += Player.instance.moveStats.DashGravityOnRelseasMultiplier * Time.fixedDeltaTime;
            }
        }
    }



}
