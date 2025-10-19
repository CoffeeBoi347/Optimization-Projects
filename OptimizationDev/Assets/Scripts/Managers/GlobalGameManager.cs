using UnityEngine;

public class GlobalGameManager : Singleton<GlobalGameManager>
{
    protected override void Awake()
    {
        base.Awake();
        Application.targetFrameRate = 60;
        Screen.SetResolution(1080, 1920, FullScreenMode.FullScreenWindow);
    }
}