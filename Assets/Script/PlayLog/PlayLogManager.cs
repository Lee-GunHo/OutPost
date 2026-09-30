using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이 데이터 로깅 시스템의 중앙 코디네이터.
/// 30초 주기 스냅샷 + 주요 이벤트(사망, 퀘스트 완료, 강화) 발생 시 즉시 스냅샷을 CSV로 남김.
/// 기존 시스템(PlayerPresenter, GameProgressManager, NPCModel 등)은 이 매니저를 몰라도 되고,
/// 이 매니저가 기존 이벤트를 구독하거나 값을 읽어오는 방식으로만 동작.
/// </summary>
public class PlayLogManager : MonoBehaviour
{
    public static PlayLogManager Instance { get; private set; }

    private const float SnapshotIntervalSec = 30f;
    private const float RecentDeathWindowSec = 300f; // 최근 5분

    private PlayerPresenter playerPresenter;
    private PlayLogZoneClassifier zoneClassifier;
    private readonly List<NPCModel> subscribedNpcModels = new List<NPCModel>();

    private string sessionId;
    private float sessionStartTime;

    private int cumulativeDeathCount;
    private readonly List<float> recentDeathTimestamps = new List<float>();
    private string lastDeathCause = string.Empty;

    private int questAcceptedCount;
    private int questCompletedCount;

    private string pendingSessionLabel = string.Empty;

