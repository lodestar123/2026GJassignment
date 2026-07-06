using UnityEngine;

/// <summary>
/// URP 2D 빌드에서 SpriteRenderer 머티리얼 보정.
/// </summary>
public static class SpriteRendererUtility
{
    static Material _spriteMaterial;

    public static void EnsureSpriteMaterial(SpriteRenderer renderer)
    {
        if (renderer == null)
            return;

        var current = renderer.sharedMaterial;
        if (current != null && current.shader != null)
        {
            var shaderName = current.shader.name;
            if (!shaderName.Contains("Hidden") && !shaderName.Contains("Error"))
                return;
        }

        if (_spriteMaterial == null)
        {
            _spriteMaterial = Resources.Load<Material>("BeatDefender/SpriteDefault");
            if (_spriteMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                    ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default")
                    ?? Shader.Find("Sprites/Default");
                if (shader != null)
                    _spriteMaterial = new Material(shader);
            }
        }

        if (_spriteMaterial != null)
            renderer.sharedMaterial = _spriteMaterial;
    }
}
