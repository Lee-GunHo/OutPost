using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeadState : IPlayerState
{
    private PlayerPresenter playerPresenter;

    private float deadTimer;
    private bool hasLoadedScene;

    private const float DeadDelay = 1f;

    public PlayerDeadState(PlayerPresenter playerPresenter)
    {
        this.playerPresenter = playerPresenter;
    }

    public void Enter()
    {
        Debug.Log("Player Dead State 진입");

        playerPresenter.StopMove();

        deadTimer = DeadDelay;
        hasLoadedScene = false;
    }

    public void Update()
    {
        if (hasLoadedScene)
        {
            return;
        }

        deadTimer -= Time.deltaTime;

        if (deadTimer <= 0f)
        {
            hasLoadedScene = true;
            playerPresenter.ReviveAtNexusFront();
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