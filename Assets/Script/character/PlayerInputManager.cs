using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputManager : MonoBehaviour
{
    private PlayerInputAction playerInputAction;

    public Vector2 MoveInput { get; private set; }

    public bool IsAttackPressed { get; private set; }
    public bool IsInteractPressed { get; private set; }

}
