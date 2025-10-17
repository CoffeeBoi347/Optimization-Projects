using System.Collections;
using UnityEngine;

public class HierachyManager : Singleton<HierachyManager>
{
    [SerializeField] private GameObject canvasHolder;
    protected override void Awake()
    {
        canvasHolder.SetActive(false);
    }

    private void Start()
    {
        StartCoroutine(UnloadAssets());
    }

    private IEnumerator UnloadAssets()
    {
        yield return AddressablesPaginationManager.Instance.isConnected;

        canvasHolder.SetActive(true);
    }
}