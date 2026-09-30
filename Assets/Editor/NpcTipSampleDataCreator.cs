using UnityEditor;
using UnityEngine;

/// <summary>
/// NPC 팁 대화 시스템(2단계) 테스트용 ScriptableObject를 자동 생성하는 에디터 툴.
/// Tools > PlayLog > Create Sample Tip Dialogue Data 메뉴에서 1회 실행.
/// 이미 자산이 있으면 건드리지 않음(덮어쓰지 않음).
/// </summary>
public static class NpcTipSampleDataCreator
{
    private const string FolderPath = "Assets/SO/NpcTip";
    private const string RuleConfigPath = FolderPath + "/DifficultyRuleConfig.asset";
    private const string DialogueSetPath = FolderPath + "/NPCTipDialogueSet.asset";

    [MenuItem("Tools/PlayLog/Create Sample Tip Dialogue Data")]
    public static void CreateSampleData()
    {
        EnsureFolder();

        bool createdRuleConfig = CreateRuleConfigIfMissing();
        bool createdDialogueSet = CreateDialogueSetIfMissing();

        if (createdRuleConfig || createdDialogueSet)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"NPC 팁 대화 샘플 데이터 준비 완료 ({FolderPath}). " +
            $"RuleConfig 생성: {createdRuleConfig}, DialogueSet 생성: {createdDialogueSet}");
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/SO"))
        {
            AssetDatabase.CreateFolder("Assets", "SO");
        }

