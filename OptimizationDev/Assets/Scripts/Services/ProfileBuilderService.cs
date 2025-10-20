using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class ProfileBuilderService : Singleton<ProfileBuilderService>
{
    private AsyncOperationHandle<GameObject> handle = new();
    private List<AsyncOperationHandle<GameObject>> handles = new();
    [SerializeField] private string profileAssetKey;

    public IEnumerator SetupPanel(List<Profile> totalRecords, Action onComplete, bool hasLoaded, int totalProfiles, int offset, Transform pagesContainer)
    {
        if (offset >= totalProfiles) { yield break; }

        int batchSize = 6;
        int count = 0;

        foreach (var menu in totalRecords)
        {
            if (!hasLoaded && totalProfiles > pagesContainer.childCount)
            {
                handle = Addressables.InstantiateAsync(profileAssetKey, pagesContainer);
                yield return handle;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject profile = handle.Result;
                    profile.SetActive(false);
                    var profileInit = profile.GetComponent<MenuPageUI>();
                    profileInit.Init(menu);
                    profile.SetActive(true); // activates when ready
                    handles.Add(handle);
                }

                count++;
                Canvas.ForceUpdateCanvases(); // rebuild the canvas immediately
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)pagesContainer); // rebuild the layout and the anchor pos of each element immediately

                if (count % batchSize == 0) // instantiate one addressable asset per 6 frames instead of loading all
                {
                    yield return null;
                }
            }
        }

        Debug.Log("Setup Panel Called! Increasing current offset now.");
        hasLoaded = offset >= totalProfiles;
        onComplete?.Invoke();
    }

    public void Cleanup()
    {
        foreach (var handle in handles.ToList())
        {
            Addressables.Release(handle);
        }
        handles.Clear();
    }
}