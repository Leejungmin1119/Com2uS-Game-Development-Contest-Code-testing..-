using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;

public class PlayerMovementManager : MonoBehaviour
{
    //싱글톤 인스턴스
    public static PlayerMovementManager instance;

    //직접 참조 필드
    public PlayerMove playermove;
    public PlayerJump playerJump;
    public PlayerMoveStatsData moveStats;

    public PlayerMoveController playerController;

    // 플레이어무브 기타 변수
    private Vector2 Velocity;
    //실제 적용시킬 플레이어 속도값

    public GameObject Player_Object;
    private Animator Player_Animator;



    void Start()
    {
        playermove = GetComponent<PlayerMove>();
        playerJump = GetComponent<PlayerJump>();
        playerController = GetComponent<PlayerMoveController>();
    }

    void Awake()
    {
        //******참조
        Player_Object = this.gameObject;
        Player_Animator = GetComponent<Animator>();

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
        //속도 종합
        ApplyVelocity();
        // 플레이어에게 실제 속도 적용
        playerController.Move(new Vector2(playermove.HorizonVelocity,playerJump.VerticalVelocity) * Time.fixedDeltaTime);
    }

    /// <summary>
    /// TODO player.instance가 아닌 매개변수로 받는 식으로 변경
    /// </summary>
    public void ApplyVelocity()
    {
        if (!playermove.isDashing)
        {
            playerJump.VerticalVelocity = Mathf.Clamp(playerJump.VerticalVelocity, -moveStats.MaxFallSpeed, 50f);
        }
        else
        {
            playerJump.VerticalVelocity = Mathf.Clamp(playerJump.VerticalVelocity, -50f, 50f);
        }

    }


}