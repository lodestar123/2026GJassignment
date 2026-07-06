using UnityEngine;

/// <summary>
/// Screen Space - Camera Canvas가 월드 스프라이트(타워·적·VFX) 위에 그려지도록 정렬.
/// Overlay 모드에서는 무시됩니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public class GameplayCanvasSetup : MonoBehaviour
{
    public const int DefaultSortingOrder = 100;

    [SerializeField] int sortingOrder = DefaultSortingOrder;
    [SerializeField] float planeDistance = 100f;

    void Awake() => Apply();

#if UNITY_EDITOR
    void OnValidate() => Apply();
#endif

    void Apply()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceCamera)
            return;

        if (canvas.worldCamera == null)
            canvas.worldCamera = Camera.main;

        if (planeDistance > 0f)
            canvas.planeDistance = planeDistance;

        if (canvas.sortingOrder < sortingOrder)
            canvas.sortingOrder = sortingOrder;
    }
}
