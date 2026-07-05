using UnityEngine;

/// <summary>
/// 타워 발사 리코일 — 고정 기준 스케일에서 펄스. 코루틴 중첩 시 크기 누적 방지.
/// </summary>
public class TowerFireRecoil : MonoBehaviour
{
    public const float DefaultScale = 0.6f;
    const float PunchAmount = 0.12f;
    const float DecaySpeed = 14f;

    float _pulse;

    void Awake()
    {
        ApplyScale(0f);
    }

    void Update()
    {
        if (_pulse <= 0f)
            return;

        _pulse = Mathf.MoveTowards(_pulse, 0f, Time.deltaTime * DecaySpeed);
        ApplyScale(_pulse);
    }

    public void Punch()
    {
        _pulse = 1f;
        ApplyScale(_pulse);
    }

    void ApplyScale(float pulse)
    {
        float s = DefaultScale * (1f + pulse * PunchAmount);
        transform.localScale = new Vector3(s, s, 1f);
    }
}
