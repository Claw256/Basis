using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BasisAssetBundlePipeline
{
    // Define static delegates
    public delegate void BeforeBuildGameobjectHandler(GameObject prefab, BasisAssetBundleObject settings);
    public delegate void BeforeBuildSceneHandler(Scene prefab, BasisAssetBundleObject settings);
    public delegate void AfterBuildHandler(string assetBundleName);
    public delegate void BuildErrorHandler(Exception ex, GameObject prefab, bool wasModified, string temporaryStorage);

    // Static delegates
    public static BeforeBuildGameobjectHandler OnBeforeBuildPrefab;
    public static AfterBuildHandler OnAfterBuildPrefab;
    public static BuildErrorHandler OnBuildErrorPrefab;

    public static BeforeBuildSceneHandler OnBeforeBuildScene;
    public static AfterBuildHandler OnAfterBuildScene;
    public static BuildErrorHandler OnBuildErrorScene;

    public static async void BuildAssetBundle(GameObject prefab, BasisAssetBundleObject settings, BasisBundleInformation BasisBundleInformation, string Password)
    {
        TemporaryStorageHandler.ClearTemporaryStorage(settings.AssetBundleDirectory);
        TemporaryStorageHandler.EnsureDirectoryExists(settings.AssetBundleDirectory);

        if (!BasisValidationHandler.IsValidPrefab(prefab))
        {
            Debug.LogError("Invalid prefab. AssetBundle build aborted.");
            return;
        }

        bool wasModified = false;

        try
        {
            // Invoke the delegate before building the asset bundle
            OnBeforeBuildPrefab?.Invoke(prefab, settings);

            string prefabPath = TemporaryStorageHandler.SavePrefabToTemporaryStorage(prefab, settings, ref wasModified, out string uniqueID);
            string assetBundleName = AssetBundleBuilder.SetAssetBundleName(prefabPath, uniqueID, settings);

           await AssetBundleBuilder.BuildAssetBundle(settings, assetBundleName, BasisBundleInformation, nameof(GameObject), Password);
            AssetBundleBuilder.ResetAssetBundleName(prefabPath);
            TemporaryStorageHandler.ClearTemporaryStorage(settings.TemporaryStorage);
            AssetDatabase.Refresh();

            // Invoke the delegate after building the asset bundle
            OnAfterBuildPrefab?.Invoke(assetBundleName);
        }
        catch (Exception ex)
        {
            // Handle the build error
            OnBuildErrorPrefab?.Invoke(ex, prefab, wasModified, settings.TemporaryStorage);
            BasisBundleErrorHandler.HandleBuildError(ex, prefab, wasModified, settings.TemporaryStorage);
            EditorUtility.DisplayDialog("Failed To Build", "please check the console for the full issue, " + ex, "will do");
        }
        EditorUtility.DisplayDialog("Completed Build", "successfully built asset bundles for assets, Will be found in ./AssetBundles", "ok");
    }

    public static async void BuildAssetBundle(Scene scene, BasisAssetBundleObject settings, BasisBundleInformation BasisBundleInformation, string Password)
    {
        TemporaryStorageHandler.ClearTemporaryStorage(settings.AssetBundleDirectory);
        TemporaryStorageHandler.EnsureDirectoryExists(settings.AssetBundleDirectory);

        if (!BasisValidationHandler.IsSceneValid(scene))
        {
            Debug.LogError("Invalid scene. AssetBundle build aborted.");
            return;
        }

        string tempScenePath = null;

        try
        {
            // Invoke the delegate before building the asset bundle
            OnBeforeBuildScene?.Invoke(scene, settings);

            tempScenePath = TemporaryStorageHandler.SaveSceneToTemporaryStorage(scene, settings, out string uniqueID);
            string assetBundleName = AssetBundleBuilder.SetAssetBundleName(tempScenePath, uniqueID, settings);

           await AssetBundleBuilder.BuildAssetBundle(settings, assetBundleName, BasisBundleInformation, nameof(Scene), Password);
            TemporaryStorageHandler.ClearTemporaryStorage(tempScenePath);

            // Invoke the delegate after building the asset bundle
            OnAfterBuildScene?.Invoke(assetBundleName);
        }
        catch (Exception ex)
        {
            // Handle the build error
            OnBuildErrorScene?.Invoke(ex, null, false, settings.TemporaryStorage); // Pass `null` for prefab since this is a scene
            Debug.LogError($"Error while building AssetBundle from scene: {ex.Message}\n{ex.StackTrace}");
        }
    }
}