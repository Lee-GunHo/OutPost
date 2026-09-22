using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 넥서스의 체력을 UI에 표시
/// </summary>
public class NexusView : MonoBehaviour
{
    /// <summary>
    /// Replace sloped mesh collision with vertical sides while preserving its footprint.
    /// The rendered model is unchanged.
    /// </summary>
    public void ConfigureBlockingBody(float minimumWorldHeight)
    {
        foreach (MeshCollider mesh in GetComponentsInChildren<MeshCollider>(true))
        {
            if (!mesh.enabled || mesh.isTrigger || mesh.sharedMesh == null)
                continue;

            Bounds bounds = mesh.sharedMesh.bounds;
            Vector3 size = bounds.size;
            float scaleY = Mathf.Max(0.001f, Mathf.Abs(mesh.transform.lossyScale.y));
            size.y = Mathf.Max(size.y, minimumWorldHeight / scaleY);
            Vector3 center = bounds.center;
            center.y = bounds.min.y + size.y * 0.5f;

            BoxCollider blocker = mesh.gameObject.AddComponent<BoxCollider>();
            blocker.center = center;
            blocker.size = size;
            blocker.sharedMaterial = mesh.sharedMaterial;
            mesh.enabled = false;
        }
    }

    [Header("체력 UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;

    [SerializeField] private GameObject healthRoot;

    public void SetHealthVisible(bool isVisible)
    {
        if (healthRoot != null)
        {
            healthRoot.SetActive(isVisible);
        }
    }

    public void Initialize(int currentHealth, int maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = maxHealth;
        }

        RefreshHealth(currentHealth, maxHealth);
    }

    public void RefreshHealth(int currentHealth, int maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{currentHealth} / {maxHealth}";
        }
    }
}
