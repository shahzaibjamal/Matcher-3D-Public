using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.IO;
using System.Collections;
using Unity.EditorCoroutines.Editor;

public class GoogleSheetCSVDownloader
{
    private const string url = "https://script.google.com/macros/s/AKfycby4DumcMK00QJCTCiuNybLa6oe4sCft8Xcfr_SG-Qdns0cF-8j3dxJAMJ_tDi3dAbgD0Q/exec"; // Google Apps Script URL
    private const string savePath = "Assets/Editor/localization.csv";

    [MenuItem("Tools/Download Google Sheet CSV")]
    private static void DownloadCSV()
    {
        EditorCoroutineUtility.StartCoroutineOwnerless(FetchCSV());
    }

    private static IEnumerator FetchCSV()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                File.WriteAllText(savePath, www.downloadHandler.text);
                Debug.Log("CSV saved to " + savePath);
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogError("Error fetching CSV: " + www.error);
            }
        }
    }
}
