using System;



/// <summary>선택 중인 타워 종류 — TowerPlacer가 소비. 같은 버튼 재클릭 시 선택 해제.</summary>

public static class TowerSelection

{

    public static bool HasSelection { get; private set; }

    public static TowerType Selected { get; private set; }



    public static event Action OnChanged;



    public static void Select(TowerType type)

    {

        if (HasSelection && Selected == type)

        {

            HasSelection = false;

            OnChanged?.Invoke();

            return;

        }



        HasSelection = true;

        Selected = type;

        OnChanged?.Invoke();

    }

}

