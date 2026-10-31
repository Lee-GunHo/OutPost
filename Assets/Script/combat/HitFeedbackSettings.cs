using TMPro;
using UnityEngine;

/// <summary>
/// 타격감 수치. Resources/GameFeel/HitFeedbackSettings 에셋에서 읽고, 없으면 아래 기본값 사용.
/// 0으로 두면 해당 연출은 꺼짐.
/// </summary>
[CreateAssetMenu(fileName = "HitFeedbackSettings", menuName = "GameFeel/Hit Feedback Settings")]
public class HitFeedbackSettings : ScriptableObject
{
    [Header("히트스톱 (초, 게임이 잠깐 멈추는 시간)")]
    [Min(0f)] public float enemyHitStop = 0.04f;
    [Min(0f)] public float enemyKillHitStop = 0.08f;
    [Min(0f)] public float playerHurtHitStop = 0.06f;
    [Min(0f)] public float bossKillHitStop = 0.3f;

    [Header("화면 흔들림 (세기 = 월드 단위 최대 흔들림)")]
    [Min(0f)] public float enemyHitShake = 0.08f;
    [Min(0f)] public float enemyKillShake = 0.16f;
    [Min(0f)] public float playerHurtShake = 0.28f;
    [Min(0f)] public float blockBreakShake = 0.06f;
    [Min(0f)] public float bossKillShake = 0.6f;
    [Tooltip("흔들림이 잦아드는 속도. 클수록 짧고 탁 끊기는 느낌")]
    [Min(0.1f)] public float shakeDecay = 12f;
    [Min(1f)] public float shakeFrequency = 28f;

    [Header("피격 시 화면 가장자리 빨간 번쩍임 (GamePostProcess)")]
    public Color hurtFlashColor = new Color(0.75f, 0.05f, 0.05f);
    [Range(0f, 1f)] public float hurtFlashIntensity = 0.45f;
    [Min(0f)] public float hurtFlashDuration = 0.45f;

    [Header("데미지 숫자")]
    public bool showDamageNumbers = true;
    [Tooltip("비우면 TMP 기본 폰트")]
    public TMP_FontAsset damageFont;
    [Tooltip("벽 뒤에서도 보이게 하는 셰이더(TextMeshPro/Distance Field Overlay). 비우면 폰트 기본 셰이더")]
    public Shader damageNumberShader;
    [Min(0.1f)] public float damageFontSize = 5f;
    public Color enemyDamageColor = Color.white;
    public Color killDamageColor = new Color(1f, 0.85f, 0.25f);
    public Color playerDamageColor = new Color(1f, 0.3f, 0.3f);
    [Min(0.1f)] public float damageNumberLifetime = 0.7f;
    [Min(0f)] public float damageNumberRise = 1.1f;
    [Tooltip("맞은 대상 위치에서 이만큼 위에 숫자 표시")]
    public float damageNumberHeight = 1.8f;

    [Header("파편 (블록을 캐거나 몬스터가 죽을 때 튀는 조각)")]
    [Min(0)] public int blockHitDebris = 3;
    [Min(0)] public int blockBreakDebris = 10;
    [Tooltip("블록이 부서질 때 추가로 튀는 큰 덩어리 개수")]
    [Min(0)] public int blockBreakChunks = 4;
    [Min(0)] public int enemyKillDebris = 8;
    [Min(0.01f)] public float debrisSize = 0.13f;
    [Min(0f)] public float debrisSpeed = 4.5f;
    [Min(0.05f)] public float debrisLifetime = 0.7f;
    [Tooltip("파편이 튕기는 바닥 높이")]
    public float floorHeight = 0f;
}
