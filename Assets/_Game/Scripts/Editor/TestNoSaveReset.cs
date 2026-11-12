using UnityEditor;

namespace StratejiGame.EditorTools
{
    /// <summary>
    /// Test modu ("StratejiGame.TestNoSave": oyun kaydı diske yazılmaz) yalnızca tek bir oyun oturumu sürer.
    /// Oyun modundan çıkılınca kendiliğinden kapanır; böylece bir sonraki oynayışta ilerleme yine kaydedilir.
    /// </summary>
    [InitializeOnLoad]
    static class TestNoSaveReset
    {
        static TestNoSaveReset()
        {
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.EnteredEditMode) SessionState.SetBool("StratejiGame.TestNoSave", false);
            };
        }
    }
}
