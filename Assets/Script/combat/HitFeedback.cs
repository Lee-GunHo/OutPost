using UnityEngine;

/// <summary>
/// 타격감(효과음 + 히트스톱 + 화면 흔들림 + 데미지 숫자 + 파편)을 한 곳에서 처리하는 진입점.
/// 전투/채굴 코드는 "무슨 일이 있었는지"만 알려주고, 연출 수치는 HitFeedbackSettings에서 조절.
/// </summary>
public static class HitFeedback
{
    private const string SettingsPath = "GameFeel/HitFeedbackSettings";

    private static HitFeedbackSettings settings;
    private static int lastKillFrame = -1;

    public static HitFeedbackSettings Settings
    {
        get
        {
            if (settings == null)
            {
                settings = Resources.Load<HitFeedbackSettings>(SettingsPath);

                if (settings == null)
                    settings = ScriptableObject.CreateInstance<HitFeedbackSettings>();
            }

            return settings;
        }
    }

    /// <summary>일반 몬스터가 맞음(플레이어/동료 공격 모두). 소리 + 숫자, 죽으면 파편</summary>
    public static void EnemyDamaged(Vector3 position, int damage, bool killed, Renderer debrisSource = null)
    {
        SfxPlayer.PlayAt(killed ? SfxId.EnemyDie : SfxId.HitEnemy, position);
        ShowDamage(position, damage, killed ? Settings.killDamageColor : Settings.enemyDamageColor, killed);

        if (killed)
        {
            lastKillFrame = Time.frameCount;
            GameFeelRunner.SpawnDebris(position + Vector3.up * 0.6f, Settings.enemyKillDebris, debrisSource, 1.1f);
        }
    }

    public static void BossDamaged(Vector3 position, int damage, bool killed, Renderer debrisSource = null)
    {
        SfxPlayer.PlayAt(killed ? SfxId.BossDie : SfxId.BossHit, position);
        ShowDamage(position + Vector3.up, damage, killed ? Settings.killDamageColor : Settings.enemyDamageColor, killed);

        if (!killed)
            return;

        lastKillFrame = Time.frameCount;
        GameFeelRunner.HitStop(Settings.bossKillHitStop);
        GameFeelRunner.Shake(Settings.bossKillShake);
        GameFeelRunner.SpawnDebris(position + Vector3.up, Settings.enemyKillDebris * 3, debrisSource, 1.6f);
    }

    /// <summary>
    /// 플레이어가 직접 적을 때린 직후 호출. 히트스톱/흔들림은 플레이어 공격에만 줘서
    /// 동료가 계속 때릴 때 화면이 멈칫거리지 않게 함
    /// </summary>
    public static void PlayerStruckEnemy()
    {
        bool killed = lastKillFrame == Time.frameCount;
        GameFeelRunner.HitStop(killed ? Settings.enemyKillHitStop : Settings.enemyHitStop);
        GameFeelRunner.Shake(killed ? Settings.enemyKillShake : Settings.enemyHitShake);
    }

    public static void PlayerSwing(Vector3 position)
    {
        SfxPlayer.PlayAt(SfxId.Swing, position);
    }

    public static void PlayerHurt(Vector3 position, int damage, bool died)
    {
        SfxPlayer.PlayAt(died ? SfxId.PlayerDeath : SfxId.PlayerHurt, position);
        ShowDamage(position, damage, Settings.playerDamageColor, died);
        GameFeelRunner.HitStop(Settings.playerHurtHitStop);
        GameFeelRunner.Shake(Settings.playerHurtShake);
    }

    /// <summary>블록/나무를 한 번 침. 부서졌으면 더 큰 소리와 파편</summary>
    public static void BlockHit(Vector3 position, BlockSoundType type, bool broken, Renderer debrisSource)
    {
        SfxPlayer.PlayAt(GetBlockSfx(type, broken), position);

        if (broken)
        {
            // 광석은 깨질 때도 반짝이는 소리를 겹쳐서 "얻었다" 느낌을 줌
            if (type == BlockSoundType.Ore)
                SfxPlayer.PlayAt(SfxId.DigOre, position, 0.7f);

            GameFeelRunner.Shake(Settings.blockBreakShake);
        }

        GameFeelRunner.SpawnDebris(position, broken ? Settings.blockBreakDebris : Settings.blockHitDebris,
            debrisSource, broken ? 1f : 0.6f);
    }

    private static SfxId GetBlockSfx(BlockSoundType type, bool broken)
    {
        switch (type)
        {
            case BlockSoundType.Wood:
                return broken ? SfxId.BreakWood : SfxId.ChopWood;
            case BlockSoundType.Stone:
                return broken ? SfxId.BreakStone : SfxId.DigStone;
            case BlockSoundType.Ore:
                return broken ? SfxId.BreakStone : SfxId.DigOre;
            default:
                return broken ? SfxId.BreakDirt : SfxId.DigDirt;
        }
    }

    private static void ShowDamage(Vector3 position, int damage, Color color, bool big)
    {
        if (damage <= 0 || !Settings.showDamageNumbers)
            return;

        GameFeelRunner.ShowDamageNumber(position + Vector3.up * Settings.damageNumberHeight, damage, color, big);
    }
}
