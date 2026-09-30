using UnityEngine;

/// <summary>
/// 대사 노드 하나의 선택지. nextNodeId가 비어있으면 대화가 그 선택지에서 끝남.
/// </summary>
[System.Serializable]
public class NPCTipChoice
{
    public string choiceText;
    public string nextNodeId;
}

/// <summary>
/// 대사 한 줄(노드). choices가 비어있으면 리프(대화 종료) 노드.
/// </summary>
[System.Serializable]
public class NPCTipNode
{
    public string nodeId;

    [TextArea]
    public string textTemplate;

    public NPCTipChoice[] choices;
}

/// <summary>
/// 특정 난관 유형에 대한 대화 한 편. nodes[0]이 시작 노드.
/// weight가 클수록 같은 유형 안에서 더 자주 선택됨.
/// </summary>
[System.Serializable]
public class NPCTipEntry
{
    public string entryId;

    [Min(0f)]
    public float weight = 1f;

    public NPCTipNode[] nodes;

    public NPCTipNode GetStartNode()
    {
        return nodes != null && nodes.Length > 0 ? nodes[0] : null;
    }

    public NPCTipNode FindNode(string nodeId)
    {
        if (nodes == null || string.IsNullOrEmpty(nodeId))
        {
            return null;
        }

        foreach (NPCTipNode node in nodes)
        {
            if (node != null && node.nodeId == nodeId)
            {
                return node;
            }
        }

        return null;
    }
}
