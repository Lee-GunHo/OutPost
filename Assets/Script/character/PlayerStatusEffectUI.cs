using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerStatusEffectUI : MonoBehaviour
{
    [Serializable]
    public class IconDefinition
    {
        public StatusEffectType effectType;
        public Sprite sprite;
        public string placeholderLabel;
        public Color placeholderColor = Color.gray;
    }

    [Header("References")]
    [SerializeField] private StatusEffectModel statusEffectModel;
    [SerializeField] private Image iconTemplate;

    [Header("Status Icons")]
    [Tooltip("각 상태이상의 Sprite를 연결하세요. 비어 있으면 임시 색상과 글자를 표시합니다.")]
    [SerializeField] private IconDefinition[] icons = Array.Empty<IconDefinition>();

    private readonly Dictionary<StatusEffectType, GameObject> slots =
        new Dictionary<StatusEffectType, GameObject>();
    private StatusEffectModel subscribedModel;

    private void Awake()
    {
        if (iconTemplate == null)
            return;

        iconTemplate.gameObject.SetActive(false);
        foreach (IconDefinition definition in icons)
        {
            if (definition == null || definition.effectType == StatusEffectType.None ||
                slots.ContainsKey(definition.effectType))
                continue;

            Image icon = Instantiate(iconTemplate, iconTemplate.transform.parent);
            icon.name = definition.effectType + "Icon";
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            bool hasSprite = definition.sprite != null;
            icon.sprite = hasSprite ? definition.sprite : iconTemplate.sprite;
            icon.color = hasSprite ? Color.white : definition.placeholderColor;

            TMP_Text label = icon.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = definition.placeholderLabel;
                label.raycastTarget = false;
                label.gameObject.SetActive(!hasSprite);
            }

            slots.Add(definition.effectType, icon.gameObject);
        }
    }

    private void OnEnable()
    {
        TryBindModel();
        RefreshIcons();
    }

    private void LateUpdate()
    {
        // Also supports a player instantiated after the HUD, or replaced on respawn.
        if (subscribedModel == null)
        {
            TryBindModel();
            RefreshIcons();
        }
    }

    private void TryBindModel()
    {
        if (subscribedModel != null)
            return;

        if (statusEffectModel == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                statusEffectModel = player.GetComponent<StatusEffectModel>();
        }

        if (statusEffectModel == null)
            return;

        subscribedModel = statusEffectModel;
        subscribedModel.OnEffectsChanged += RefreshIcons;
    }

    private void RefreshIcons()
    {
        foreach (KeyValuePair<StatusEffectType, GameObject> slot in slots)
        {
            bool visible = subscribedModel != null && subscribedModel.HasEffect(slot.Key);
            if (slot.Value.activeSelf != visible)
                slot.Value.SetActive(visible);
        }
    }

    private void OnDisable()
    {
        if (subscribedModel != null)
            subscribedModel.OnEffectsChanged -= RefreshIcons;

        subscribedModel = null;
        foreach (GameObject slot in slots.Values)
            slot.SetActive(false);
    }
}