    public static PlayLogManager GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject go = new GameObject("PlayLogManager");
        return go.AddComponent<PlayLogManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        sessionId = Guid.NewGuid().ToString();
        sessionStartTime = Time.time;
    }

    private void Start()
    {
        playerPresenter = FindFirstObjectByType<PlayerPresenter>();

        SeedMapModel seedMapModel = FindFirstObjectByType<SeedMapModel>();
        zoneClassifier = new PlayLogZoneClassifier(seedMapModel);

        if (playerPresenter != null)
        {
            playerPresenter.OnPlayerDied += HandlePlayerDied;
            playerPresenter.OnPlayerUpgraded += HandlePlayerUpgraded;
        }
        else
        {
            Debug.LogWarning("PlayLogManager: PlayerPresenter를 찾지 못했습니다.");
        }

        SubscribeToAllNpcModels();

        StartCoroutine(PeriodicSnapshotRoutine());
    }

    private void OnDestroy()
    {
        if (playerPresenter != null)
        {
            playerPresenter.OnPlayerDied -= HandlePlayerDied;
            playerPresenter.OnPlayerUpgraded -= HandlePlayerUpgraded;
        }

        foreach (NPCModel npcModel in subscribedNpcModels)
        {
            if (npcModel == null)
            {
                continue;
            }

            npcModel.OnQuestAccepted -= HandleQuestAccepted;
            npcModel.OnQuestCompleted -= HandleQuestCompleted;
        }
    }

    private void OnApplicationQuit()
    {
        RecordSessionEndSurvey(pendingSessionLabel);
    }

    private void SubscribeToAllNpcModels()
    {
        NPCModel[] npcModels = FindObjectsByType<NPCModel>(FindObjectsSortMode.None);

        foreach (NPCModel npcModel in npcModels)
        {
            npcModel.OnQuestAccepted += HandleQuestAccepted;
            npcModel.OnQuestCompleted += HandleQuestCompleted;
            subscribedNpcModels.Add(npcModel);
        }
    }

    private IEnumerator PeriodicSnapshotRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(SnapshotIntervalSec);

        while (true)
        {
            yield return wait;
            RecordSnapshot("Periodic");
        }
    }

    private void HandlePlayerDied(string cause, Vector3 position)
    {
        cumulativeDeathCount++;
        recentDeathTimestamps.Add(Time.time);
        lastDeathCause = cause;

        RecordSnapshot("Death");
    }

    private void HandlePlayerUpgraded(string statName)
    {
        RecordSnapshot("Upgrade");
    }

    private void HandleQuestAccepted(string questId)
    {
        questAcceptedCount++;
    }

    private void HandleQuestCompleted(string questId)
    {
        questCompletedCount++;
        RecordSnapshot("QuestComplete");
    }

    /// <summary>
    /// 세션 종료 시("가장 어려웠던 점" 설문 등) 라벨을 지정. 실제 설문 UI는 이후 단계에서 연결.
    /// </summary>
    public void SetSessionEndLabel(string label)
    {
        pendingSessionLabel = label;
    }

    public void RecordSessionEndSurvey(string difficultyLabel)
    {
        float sessionPlayTimeSec = Time.time - sessionStartTime;

        PlayLogWriter.AppendSessionLabel(
            sessionId,
            DateTime.UtcNow.ToString("o"),
            sessionPlayTimeSec,
            difficultyLabel
        );
    }

    private void RecordSnapshot(string eventType)
    {
        PlayLogEntry entry = BuildCurrentSnapshot(eventType);

        if (entry == null)
        {
            return;
        }

        PlayLogWriter.AppendSnapshot(entry);
    }

    /// <summary>
    /// 현재 플레이 상태를 스냅샷으로 만들어 반환(CSV에는 쓰지 않음).
    /// 난관 진단기(IDifficultyDiagnoser)가 진단 입력으로 그대로 재사용.
    /// </summary>
    public PlayLogEntry BuildCurrentSnapshot(string eventType)
    {
        if (playerPresenter == null)
        {
            return null;
        }

        float nowSec = Time.time - sessionStartTime;
        Vector3 position = playerPresenter.transform.position;

        PruneOldDeathTimestamps();
        zoneClassifier.Update(position, nowSec);

        ItemStack selectedItem = HotbarPresenter.Instance != null
            ? HotbarPresenter.Instance.GetSelectedItem()
            : null;
        ItemData selectedItemData = selectedItem != null ? selectedItem.item : null;

        return new PlayLogEntry
        {
            Timestamp = DateTime.UtcNow.ToString("o"),
            SessionId = sessionId,
            ElapsedSessionSec = nowSec,
            EventType = eventType,

            ToolGrade = selectedItemData != null ? selectedItemData.itemGrade.ToString() : "None",
            ToolType = selectedItemData != null ? selectedItemData.toolType.ToString() : playerPresenter.CurrentToolType.ToString(),

            CumulativeDeathCount = cumulativeDeathCount,
            RecentDeathCount5Min = recentDeathTimestamps.Count,
            LastDeathCause = eventType == "Death" ? lastDeathCause : string.Empty,

            MonsterKillCount = GameProgressManager.Instance != null ? GameProgressManager.Instance.NormalMonsterKillCount : 0,
            BossKillCount = GameProgressManager.Instance != null ? GameProgressManager.Instance.BossKillCount : 0,

            QuestAcceptedCount = questAcceptedCount,
            QuestCompletedCount = questCompletedCount,

            CurrentZone = zoneClassifier.CurrentZone,
            ZoneDwellTimeSec = zoneClassifier.GetDwellTimeSec(nowSec),

            PosX = position.x,
            PosY = position.y,
            PosZ = position.z
        };
    }

    /// <summary>
    /// NPC 팁 대화 중 진단 결과/선택지를 1단계 로그와 같은 세션 ID로 기록 (2단계 연동).
    /// </summary>
    public void RecordTipDialogue(
        string difficultyType,
        float confidence,
        string entryId,
        string nodeId,
        string chosenChoiceText)
    {
        PlayLogWriter.AppendTipDialogue(
            sessionId,
            DateTime.UtcNow.ToString("o"),
            difficultyType,
            confidence,
            entryId,
            nodeId,
            chosenChoiceText
        );
    }

    /// <summary>
    /// Compare 진단 모드에서 규칙 기반/ML 결과를 나란히 기록 (4단계 연동).
    /// </summary>
    public void RecordDiagnosisComparison(
        string ruleType,
        float ruleConfidence,
        string mlType,
        float mlConfidence)
    {
        PlayLogWriter.AppendDiagnosisComparison(
            sessionId,
            DateTime.UtcNow.ToString("o"),
            ruleType,
            ruleConfidence,
            mlType,
            mlConfidence
        );
    }

    /// <summary>
    /// 팁을 받고 일정 시간 뒤의 만족도 설문 결과 기록 (5단계 연동). rating이 null이면 건너뛴 것.
    /// </summary>
    public void RecordTipFeedback(string difficultyType, string entryId, int? rating)
    {
        PlayLogWriter.AppendTipFeedback(
            sessionId,
            DateTime.UtcNow.ToString("o"),
            difficultyType,
            entryId,
            rating
        );
    }

    /// <summary>
    /// 팁을 받은 뒤 실제로 난관이 해결됐는지 자동 판정 결과 기록 (5단계 연동).
    /// </summary>
    public void RecordTipResolution(
        string tipTimestamp,
        string difficultyType,
        string entryId,
        bool resolved,
        float? minutesToResolve)
    {
        PlayLogWriter.AppendTipResolution(
            sessionId,
            tipTimestamp,
            DateTime.UtcNow.ToString("o"),
            difficultyType,
            entryId,
            resolved,
            minutesToResolve
        );
    }

    /// <summary>
    /// A/B 테스트 모드에서 이번 세션이 Rule/ML 중 어디에 배정됐는지 기록 (5단계 연동).
    /// </summary>
    public void RecordAbTestAssignment(string group)
    {
        PlayLogWriter.AppendAbTestAssignment(
            sessionId,
            DateTime.UtcNow.ToString("o"),
            group
        );
    }

    private void PruneOldDeathTimestamps()
    {
        float cutoff = Time.time - RecentDeathWindowSec;

        recentDeathTimestamps.RemoveAll(timestamp => timestamp < cutoff);
    }
}
