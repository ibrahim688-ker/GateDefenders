using UnityEditor;
using UnityEditor.SceneManagement;

namespace StratejiGame.EditorTools
{
    /// <summary>
    /// Editörde Play'e basınca hangi sahne açık olursa olsun oyun, telefondaki gibi Yükleniyor sahnesinden başlar
    /// (Yükleniyor → Ana menü). Editör her açılışta / derlemede yeniden ayarlar.
    /// </summary>
    [InitializeOnLoad]
    static class PlayFromLoading
    {
        const string LoadingScene = "Assets/_Game/Scenes/Loading.unity";

        static PlayFromLoading()
        {
            EditorApplication.delayCall += () =>
            {
                var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(LoadingScene);
                if (scene != null) EditorSceneManager.playModeStartScene = scene;
            };
        }
    }
}
