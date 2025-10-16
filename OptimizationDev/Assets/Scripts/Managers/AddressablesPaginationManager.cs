using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

[System.Serializable]
public class Profile
{
    public int idprofiles;
    public string profileName;
}

[System.Serializable]
public class ProfileDataFetch
{
    public int count;
    public List<Profile> records;
}

public class AddressablesPaginationManager : Singleton<AddressablesPaginationManager>
{

    [Header("Elements Settings - Predefined Values")]

    [SerializeField] private int panelsToLoad = 20;
    [SerializeField] private int totalProfiles;
    [SerializeField] private string profileAssetKey;
    [SerializeField] private int currentOffset;
    [Header("Elements Settings - Holders")]

    [SerializeField] private Transform pagesContainer;
    public Config config;

    [Header("Other Settings - Cooldown Values")]

    [SerializeField] private int currentCooldownThreshold;
    [SerializeField] private int maxCooldownThreshold;

    private void Start()
    {
        StartCoroutine(UnloadPanels(currentOffset));
    }

    private IEnumerator UnloadPanels(int offset)
    {
        UnityWebRequest request = new UnityWebRequest(config.titleURL, config.method);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        Debug.Log($"Successfully sent a request to {config.titleURL}. Awaiting result.");

        ProfileDataFetch json = JsonUtility.FromJson<ProfileDataFetch>(request.downloadHandler.text);

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Request loaded successfully.");
            currentOffset = pagesContainer.childCount;
            StartCoroutine(ShowPanel(json.records));
        }

        else
        {
            Debug.LogError("Unavailable to unload panels. Retrying again...");
            RetryUnloadingPanels();
            yield return null;
        }
    }

    void RetryUnloadingPanels()
    {
        currentCooldownThreshold++;

        if(maxCooldownThreshold > currentCooldownThreshold)
        {
            Debug.Log($"Unloading panels... Attempt {currentCooldownThreshold}.");
            StartCoroutine(UnloadPanels(currentOffset));
        }

        else
        {
            Debug.Log($"Unavailable to load panels. Please check your connection and try again.");
            return;
        }
    }

    private IEnumerator ShowPanel(List<Profile> totalRecords)
    {
        foreach(var menu in totalRecords)
        {
            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(profileAssetKey, pagesContainer);
            yield return handle;

            if(handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject profile = handle.Result;
                var profileInit = profile.GetComponent<MenuPageUI>();
                profileInit.Init(menu);
            }
        }
    }
}