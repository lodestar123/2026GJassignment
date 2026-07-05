/// <summary>튜토리얼 단계 — 표시 문구 + 진행 조건.</summary>
public enum TutorialAdvanceMode
{
    /// <summary>「다음」 버튼.</summary>
    Manual,
    /// <summary>리듬 키 1회 — Perfect 또는 Good.</summary>
    WaitFirstGoodTap,
    /// <summary>GoldPulse 커맨드 성공(Good 이상).</summary>
    WaitGoldPulseSuccess,
    /// <summary>Scroll 휠로 패턴 1회 변경.</summary>
    WaitPatternChange,
    /// <summary>RhythmShot 커맨드 성공.</summary>
    WaitRhythmShotSuccess,
    /// <summary>타워 1기 배치.</summary>
    WaitTowerPlaced,
    /// <summary>OverloadStrike 커맨드 성공.</summary>
    WaitOverloadSuccess,
}

public readonly struct TutorialStep
{
    public string Title { get; }
    public string Body { get; }
    public TutorialAdvanceMode AdvanceMode { get; }
    public CommandType HighlightPattern { get; }

    public TutorialStep(
        string title,
        string body,
        TutorialAdvanceMode advanceMode,
        CommandType highlightPattern = CommandType.None)
    {
        Title = title;
        Body = body;
        AdvanceMode = advanceMode;
        HighlightPattern = highlightPattern;
    }
}