        if (!AssetDatabase.IsValidFolder(FolderPath))
        {
            AssetDatabase.CreateFolder("Assets/SO", "NpcTip");
        }
    }

    private static bool CreateRuleConfigIfMissing()
    {
        if (AssetDatabase.LoadAssetAtPath<DifficultyRuleConfig>(RuleConfigPath) != null)
        {
            return false;
        }

        DifficultyRuleConfig config = ScriptableObject.CreateInstance<DifficultyRuleConfig>();
        AssetDatabase.CreateAsset(config, RuleConfigPath);
        return true;
    }

    private static bool CreateDialogueSetIfMissing()
    {
        if (AssetDatabase.LoadAssetAtPath<NPCTipDialogueSet>(DialogueSetPath) != null)
        {
            return false;
        }

        NPCTipDialogueSet dialogueSet = ScriptableObject.CreateInstance<NPCTipDialogueSet>();

        dialogueSet.EditorSetEntries(new[]
        {
            new NPCTipDialogueSet.DifficultyEntries
            {
                difficultyType = DifficultyType.EquipmentLack,
                entries = BuildEquipmentLackEntries()
            },
            new NPCTipDialogueSet.DifficultyEntries
            {
                difficultyType = DifficultyType.CombatStruggle,
                entries = BuildCombatStruggleEntries()
            },
            new NPCTipDialogueSet.DifficultyEntries
            {
                difficultyType = DifficultyType.Lost,
                entries = BuildLostEntries()
            },
            new NPCTipDialogueSet.DifficultyEntries
            {
                difficultyType = DifficultyType.ResourceLack,
                entries = BuildResourceLackEntries()
            },
            new NPCTipDialogueSet.DifficultyEntries
            {
                difficultyType = DifficultyType.Smooth,
                entries = BuildSmoothEntries()
            }
        });

        AssetDatabase.CreateAsset(dialogueSet, DialogueSetPath);
        EditorUtility.SetDirty(dialogueSet);
        return true;
    }

    private static NPCTipEntry[] BuildEquipmentLackEntries()
    {
        return new[]
        {
            new NPCTipEntry
            {
                entryId = "equip_1",
                weight = 1f,
                nodes = new[]
                {
                    new NPCTipNode
                    {
                        nodeId = "start",
                        textTemplate = "그 {toolGrade} 등급 장비로 여기까지 온 것도 대단하네. 좀 더 나은 장비가 필요해 보이는데?",
                        choices = new[]
                        {
                            new NPCTipChoice { choiceText = "장비는 어디서 구해?", nextNodeId = "shop_hint" },
                            new NPCTipChoice { choiceText = "괜찮아, 알아서 할게", nextNodeId = "" }
                        }
                    },
                    new NPCTipNode
                    {
                        nodeId = "shop_hint",
                        textTemplate = "대장간이나 상점을 들러봐. 지금 장비로는 앞으로 더 힘들어질 거야.",
                        choices = new NPCTipChoice[0]
                    }
                }
            },
            new NPCTipEntry
            {
                entryId = "equip_2",
                weight = 1f,
                nodes = new[]
                {
                    new NPCTipNode
                    {
                        nodeId = "start",
                        textTemplate = "사망을 {deathCount}번이나 했다고? 장비부터 점검해보는 게 어때.",
                        choices = new[]
                        {
                            new NPCTipChoice { choiceText = "어떤 장비가 좋아?", nextNodeId = "advice" },
                            new NPCTipChoice { choiceText = "몬스터가 너무 강해서 그래", nextNodeId = "denial" }
                        }
                    },
                    new NPCTipNode
                    {
                        nodeId = "advice",
                        textTemplate = "등급이 높은 무기/방어구일수록 생존에 큰 도움이 돼. 조금만 더 모아봐.",
                        choices = new NPCTipChoice[0]
                    },
                    new NPCTipNode
                    {
                        nodeId = "denial",
                        textTemplate = "그럴 수도 있지만, 장비 등급도 한 번 확인해보면 좋을 거야.",
                        choices = new NPCTipChoice[0]
                    }
                }
            }
        };
    }

    private static NPCTipEntry[] BuildCombatStruggleEntries()
    {
        return new[]
        {
            new NPCTipEntry
            {
                entryId = "combat_1",
                weight = 1f,
                nodes = new[]
                {
                    new NPCTipNode
                    {
                        nodeId = "start",
                        textTemplate = "최근에 자주 쓰러지던데, 전투 방식을 좀 바꿔보는 건 어때?",
                        choices = new[]
                        {
                            new NPCTipChoice { choiceText = "어떻게 싸워야 해?", nextNodeId = "tips" },
                            new NPCTipChoice { choiceText = "그냥 운이 나빴어", nextNodeId = "" }
                        }
                    },
                    new NPCTipNode
                    {
                        nodeId = "tips",
                        textTemplate = "무작정 덤비지 말고, 몬스터 패턴을 보고 피하는 타이밍을 잡아봐.",
                        choices = new NPCTipChoice[0]
                    }
                }
            },
            new NPCTipEntry
            {
                entryId = "combat_2",
                weight = 1f,
                nodes = new[]
                {
                    new NPCTipNode
                    {
                        nodeId = "start",
                        textTemplate = "{deathCount}번이나 쓰러졌다니... 무리하지 말고 한 마리씩 상대해봐.",
                        choices = new[]
                        {
                            new NPCTipChoice { choiceText = "알겠어, 조심할게", nextNodeId = "" },
                            new NPCTipChoice { choiceText = "무기를 바꿔야 할까?", nextNodeId = "weapon" }
                        }
                    },
                    new NPCTipNode
                    {
                        nodeId = "weapon",
                        textTemplate = "무기보다 먼저 거리 조절과 회피 타이밍부터 연습해보는 게 나을 거야.",
                        choices = new NPCTipChoice[0]
                    }
                }
            }
        };
    }

    private static NPCTipEntry[] BuildLostEntries()
    {
        return new[]
        {
            new NPCTipEntry
            {
                entryId = "lost_1",
                weight = 1f,
                nodes = new[]
                {
                    new NPCTipNode
                    {
                        nodeId = "start",
                        textTemplate = "한 자리에 계속 머물러 있던데, 길을 잃은 거야?",
                        choices = new[]
                        {
                            new NPCTipChoice { choiceText = "{questName} 퀘스트를 어디서 해야 할지 모르겠어", nextNodeId = "quest_hint" },
                            new NPCTipChoice { choiceText = "그냥 둘러보고 있었어", nextNodeId = "" }
                        }
                    },
                    new NPCTipNode
                    {
                        nodeId = "quest_hint",
                        textTemplate = "퀘스트 목표는 보통 표시된 방향을 따라가면 나와. 너무 오래 헤매지는 마.",
                        choices = new NPCTipChoice[0]
                    }
                }
            },
            new NPCTipEntry
            {
                entryId = "lost_2",
                weight = 1f,
                nodes = new[]
                {
                    new NPCTipNode
                    {
                        nodeId = "start",
                        textTemplate = "같은 구역만 맴도는 것 같은데, 지도를 한 번 확인해봐.",
                        choices = new[]
                        {
                            new NPCTipChoice { choiceText = "지도는 어떻게 봐?", nextNodeId = "map_hint" },
                            new NPCTipChoice { choiceText = "알아서 찾아볼게", nextNodeId = "" }
                        }
                    },
                    new NPCTipNode
                    {
                        nodeId = "map_hint",
                        textTemplate = "인벤토리나 메뉴에서 지도를 열 수 있을 거야. 막히면 나한테 다시 물어봐.",
                        choices = new NPCTipChoice[0]
                    }
                }
            }
        };
    }

    private static NPCTipEntry[] BuildResourceLackEntries()
    {
        return new[]
        {
            new NPCTipEntry
            {
                entryId = "resource_1",
                weight = 1f,
                nodes = new[]
                {
                    new NPCTipNode
                    {
                        nodeId = "start",
                        textTemplate = "한참 플레이했는데 몬스터를 별로 못 잡았네. 자원이 부족한 거 아니야?",
                        choices = new[]
                        {
                            new NPCTipChoice { choiceText = "어디서 자원을 모아?", nextNodeId = "gather_hint" },
                            new NPCTipChoice { choiceText = "천천히 하고 있어서 그래", nextNodeId = "" }
                        }
                    },
                    new NPCTipNode
                    {
                        nodeId = "gather_hint",
                        textTemplate = "안전 구역 근처에서 나무나 돌부터 모아봐. 기본 자원이 있어야 장비도 만들 수 있어.",
                        choices = new NPCTipChoice[0]
                    }
                }
            },
            new NPCTipEntry
            {
                entryId = "resource_2",
                weight = 1f,
                nodes = new[]
                {
                    new NPCTipNode
                    {
                        nodeId = "start",
                        textTemplate = "{toolGrade} 등급 장비 그대로면 자원 모으기도 오래 걸릴 텐데.",
                        choices = new[]
                        {
                            new NPCTipChoice { choiceText = "더 좋은 도구는 어떻게 만들어?", nextNodeId = "craft_hint" },
                            new NPCTipChoice { choiceText = "괜찮아, 천천히 할게", nextNodeId = "" }
                        }
                    },
                    new NPCTipNode
                    {
                        nodeId = "craft_hint",
                        textTemplate = "기본 자원을 어느 정도 모으면 제작대에서 더 나은 도구를 만들 수 있어.",
                        choices = new NPCTipChoice[0]
                    }
                }
            }
        };
    }

    private static NPCTipEntry[] BuildSmoothEntries()
    {
        return new[]
        {
            new NPCTipEntry
            {
                entryId = "smooth_1",
                weight = 1f,
                nodes = new[]
                {
                    new NPCTipNode
                    {
                        nodeId = "start",
                        textTemplate = "오, 잘 하고 있네! 이대로만 하면 금방 성장하겠어.",
                        choices = new[]
                        {
                            new NPCTipChoice { choiceText = "고마워!", nextNodeId = "" },
                            new NPCTipChoice { choiceText = "더 조언해줄 거 있어?", nextNodeId = "encourage" }
                        }
                    },
                    new NPCTipNode
                    {
                        nodeId = "encourage",
                        textTemplate = "지금 페이스 그대로 유지하면 돼. 무리하지 말고 즐겨.",
                        choices = new NPCTipChoice[0]
                    }
                }
            },
            new NPCTipEntry
            {
                entryId = "smooth_2",
                weight = 1f,
                nodes = new[]
                {
                    new NPCTipNode
                    {
                        nodeId = "start",
                        textTemplate = "사망도 거의 없고 순조롭게 진행 중이네. 대단한데?",
                        choices = new[]
                        {
                            new NPCTipChoice { choiceText = "다음엔 뭘 해야 할까?", nextNodeId = "next" },
                            new NPCTipChoice { choiceText = "고마워", nextNodeId = "" }
                        }
                    },
                    new NPCTipNode
                    {
                        nodeId = "next",
                        textTemplate = "다음 구역으로 넘어가서 더 좋은 자원을 노려봐도 좋을 것 같아.",
                        choices = new NPCTipChoice[0]
                    }
                }
            }
        };
    }
}
