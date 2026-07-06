#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class TowerVisualConfigEditor
{
    const string SpritesRoot = "Assets/Resources/BeatDefender/Sprites";
    const string PrefabPath = "Assets/Resources/BeatDefender/BeatTower.prefab";
    const string SpriteMaterialPath = "Assets/Resources/BeatDefender/SpriteDefault.mat";

    [MenuItem("Beat Defender/Setup Tower Visual Config")]
    public static void SetupFromMenu()
    {
        if (EnsureConfigAsset())
            Debug.Log("Beat Defender: BeatTower 프리팹 이미지 설정 완료 → " + PrefabPath);
        else
            Debug.LogError("Beat Defender: BeatTower 프리팹 이미지 설정 실패.");
    }

    public static bool EnsureConfigAsset()
    {
        MapPrefabBuilder.SyncTowerSpritesToResources();
        return EnsureSpriteMaterialAsset() && EnsurePrefabSprites();
    }

    static bool EnsureSpriteMaterialAsset()
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(SpriteMaterialPath) != null)
            return true;

        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
            ?? Shader.Find("Sprites/Default");
        if (shader == null)
            return false;

        var mat = new Material(shader);
        AssetDatabase.CreateAsset(mat, SpriteMaterialPath);
        AssetDatabase.SaveAssets();
        return true;
    }

    static bool EnsurePrefabSprites()
    {
        var lv1 = LoadSprite($"{SpritesRoot}/Tower_Level1_1.png", "Tower_Level1_1_0");
        var lv2 = LoadSprite($"{SpritesRoot}/Tower_Level2.png", "Tower_Level2_0");
        var lv3 = LoadSprite($"{SpritesRoot}/Tower_Level3.png", "Tower_Level3_1");
        var ring = LoadSprite($"{SpritesRoot}/RangeIndicatorRing.png", "RangeIndicatorRing_0");

        if (lv1 == null || ring == null)
            return false;

        if (!System.IO.File.Exists(PrefabPath))
            return false;

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var body = root.GetComponent<SpriteRenderer>();
            if (body != null)
            {
                body.sprite = lv1;
                SpriteRendererUtility.EnsureSpriteMaterial(body);
            }

            root.GetComponent<BeatTower>()?.SetLevelSprites(lv1, lv2, lv3);
            root.GetComponent<TowerRangeVisualizer>()?.SetRangeRingSprite(ring);

            var ringTf = root.transform.Find("RangeRing");
            var ringSr = ringTf != null ? ringTf.GetComponent<SpriteRenderer>() : null;
            if (ringSr != null)
            {
                ringSr.sprite = ring;
                SpriteRendererUtility.EnsureSpriteMaterial(ringSr);
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        return true;
    }

    static Sprite LoadSprite(string path, string spriteName)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var asset in assets)
        {
            if (asset is Sprite sprite && sprite.name == spriteName)
                return sprite;
        }

        foreach (var asset in assets)
        {
            if (asset is Sprite sprite)
                return sprite;
        }

        return null;
    }
}
#endif
