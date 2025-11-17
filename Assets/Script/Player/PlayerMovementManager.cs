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

    // 플레이어무브 기타 변수
    public bool isFilp;
    public GameObject Player_Object;

    private Animator Player_Animator;
    public Rigidbody2D Player_rigidBody;
    private SpriteRenderer Player_Sprite;

    void Start()
    {
        playermove = GetComponent<PlayerMove>();
        playerJump = GetComponent<PlayerJump>();
    }
    void Awake()
    {
        //******참조
        Player_Object = this.gameObject;
        Player_Animator = GetComponent<Animator>();

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

        //최종 속도 적용
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

    /// <summary>
    /// TODO player.instance가 아닌 매개변수로 받는 식으로 변경
    /// </summary>
    public void ApplyVelocity()
    {
        if (!playermove.isDashing)
        {
            PlayerJump.VerticalVelocity = Mathf.Clamp(PlayerJump.VerticalVelocity, -moveStats.MaxFallSpeed, 50f);
        }
        else
        {
            PlayerJump.VerticalVelocity = Mathf.Clamp(PlayerJump.VerticalVelocity, -50f, 50f);
        }

        Player_rigidBody.linearVelocity = new Vector2(instance.playermove.movevelocity, PlayerJump.VerticalVelocity);
    }


}