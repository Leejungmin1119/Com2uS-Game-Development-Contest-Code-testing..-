using UnityEngine;

public class PlayerMoveController : MonoBehaviour
{
    public const float CollisionPading = 0.015f;
    public bool IsCollidingUp { get; private set; }
    public bool IsCollidingDown { get; private set; }
    public bool IsCollidingLeft { get; private set; }
    public bool IsCollidingRiget { get; private set; }


    class RayConers
    {
        public Vector2 RayCastLeft;
        public Vector2 RayCastRight;
        public Vector2 RayCastUp;
        public Vector2 RayCastDown;

    }

}
