using System;
using UnityEngine;
using System.Collections;
public class RetryLoaderService : Singleton<RetryLoaderService>
{
    // <summary> : ERROR HANDLING for loading pages
    public void RetryLoadingPanels(ref int currentThreshold, int maxThreshold, int currentOffset, Func<int, bool, IEnumerator> operationToRun)
    {
        currentThreshold++;

        if (maxThreshold > currentOffset)
        {
            Debug.Log($"[RetryUnloadingPanels] Unloading panels... Attempt {currentOffset}.");
            StartCoroutine(operationToRun(currentOffset, false));
        }

        else
        {
            Debug.Log($"Unavailable to load panels. Please check your connection and try again.");
            return;
        }
    }
}