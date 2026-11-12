using System.IO;
using UnityEditor.Android;
using UnityEngine;

namespace StratejiGame.EditorTools
{
    /// <summary>
    /// Oyun internet kullanmıyor (reklam, analiz, sunucu yok). Unity, dahili modüller yüzünden Android manifestine
    /// INTERNET iznini kendiliğinden ekliyor; bu adım Gradle projesi üretildikten sonra o izni kaldırır.
    /// Böylece oyun cihazdan dışarı veri gönderemez (gizlilik politikası ve Data Safety beyanı: veri toplanmıyor).
    /// İleride reklam / analiz eklenirse bu dosya silinmeli ve beyanlar güncellenmelidir.
    /// </summary>
    public class RemoveInternetPermission : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 999;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // path: .../Gradle/unityLibrary
            string manifest = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifest)) { Debug.LogWarning("RemoveInternetPermission: manifest bulunamadı: " + manifest); return; }
            string text = File.ReadAllText(manifest);
            string line = "<uses-permission android:name=\"android.permission.INTERNET\" />";
            if (!text.Contains(line)) return;
            File.WriteAllText(manifest, text.Replace(line, ""));
            Debug.Log("RemoveInternetPermission: INTERNET izni manifestten kaldırıldı.");
        }
    }
}
