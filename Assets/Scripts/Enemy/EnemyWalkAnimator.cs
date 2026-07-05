using UnityEngine;

/// <summary>
/// [Legacy] SpriteRenderer 프레임 순환 — Animator 프리팹 사용 시 불필요.
/// </summary>
[System.Obsolete("Use Animator + AnimationClip on enemy prefab instead.")]
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyWalkAnimator : MonoBehaviour
{
    [SerializeField] float framesPerSecond = 10f;

    SpriteRenderer _sprite;
    Sprite[] _frames;
    int _index;
    float _timer;
    bool _initialized;

    void Awake() => _sprite = GetComponent<SpriteRenderer>();

    public void Initialize(Sprite[] frames, float fps = 10f)
    {
        _frames = frames;
        framesPerSecond = Mathf.Max(1f, fps);
        _index = 0;
        _timer = 0f;
        _initialized = _frames != null && _frames.Length > 0 && _sprite != null;

        if (_initialized)
            _sprite.sprite = _frames[0];
    }

    void Update()
    {
        if (!_initialized || _frames.Length <= 1)
            return;

        _timer += Time.deltaTime;
        float frameDuration = 1f / framesPerSecond;

        while (_timer >= frameDuration)
        {
            _timer -= frameDuration;
            _index = (_index + 1) % _frames.Length;
            _sprite.sprite = _frames[_index];
        }
    }
}
