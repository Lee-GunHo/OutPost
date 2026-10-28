using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

/// <summary>
/// 상점 지급 목록에 퀘스트 요구/보상 아이템이 들어가지 않았는지 에셋을 직접 읽어 확인.
/// 제외는 코드로 강제하지 않고 인스펙터 구성으로만 관리하기로 했으므로, 실수로 넣었을 때 이 테스트가 잡아줌.
/// </summary>
public class ShopQuestItemExclusionTests
{
    [Test]
    public void ShopGiveLists_DoNotContainQuestItems()
    {
        HashSet<ItemData> questItems = new HashSet<ItemData>();

        foreach (string guid in AssetDatabase.FindAssets("t:QuestData"))
        {
            QuestData quest = AssetDatabase.LoadAssetAtPath<QuestData>(AssetDatabase.GUIDToAssetPath(guid));

            if (quest == null)
            {
                continue;
            }

            if (quest.RequiredItem != null)
            {
                questItems.Add(quest.RequiredItem);
            }

            if (quest.RewardItem != null)
            {
                questItems.Add(quest.RewardItem);
            }
        }

        Assert.IsNotEmpty(questItems, "QuestData 에셋에서 퀘스트 아이템을 찾지 못했습니다.");

        foreach (string guid in AssetDatabase.FindAssets("t:ShopData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ShopData shop = AssetDatabase.LoadAssetAtPath<ShopData>(path);

            if (shop == null || shop.TradeItems == null)
            {
                continue;
            }

            foreach (ShopItemData shopItem in shop.TradeItems)
            {
                if (shopItem == null || shopItem.ItemData == null)
                {
                    continue;
                }

                Assert.IsFalse(
                    questItems.Contains(shopItem.ItemData),
                    $"{path}의 지급 목록에 퀘스트 아이템 '{shopItem.ItemData.itemName}'이(가) 들어 있습니다.");
            }
        }
    }
}
