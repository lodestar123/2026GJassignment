using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 타워 종류 선택 — 마우스 좌클릭만. UI Button Submit/키보드 네비게이션과 리듬 입력 충돌 방지.
/// </summary>
[RequireComponent(typeof(Image))]
public class TowerTypeButton : MonoBehaviour, IPointerClickHandler
{
    public TowerType Type { get; set; }
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

        TowerSelection.Select(Type);
    }

    public void SetRaycast(bool enabled)
    {
        if (_image == null)
            _image = GetComponent<Image>();
        if (_image != null)
            _image.raycastTarget = enabled;
    }
}
