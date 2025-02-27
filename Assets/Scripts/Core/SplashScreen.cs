using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class SplashScreen : MonoBehaviour
{
    public VideoPlayer videoPlayer; // Drag & Drop VideoPlayer dari Inspector

    void Start()
    {
        // Path ke video di StreamingAssets
        string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, "SplashScreen.mp4");

        // Untuk Android, tambahkan "file://"
#if UNITY_ANDROID
        videoPath = "file://" + videoPath;
#endif

        // Set URL video
        videoPlayer.url = videoPath;
        videoPlayer.Play();

        // Pindah scene setelah video selesai
        videoPlayer.loopPointReached += LoadNextScene;
    }

    void LoadNextScene(VideoPlayer vp)
    {
        SceneManager.LoadScene("Tagline"); // Ganti dengan nama scene tujuan
    }
}
