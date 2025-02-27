using UnityEngine;
using System.IO;
using System.Collections;
using UnityEngine.Networking;

public class AudioLoader : MonoBehaviour
{
    public string audioFileName = "TTS_Output"; // Filename without extension
    private AudioClip audioClip;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>(); // Get the AudioSource component
    }

    public void LoadAndPlayAudio()
    {
        StartCoroutine(LoadAudioFromPersistentPath());
    }

    private IEnumerator LoadAudioFromPersistentPath()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "Audio", audioFileName + ".mp3");

        if (!File.Exists(filePath))
        {
            Debug.LogError("Audio file not found: " + filePath);
            yield break;
        }

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                audioClip = DownloadHandlerAudioClip.GetContent(www);
                PlayAudio(audioClip);
            }
            else
            {
                Debug.LogError("Failed to load audio: " + www.error);
            }
        }
    }

    private void PlayAudio(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
            Debug.Log("Playing Audio: " + clip.name);
        }
        else
        {
            Debug.LogError("AudioSource or AudioClip is null!");
        }
    }
}