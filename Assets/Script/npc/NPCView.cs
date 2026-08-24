using UnityEngine;

/// <summary>
/// NPC의 시각적인 표현 담당
/// 상호작용 표시, NPC 회전, 애니메이션 실행 등이 포함되어 있습니다.
/// </summary>
public class NPCView : MonoBehaviour
{
    [Header("상호작용 표시")]
    [SerializeField] private GameObject interactionMark;

    //[Header("애니메이터")]
    //[SerializeField] private Animator animator;

    public void ShowInteractionMark()
    {
        if (interactionMark != null)
        {
            interactionMark.SetActive(true);
        }
    }

    public void HideInteractionMark()
    {
        if (interactionMark != null)
        {
            interactionMark.SetActive(false);
        }
    }

    /// <summary>
    /// NPC가 플레이어를 바라보게 만드는 함수
    /// </summary>
    /// <param name="player"></param>
    public void LookAtPlayer(Transform player)
    {
        if (player == null)
        {
            return;
        }

        Vector3 direction = player.position - transform.position;

        direction.y = 0f;

        if (direction == Vector3.zero)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(direction);
    }

    /*
    /// <summary>
    /// NPC가 대화하는 애니메이션을 실행하는 함수
    /// </summary>
    public void PlayTalk()
    {
        if (animator != null)
        {
            animator.SetTrigger("Talk");
        }
    }

    public void PlayIdle()
    {
        if (animator != null)
        {
            animator.SetTrigger("Idle");
        }
    }
    */
}
