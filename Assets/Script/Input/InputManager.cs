using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static PlayerInput PlayerInput;

    public static Vector2 Movement;

    [Header("키 입력 확인")]
    public static bool JumpWasPressed;
    public static bool JumpIsHeld;
    public static bool JumpWasReleased;
    public static bool RunIsHeld;
    public static bool DashWasPressed;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction runAction;
    private InputAction DashAction;
    private void Awake()
    {


        // 키 할당

        //1. Input Action 의 할당되어 있는 기능을 각각 지정
        PlayerInput = GetComponent<PlayerInput>();

        moveAction = PlayerInput.actions["move"];
        jumpAction = PlayerInput.actions["Jump"];
        runAction = PlayerInput.actions["Run"];
        DashAction = PlayerInput.actions["Dash"];
    }

    private void Update()
    {
        // 2. 실제로 사용자가 커스텀으로 설정한 값에 따라서 각 기능(걷기,달리기,w,a,s,d등 다양한 기능을 쓸 수 있도록 지원)

        Movement = moveAction.ReadValue<Vector2>();//w,a,s,d

        // 점프 관련 키 입력 여부를 상세히 체크
        JumpWasPressed = jumpAction.WasPressedThisFrame(); // ?
        JumpWasReleased = jumpAction.WasReleasedThisFrame(); // 점프키를 입력중?
        JumpIsHeld = jumpAction.IsPressed(); // 점프키를 입력?

        // 달리기 관련 키 입력 여부 체크
        RunIsHeld = runAction.IsPressed();// 달리기 키 입력?

        DashWasPressed = DashAction.WasPressedThisFrame();

    }

}
