using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

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
    private float scrollRectSensivity = 0.05f;
    private float scrollRectSensivityUp = 0.95f;

    [Header("Elements Settings - Holders")]

    [SerializeField] private Button loadPreviousPanelsButton;
    [SerializeField] private ScrollRect panelsScrollRect;
    [SerializeField] private Vector2 panelsScrollRectPos;
    [SerializeField] private Transform pagesContainer;
    private AsyncOperationHandle<GameObject> handle = new();
    private List<AsyncOperationHandle<GameObject>> handles = new();


    public Config config;

    [Header("Other Settings - Cooldown Values")]

    [SerializeField] private int currentCooldownThresholdUnloadingPanels;
    [SerializeField] private int currentCooldownThresholdLoadingPanels;
    [SerializeField] private int maxCooldownThreshold;

    public bool isConnected = false;
    private bool isLoadingNext = false;
    private bool hasLoaded = false;
    private bool initLoad = false;

    private void Start()
    {
        loadPreviousPanelsButton.gameObject.SetActive(false);
        StartCoroutine(UnloadPanels(currentOffset, false));
    }

    private void OnEnable()
    {
        panelsScrollRect.onValueChanged.AddListener(OnScrollViewChanged);
        loadPreviousPanelsButton.onClick.AddListener(OnPreviousButtonClicked);
    }

    private void OnDisable()
    {
        if(panelsScrollRect != null && loadPreviousPanelsButton != null)
        {
            panelsScrollRect.onValueChanged.RemoveListener(OnScrollViewChanged);
            loadPreviousPanelsButton.onClick.RemoveListener(OnPreviousButtonClicked);
        }
    }

    private IEnumerator UnloadPanels(int offset, bool isScrollingUp)
    {
        if (offset > totalProfiles && panelsScrollRectPos.y < scrollRectSensivity) { }

        else
        {
            isLoadingNext = true;
            var url = BuildUrl(offset);
            UnityWebRequest request = UnityWebRequest.Get(url);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            Debug.Log($"Successfully sent a request to {config.titleURL}. Awaiting result.");

            ProfileDataFetch json = JsonUtility.FromJson<ProfileDataFetch>(request.downloadHandler.text);
            if (request.result == UnityWebRequest.Result.Success)
            {
                currentCooldownThresholdUnloadingPanels = 0;
                isConnected = true;
                initLoad = true;
                for (int i = 0; i < pagesContainer.childCount; i++)
                {
                    pagesContainer.GetChild(i).gameObject.SetActive(false);
                }

                StartCoroutine(SetupPanel(json.records));
                ShowPanel(false);
            }

            else if (string.IsNullOrEmpty(request.downloadHandler.text))
            {
                Debug.LogError("Empty JSON received");
                yield break;
            }


            else
            {
                Debug.LogError($"Unavailable to unload panels. Retrying again. {request.responseCode} : {request.error}");
                RetryUnloadingPanels();
            }

            StartCoroutine(ResetLoadingPanels(0.05f));
        }
    }

    private IEnumerator LoadPanels(int offset, bool isScrollingUp)
    {
        if (initLoad)
        {
            var url = BuildUrl(offset);
            var request = UnityWebRequest.Get(url);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            ProfileDataFetch json = JsonUtility.FromJson<ProfileDataFetch>(request.downloadHandler.text);

            if (request.result == UnityWebRequest.Result.Success)
            {
                currentCooldownThresholdLoadingPanels = 0;
                ShowPanel(true);

                for (int i = 0; i < pagesContainer.childCount; i++)
                {
                    bool shouldBeActive = (i >= 0 && i < panelsToLoad);
                    pagesContainer.GetChild(i).gameObject.SetActive(shouldBeActive);
                }
            }

            else if (string.IsNullOrEmpty(request.downloadHandler.text))
            {
                Debug.LogError("Empty JSON received");
                yield break;
            }

            else
            {
                Debug.LogError($"Unavailable to load panels. Retrying again. {request.responseCode} : {request.error}");
                RetryLoadingPanels();
            }

            StartCoroutine(ResetLoadingPanels(0.05f));
        }
    }


    private string BuildUrl(int offset) => $"{config.titleURL}?limit={panelsToLoad}&offset={offset}";

    void RetryUnloadingPanels()
    {
        currentCooldownThresholdUnloadingPanels++;

        if (maxCooldownThreshold > currentCooldownThresholdUnloadingPanels)
        {
            Debug.Log($"Unloading panels... Attempt {currentCooldownThresholdUnloadingPanels}.");
            StartCoroutine(UnloadPanels(currentOffset, false));
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
            StartCoroutine(LoadPanels(currentOffset, false));
        }
    }

    void OnScrollViewChanged(Vector2 scrollPos)
    {
        panelsScrollRectPos = scrollPos;

        if (!isLoadingNext && scrollPos.y <= scrollRectSensivity)
        {
            loadPreviousPanelsButton.gameObject.SetActive(false);
            StartCoroutine(UnloadPanels(currentOffset, false));
        }

        else if (!isLoadingNext && currentOffset > panelsToLoad && panelsScrollRect.verticalNormalizedPosition > scrollRectSensivityUp)
        {
            currentOffset = Mathf.Max(0, currentOffset);
            loadPreviousPanelsButton.gameObject.SetActive(true);
        }
    }

    void OnPreviousButtonClicked()
    {
        currentOffset = Mathf.Max(currentOffset - panelsToLoad, 0);
        isLoadingNext = true;
        Debug.Log($"Offset: {currentOffset}");
        StartCoroutine(LoadPanels(currentOffset, true));
    }


    private IEnumerator SetupPanel(List<Profile> totalRecords)
    {
        int batchSize = 9;
        int count = 0;

        if (initLoad && !hasLoaded && currentOffset >= panelsToLoad)
        {
            for (int i = currentOffset - panelsToLoad; i < currentOffset; i++)
            {
                if (pagesContainer.GetChild(i).gameObject.activeInHierarchy)
                {
                    pagesContainer.GetChild(i).gameObject.SetActive(false);
                }
            }
        }

        foreach (var menu in totalRecords)
        {
            if (!hasLoaded)
            {
                handle = Addressables.InstantiateAsync(profileAssetKey, pagesContainer);
                yield return handle;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject profile = handle.Result;
                    var profileInit = profile.GetComponent<MenuPageUI>();
                    profileInit.Init(menu);
                    handles.Add(handle);
                }

                count++;
                Canvas.ForceUpdateCanvases(); // rebuild the canvas immediately
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)pagesContainer); // rebuild the layout and the anchor pos of each element immediately

                if (count % batchSize == 0)
                {
                    yield return null;
                }
            }
        }

        hasLoaded = currentOffset >= totalProfiles - panelsToLoad;
        if(!isLoadingNext) currentOffset += totalRecords.Count;
    }

    private void ShowPanel(bool isScrollingUp)
    {
        if (isScrollingUp && currentOffset >= panelsToLoad && panelsScrollRectPos.y > scrollRectSensivityUp)
        {
            Debug.Log("Scrolling Up");
            loadPreviousPanelsButton.gameObject.SetActive(true);
            DeactivatePanelElements(currentOffset - panelsToLoad, currentOffset);
        }

        else if (currentOffset >= panelsToLoad && panelsScrollRectPos.y < scrollRectSensivityUp && !isScrollingUp)
        {
            Debug.Log("Scrolling Down");
            loadPreviousPanelsButton.gameObject.SetActive(false);
            DeactivatePanelElements(currentOffset, currentOffset + panelsToLoad);
        }
    }

    private void DeactivatePanelElements(int start, int exit)
    {
        DeactivateAllElements();

        try
        {
            for (int i = start; i < exit; i++)
            {
                pagesContainer.GetChild(i).gameObject.SetActive(true);
            }
        }

        catch { return; }
    }

    private void DeactivateAllElements()
    {
        for(int i = 0; i < pagesContainer.childCount; i++)
        {
            pagesContainer.GetChild(i).gameObject.SetActive(false);
        }
    }
    private IEnumerator ResetLoadingPanels(float t)
    {
        yield return new WaitForSeconds(t);
        isLoadingNext = false;
    }

    private void OnApplicationQuit()
    {
        foreach (var h in handles)
        {
            Addressables.Release(h);
        }
    }
}