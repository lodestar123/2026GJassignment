using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 리듬 키 입력 시 UI 포커스 해제 — Space Submit·WASD 네비게이션과의 충돌 방지.
/// </summary>
[DefaultExecutionOrder(-100)]
public class RhythmUiInputGuard : MonoBehaviour
{
    void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        if (!RhythmKeyFilter.TryGetRhythmKeyDown(out _))
            return;

        var es = EventSystem.current;
        if (es != null)
            es.SetSelectedGameObject(null);
    }
}
