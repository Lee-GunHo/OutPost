using UnityEngine;

public class PlayerMineState : IPlayerState
{
    private PlayerPresenter playerPresenter;

    private bool hasMined;

    public PlayerMineState(PlayerPresenter playerPresenter)
    {
        this.playerPresenter = playerPresenter;
    }

    public void Enter()
    {
        hasMined = false;

        Debug.Log("Mine State 진입");
    }

    public void Update()
    {
        if (!hasMined)
        {
            //playerPresenter.TryBreakWall();
            hasMined = true;
        }

        playerPresenter.ReturnToIdleOrMove();
    }

    public void FixedUpdate()
    {
    }

    public void Exit()
    {
    }
}