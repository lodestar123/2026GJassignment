using System;
using System.Collections.Generic;
using UnityEngine;

public class SkillCooldownController : MonoBehaviour
{
    public static SkillCooldownController Instance { get; private set; }

    public const float OverloadStrikeCooldown = 4f;
    public const float ChainZapCooldown = 8f;

    readonly Dictionary<CommandType, float> _readyAt = new();

    public event Action OnCooldownsChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool IsOnCooldown(CommandType type)
    {
        return GetRemaining(type) > 0f;
    }

    public float GetRemaining(CommandType type)
    {
        if (_cooldownsDisabled)
            return 0f;

        if (!_readyAt.TryGetValue(type, out var readyAt))
            return 0f;

        return Mathf.Max(0f, readyAt - Time.time);
    }

    public bool RequiresCooldown(CommandType type)
    {
        return type == CommandType.OverloadStrike || type == CommandType.ChainZap;
    }

    public bool TryConsume(CommandType type)
    {
        if (!RequiresCooldown(type))
            return true;

        if (_cooldownsDisabled)
            return true;

        if (IsOnCooldown(type))
            return false;

        float duration = type switch
        {
            CommandType.OverloadStrike => OverloadStrikeCooldown,
            CommandType.ChainZap => ChainZapCooldown,
            _ => 0f
        };

        _readyAt[type] = Time.time + duration;
        OnCooldownsChanged?.Invoke();
        return true;
    }

    public void SetCooldownsDisabled(bool disabled)
    {
        _cooldownsDisabled = disabled;
    }

    bool _cooldownsDisabled;

    public bool CanUse(CommandType type)
    {
        if (!_cooldownsDisabled && RequiresCooldown(type) && IsOnCooldown(type))
            return false;

        return true;
    }
}
