using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 통합 타워 배치 모드 토글 — 마우스 좌클릭만.
/// </summary>
[RequireComponent(typeof(Image))]
public class TowerTypeButton : MonoBehaviour, IPointerClickHandler
{
    public bool Interactable { get; set; } = true;

    Image _image;

    void Awake()
    {
        _image = GetComponent<Image>();
        var legacyButton = GetComponent<Button>();
        if (legacyButton != null)
            Destroy(legacyButton);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!Interactable || eventData.button != PointerEventData.InputButton.Left)
            return;

        TowerSelection.ToggleArm();
    }

    public void SetRaycast(bool enabled)
    {
        if (_image == null)
            _image = GetComponent<Image>();
        if (_image != null)
            _image.raycastTarget = enabled;
    }
}
