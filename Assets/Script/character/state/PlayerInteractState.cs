public class PlayerInteractState : IPlayerState
{
    private PlayerPresenter playerPresenter;

    private float interactDuration = 0.15f;
    private float interactTimer;

    public PlayerInteractState(PlayerPresenter playerPresenter)
    {
        this.playerPresenter = playerPresenter;
    }

    public void Enter()
    {
        interactTimer = interactDuration;

        playerPresenter.StopMove();
        playerPresenter.TryInteract();
    }

    public void Update()
    {
        interactTimer -= UnityEngine.Time.deltaTime;

        if (interactTimer <= 0f)
        {
            playerPresenter.ReturnToIdleOrMove();
        }
    }

    public void FixedUpdate()
    {
        playerPresenter.StopMove();
    }

    public void Exit()
    {
    }
}