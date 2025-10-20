using UnityEngine;

public class GlobalGameManager : MonoBehaviour
{
    public void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.SetResolution(1080, 1920, FullScreenMode.FullScreenWindow);
    }
}