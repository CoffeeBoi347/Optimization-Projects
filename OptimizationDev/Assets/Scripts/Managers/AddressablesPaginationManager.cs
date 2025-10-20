using System.Collections;
using System.Collections.Generic;
using UnityEngine;
<<<<<<< Updated upstream
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
=======
using UnityEngine.UI;
>>>>>>> Stashed changes

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

public sealed class AddressablesPaginationManager : Singleton<AddressablesPaginationManager>
{
<<<<<<< Updated upstream

    [Header("Elements Settings - Predefined Values")]

    [SerializeField] private int panelsToLoad = 20;
    [SerializeField] private int totalProfiles;
    [SerializeField] private string profileAssetKey;
    [SerializeField] private int currentOffset;
    [Header("Elements Settings - Holders")]
=======
    public PaginationStates paginationStates;

    [Header("Elements Settings - Predefined Values") ,Tooltip("Predefined Values For Loading Elements/Clamping Values/Scroll Rect Loading")]

    [SerializeField] private int panelsToLoad = 20;
    [SerializeField] private int totalProfiles;
    private int currentOffset;
    private int targetOffset;
>>>>>>> Stashed changes

    [SerializeField] private Transform pagesContainer;
<<<<<<< Updated upstream
    public Config config;
=======
>>>>>>> Stashed changes

    [Header("Other Settings - Cooldown Values")]

    [SerializeField] private int currentCooldownThreshold;
    [SerializeField] private int maxCooldownThreshold;

    #region Private References

    public bool isConnected = false;
<<<<<<< Updated upstream

    private void Start()
    {
        StartCoroutine(UnloadPanels(currentOffset));
=======
    private bool isLoadingNext = false;
    private bool isLoadingPrevious = false;
    private bool hasLoaded = false;

    #endregion

    private void Start()
    {
        paginationStates = PaginationStates.None;

        Debug.Log("[START] : Addressables Panel Manager : Unloading Panels Now.");
        Debug.Log($"pagesContainer: {pagesContainer}");
        Debug.Log($"loadPreviousPanelsButton: {loadPreviousPanelsButton}");
        Debug.Log($"panelsScrollRect: {panelsScrollRect}");

        if (pagesContainer == null || loadPreviousPanelsButton == null || panelsScrollRect == null)
        {
            Debug.LogError("Missing references. Assign the respected references currently stated null.");
            return;
        }

        targetOffset = currentOffset;
        loadPreviousPanelsButton.gameObject.SetActive(false);
        StartCoroutine(LoadNextPage(currentOffset, false));
>>>>>>> Stashed changes
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
            isConnected = true;

            currentOffset = pagesContainer.childCount;
            StartCoroutine(ShowPanel(json.records));
        }

        else
        {
            Debug.LogError("Unavailable to unload panels. Retrying again...");
            RetryUnloadingPanels();
            yield return null;
        }

        ProfileBuilderService.Instance.Cleanup();
    }

<<<<<<< Updated upstream
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
=======
    #region Loading Pages

    private IEnumerator LoadNextPage(int offset, bool isScrollingUp)
    {
        if(offset > totalProfiles ) {  yield break; }
        yield return new WaitForEndOfFrame();
        isLoadingNext = true;
        StartCoroutine(RESTAPIService.Instance.SendRequestGetElements
            (
                panelsToLoad,
                offset,
                onSuccess: (jsonText) =>
                {
                    try
                    {
                        var json = JsonUtility.FromJson<ProfileDataFetch>(jsonText);

                        currentCooldownThresholdUnloadingPanels = 0;
                        isConnected = true;
                        isLoadingNext = true;

                        StartCoroutine(ProfileBuilderService.Instance.SetupPanel(json.records, () => // if setup is finished, then increase current offset and reset loading next
                        {
                            paginationStates = PaginationStates.OnNextPage;
                            currentOffset = Mathf.Min(currentOffset + panelsToLoad, totalProfiles);
                            isLoadingNext = false;
                            if (currentOffset > panelsToLoad) { ResetCurrentOffsetOutOfRange(0); }
                            Debug.Log($"[Setup Panel] : Builded UI page successfully.");
                            targetOffset = currentOffset;
                        }, hasLoaded, totalProfiles, currentOffset, pagesContainer));

                        StartCoroutine(ResetBoolean(0.05f, () => isLoadingNext = false));
                        
                        for(int i = currentOffset - panelsToLoad; i < currentOffset; i++)
                        {
                            if (pagesContainer.GetChild(i).gameObject.activeInHierarchy)
                            {
                                pagesContainer.GetChild(i).gameObject.SetActive(false);
                            }
                        }
                    }

                    catch (Exception e)
                    {
                        Debug.Log($"Error: {e.Message}");
                    }
                },
                onError: (jsonText) =>
                {
                    Debug.Log($"Unavailable to reload panels. Trying again.");
                    RetryLoaderService.Instance.RetryLoadingPanels(ref currentCooldownThresholdUnloadingPanels, maxCooldownThreshold, currentOffset, LoadNextPage);
                }

            ));
    }

