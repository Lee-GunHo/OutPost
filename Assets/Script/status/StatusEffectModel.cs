using System;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffectModel : MonoBehaviour
{
    // 현재 플레이어에게 적용 중인 상태효과 목록
    private List<StatusEffectData> activeEffects = new List<StatusEffectData>();

    // 외부에서 상태효과 목록을 읽기만 할 수 있게 제공
    public IReadOnlyList<StatusEffectData> ActiveEffects => activeEffects;

    public event Action OnEffectsChanged;

    // 상태효과 데미지, 스탯 변경을 실제 플레이어에게 적용하기 위한 참조
    private PlayerPresenter playerPresenter;

    private void Awake()
    {
        playerPresenter = GetComponent<PlayerPresenter>();
    }

    private void Update()
    {
        UpdateEffectDuration();
    }

    // 상태효과 추가
    public void AddEffect(StatusEffectData newEffect)
    {
        if (newEffect == null)
        {
            return;
        }

        if (newEffect.EffectType == StatusEffectType.None)
        {
            return;
        }

        // 같은 종류의 상태효과가 이미 있으면 기존 효과 제거 후 새로 적용
        // 예: 독이 걸린 상태에서 다시 독에 걸리면 시간 초기화 느낌
        RemoveEffect(newEffect.EffectType);

        activeEffects.Add(newEffect);

        ApplyStatusEffectStart(newEffect);

        OnEffectsChanged?.Invoke();

        Debug.Log("상태 효과 추가: " + newEffect.EffectType);
    }

    // 상태효과 제거
    public void RemoveEffect(StatusEffectType effectType)
    {
        bool changed = false;
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].EffectType == effectType)
            {
                Debug.Log("상태 효과 제거: " + activeEffects[i].EffectType);

                RemoveStatusEffectEnd(activeEffects[i]);

                activeEffects.RemoveAt(i);
                changed = true;
            }
        }

        if (changed)
            OnEffectsChanged?.Invoke();
    }

    // 특정 상태효과가 현재 적용 중인지 확인
    public bool HasEffect(StatusEffectType effectType)
    {
        foreach (StatusEffectData effect in activeEffects)
        {
            if (effect.EffectType == effectType)
            {
                return true;
            }
        }

        return false;
    }

    // 매 프레임 상태효과 시간 감소 및 효과 처리
    private void UpdateEffectDuration()
    {
        bool changed = false;
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            StatusEffectData effect = activeEffects[i];

            float deltaTime = Time.deltaTime;

            if (IsDamageOverTimeEffect(effect.EffectType))
            {
                UpdateDamageOverTime(effect, deltaTime);
            }

            effect.DecreaseDuration(deltaTime);

            if (effect.IsExpired())
            {
                Debug.Log("상태 효과 시간 종료: " + effect.EffectType);

                RemoveStatusEffectEnd(effect);

                activeEffects.RemoveAt(i);
                changed = true;
            }
        }

        if (changed)
            OnEffectsChanged?.Invoke();
    }

    // 지속 피해 계열 상태효과인지 확인
    private bool IsDamageOverTimeEffect(StatusEffectType effectType)
    {
        return effectType == StatusEffectType.Poison
            || effectType == StatusEffectType.Bleed
            || effectType == StatusEffectType.Burn;
    }

    // 지속 피해 처리
    private void UpdateDamageOverTime(StatusEffectData effect, float deltaTime)
    {
        int readyTickCount = effect.GetReadyTickCount(deltaTime);

        if (readyTickCount <= 0)
        {
            return;
        }

        for (int i = 0; i < readyTickCount; i++)
        {
            effect.IncreaseTickCount();

            int damage = CalculateDotDamage(effect);

            if (damage <= 0)
            {
                continue;
            }

            if (playerPresenter == null)
            {
                continue;
            }

            playerPresenter.TakeStatusDamage(damage);

            Debug.Log($"{effect.EffectType} 지속 데미지 적용: {damage}, 틱: {effect.TickCount}");
        }
    }

    // 상태효과별 지속 피해 계산
    private int CalculateDotDamage(StatusEffectData effect)
    {
        switch (effect.EffectType)
        {
            case StatusEffectType.Poison:
                // 중독: 일정한 데미지
                return Mathf.RoundToInt(effect.Value);

            case StatusEffectType.Bleed:
                // 출혈: 시간이 지날수록 데미지 증가
                return Mathf.RoundToInt(effect.Value + effect.TickCount);

            case StatusEffectType.Burn:
                // 화상: 일정한 데미지
                return Mathf.RoundToInt(effect.Value);

            default:
                return 0;
        }
    }

    // 상태효과가 처음 적용될 때 실행
    private void ApplyStatusEffectStart(StatusEffectData effect)
    {
        if (playerPresenter == null)
        {
            return;
        }

        if (effect.AttackModifier != 0 || effect.DefenseModifier != 0)
        {
            playerPresenter.AddStatusStats(
                effect.AttackModifier,
                effect.DefenseModifier
            );
        }
    }

    // 상태효과가 끝나거나 제거될 때 실행
    private void RemoveStatusEffectEnd(StatusEffectData effect)
    {
        if (playerPresenter == null)
        {
            return;
        }

        if (effect.AttackModifier != 0 || effect.DefenseModifier != 0)
        {
            playerPresenter.RemoveStatusStats(
                effect.AttackModifier,
                effect.DefenseModifier
            );
        }
    }


}
