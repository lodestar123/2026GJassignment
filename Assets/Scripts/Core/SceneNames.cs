public static class SceneNames
{
    public const string Start = "StartScene";
    public const string Game = "GameScene";
    public const string GameSpiral = "GameSceneSpiral";
    public const string Practice = "PracticeScene";

    public static string GetGameScene(MapLayoutKind layout) =>
        layout == MapLayoutKind.SpiralIslands ? GameSpiral : Game;
}
