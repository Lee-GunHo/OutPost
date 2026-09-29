using UnityEngine;

public sealed class MonsterCombatView : MonoBehaviour
{
    private GameObject warning;
    private Material material;

    public Material AttackMaterial
    {
        get
        {
            if (material == null)
                material = Resources.Load<Material>("MonsterCombat/AttackVisual");
            return material;
        }
    }

    public void ShowWarning(MonsterAttackKind kind, Vector3 origin, Vector3 direction, float radius)
    {
        ClearWarning();
        if (kind == MonsterAttackKind.Melee) return;
        warning = new GameObject("Monster " + kind + " warning");
        var line = warning.AddComponent<LineRenderer>();
        line.sharedMaterial = AttackMaterial;
        line.useWorldSpace = true;
        line.widthMultiplier = 0.07f;
        line.startColor = line.endColor = kind == MonsterAttackKind.Circle
            ? new Color(1f, 0.15f, 0.05f, 0.9f)
            : kind == MonsterAttackKind.Charge ? new Color(1f, 0.3f, 0.65f, 0.9f)
            : new Color(1f, 0.65f, 0.05f, 0.9f);
        if (kind == MonsterAttackKind.Circle)
        {
            line.loop = true;
            line.positionCount = 64;
            origin.y = 0.12f;
            for (int i = 0; i < 64; i++)
            {
                float angle = i * Mathf.PI * 2f / 64f;
                line.SetPosition(i, origin + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
            }
        }
        else if (kind == MonsterAttackKind.Charge)
        {
            origin.y = 0.12f;
            Vector3 tip = origin + direction * radius;
            Vector3 side = Vector3.Cross(Vector3.up, direction) * 0.35f;
            line.positionCount = 5;
            line.SetPosition(0, origin);
            line.SetPosition(1, tip);
            line.SetPosition(2, tip - direction * 0.5f + side);
            line.SetPosition(3, tip);
            line.SetPosition(4, tip - direction * 0.5f - side);
        }
        else
        {
            line.positionCount = 2;
            line.SetPosition(0, origin);
            line.SetPosition(1, origin + direction * radius);
        }
    }

    public void ClearWarning()
    {
        if (warning == null) return;
        warning.SetActive(false);
        Destroy(warning);
        warning = null;
    }

    private void OnDisable() { ClearWarning(); }
}
