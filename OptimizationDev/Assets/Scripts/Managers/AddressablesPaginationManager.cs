using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

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
    [Header("Elements Settings - Predefined Values") ,Tooltip("Predefined Values For Loading Elements/Clamping Values/Scroll Rect Loading")]

    [SerializeField] private int panelsToLoad = 20;
    [SerializeField] private int totalProfiles;
    [SerializeField] private string profileAssetKey;
    [SerializeField] private int currentOffset;
    private int targetOffset;

    [Range(0f, 1f)] [SerializeField] private float scrollRectSensivity = 0.1f;
    [Range(0f, 1f)] [SerializeField] private float scrollRectSensivityUp = 0.95f;

    [Header("Elements Settings - Holders"), Tooltip("Holder References For UI Elements.")]

    [SerializeField] private Button loadPreviousPanelsButton;
    [SerializeField] private ScrollRect panelsScrollRect;
    [SerializeField] private Vector2 panelsScrollRectPos;
    [SerializeField] private Transform pagesContainer;
    private AsyncOperationHandle<GameObject> handle = new();
    private List<AsyncOperationHandle<GameObject>> handles = new();

    [Header("Other Settings - Cooldown Values")]

    [SerializeField] private int currentCooldownThresholdUnloadingPanels;
    [SerializeField] private int currentCooldownThresholdLoadingPanels;
    [SerializeField] private int maxCooldownThreshold;

    public bool isConnected = false;
    private bool isLoadingNext = false;
    private bool isLoadingPrevious = false;
    private bool hasLoaded = false;

    protected override void Awake()
    {
        base.Awake();

        // Cleanup
        foreach(var handle in handles)
        {
            Addressables.Release(handle);
        }
    }

    private void Start()
    {
        Debug.Log("[START] : Addressables Panel Manager : Unloading Panels Now.");
        Debug.Log($"pagesContainer: {pagesContainer}");
        Debug.Log($"loadPreviousPanelsButton: {loadPreviousPanelsButton}");
        Debug.Log($"panelsScrollRect: {panelsScrollRect}");

        if (pagesContainer == null || loadPreviousPanelsButton == null || panelsScrollRect == null)
        {
            Debug.LogError("Missing references. Assign the respected references currently stated null.");
            return;
        }

        currentOffset = 0;
        targetOffset = currentOffset;
        loadPreviousPanelsButton.gameObject.SetActive(false);
        StartCoroutine(LoadNextPage(currentOffset, false));
    }

    private void OnEnable()
    {
        if (panelsScrollRect != null && loadPreviousPanelsButton != null)
        {
            panelsScrollRect.onValueChanged.AddListener(OnScrollViewChanged);
            loadPreviousPanelsButton.onClick.AddListener(OnPreviousButtonClicked);
        }
    }
    private void OnDisable()
    {
        if(panelsScrollRect != null && loadPreviousPanelsButton != null)
        {
            panelsScrollRect.onValueChanged.RemoveListener(OnScrollViewChanged);
            loadPreviousPanelsButton.onClick.RemoveListener(OnPreviousButtonClicked);
        }
    }

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

                        StartCoroutine(SetupPanel(json.records, () => // if setup is finished, then increase current offset and reset loading next
                        {
                            currentOffset = Mathf.Min(currentOffset + panelsToLoad, totalProfiles);
                            isLoadingNext = false;
                            if (currentOffset > panelsToLoad) { ResetCurrentOffsetOutOfRange(0); }
                            Debug.Log($"[Setup Panel] : Builded UI page successfully.");
                            targetOffset = currentOffset;
                        }));

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
                    RetryUnloadingPanels();
                }

            ));
    }

    private IEnumerator LoadPreviousPage(int offset, bool isScrollingUp)
    {
        yield return new WaitForEndOfFrame();

        try
        {
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
            RetryLoadingPanels();
        }
    }

    // ERROR HANDLING for loading pages
    void RetryUnloadingPanels()
    {
        currentCooldownThresholdUnloadingPanels++;

        if (maxCooldownThreshold > currentCooldownThresholdUnloadingPanels)
        {
            Debug.Log($"[RetryUnloadingPanels] Unloading panels... Attempt {currentCooldownThresholdUnloadingPanels}.");
            StartCoroutine(LoadNextPage(currentOffset, false));
        }

        else
        {
            Debug.Log($"Unavailable to load panels. Please check your connection and try again.");
            return;
        }
    }

    void RetryLoadingPanels()
    {
        currentCooldownThresholdLoadingPanels++;

        if (maxCooldownThreshold > currentCooldownThresholdLoadingPanels)
        {
            Debug.Log($"Loading panels... Attempt {currentCooldownThresholdLoadingPanels}");
            StartCoroutine(LoadPreviousPage(currentOffset, false));
        }
    }

    void ResetCurrentOffsetOutOfRange(int startLimit)
    {
        try
        {
            for (int i = 0; i < pagesContainer.childCount; i++)
            {
                if (pagesContainer.GetChild(i).gameObject.activeSelf)
                {
                    startLimit++;
                }
            }

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


    private IEnumerator SetupPanel(List<Profile> totalRecords, Action onComplete) 
    {
        if (currentOffset >= totalProfiles) { yield break; }

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
        hasLoaded = currentOffset >= totalProfiles;
        onComplete?.Invoke();
    }

    private IEnumerator ResetBoolean(float t, Action b)
    {
        yield return new WaitForSeconds(t);
        b.Invoke();
    }

    // ---------------------------------------------------------------------

    private void OnApplicationQuit()
    {
        // Cleanup

        foreach (var h in handles)
        {
            Addressables.Release(h);
        }
    }
}