    private IEnumerator LoadPreviousPage(int offset, bool isScrollingUp)
    {
        yield return new WaitForEndOfFrame();

        try
        {
            paginationStates = PaginationStates.OnPreviousPage;
            Debug.Log("[LoadPanels]: Loading Previous Panels Success.");
            isLoadingPrevious = true;
            currentCooldownThresholdLoadingPanels = 0;
            for (int i = 0; i < pagesContainer.childCount; i++)
            {
                bool shouldBeActive = (i >= currentOffset - panelsToLoad && i < currentOffset && isLoadingPrevious);
                Debug.Log($"{i} is active? {shouldBeActive}");
                pagesContainer.GetChild(i).gameObject.SetActive(shouldBeActive);
            }
        }

        catch(System.Exception e)
        {
            Debug.LogError($"Error: {e.Message} | Unavailable to load previous page. Retrying...");
            RetryLoaderService.Instance.RetryLoadingPanels(ref currentCooldownThresholdLoadingPanels, maxCooldownThreshold, currentOffset, LoadPreviousPage);
        }
    }
>>>>>>> Stashed changes

            if(handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject profile = handle.Result;
                var profileInit = profile.GetComponent<MenuPageUI>();
                profileInit.Init(menu);
            }
<<<<<<< Updated upstream
        }
=======

            if (pagesContainer.childCount >= totalProfiles && currentOffset > startLimit)
            {
                currentOffset = startLimit;
            }
        }

        catch(System.Exception e)
        {
            Debug.LogError($"Error: {e.Message}");
        }
    }

    #endregion

    void OnScrollViewChanged(Vector2 scrollPos)
    {
        panelsScrollRectPos = scrollPos;

        if (!isLoadingNext && panelsScrollRect.verticalNormalizedPosition <= scrollRectSensivity) // if new pool isnt being instantiated and scroll rect is at bottom
        {
            Debug.Log("[OnScrollViewChanged] Loading next panels.");
            loadPreviousPanelsButton.gameObject.SetActive(false);
            StartCoroutine(LoadNextPage(currentOffset, false));
        }

        else if (currentOffset >= panelsToLoad &&
                 panelsScrollRect.verticalNormalizedPosition >= scrollRectSensivityUp && !isLoadingPrevious) // if new pool isnt being spawned and scroll rect is at top
        {
            loadPreviousPanelsButton.gameObject.SetActive(true);
        }
    }

    void OnPreviousButtonClicked() // activated when scroll rect value is less than its sensitivity
    {
        if(currentOffset > panelsToLoad)
            currentOffset = Mathf.Max(currentOffset - panelsToLoad, 0);

        isLoadingPrevious = true;

        Debug.Log("[OnScrollViewChanged] Loading previous panels.");
        StartCoroutine(LoadPreviousPage(currentOffset, true));

        StartCoroutine(ResetBoolean(0.05f, () => isLoadingPrevious = false));

        loadPreviousPanelsButton.gameObject.SetActive(false);
    }

    private IEnumerator ResetBoolean(float t, Action b)
    {
        yield return new WaitForSeconds(t);
        b.Invoke();
    }

    private void OnDestroy()
    {
        ProfileBuilderService.Instance.Cleanup();
>>>>>>> Stashed changes
    }
}

public enum PaginationStates
{
    None,
    OnNextPage,
    OnPreviousPage,
    OnEOL
}