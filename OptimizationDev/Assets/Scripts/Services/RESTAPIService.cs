using System;
using System.Collections;
using UnityEngine.Networking;

public class RESTAPIService : Singleton<RESTAPIService>
{
    public Config config;

    public IEnumerator SendRequestGetElements(int limit, int offset, Action<string> onSuccess, Action<string> onError)
    {
        if(config != null)
        {
            var url = BuildUrl(limit, offset);
            var request = UnityWebRequest.Get(url);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(request.downloadHandler.text);
            }

            else
            {
                onError?.Invoke(request.downloadHandler.error);
            }
        }
    }

    private string BuildUrl(int limit, int offset) => $"{config.titleURL}?limit={limit}&offset={offset}";
}