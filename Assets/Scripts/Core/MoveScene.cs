using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MoveScene : MonoBehaviour
{
    [SerializeField] private string sceneName = "Contact"; // Nama scene yang bisa diatur di Inspector
    [SerializeField] private float delay = 2f; // Delay sebelum berpindah scene

    void Start()
    {
        Debug.Log("Game started! Calling ChangeeScene...");
        ChangeScene();
    }

    public void ChangeScene()
    {
        Debug.Log("ChangeScene() called! Scene: " + sceneName + ", Delay: " + delay + "s");
        StartCoroutine(LoadSceneAfterDelay());
    }

    private IEnumerator LoadSceneAfterDelay()
    {
        Debug.Log("Waiting for " + delay + " seconds...");
        yield return new WaitForSeconds(delay);
        Debug.Log("Time's up! Loading scene: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }
}
