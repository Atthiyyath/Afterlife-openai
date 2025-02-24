using UnityEngine;

public class AudioLoader : MonoBehaviour
{
    public string audioFileName = "TTS_Output"; // Set the file name (without extension)
    private AudioClip audioClip;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>(); // Get the AudioSource component
        //LoadAndPlayAudio();
    }

    public void LoadAndPlayAudio()
    {
        // Load the audio clip from Resources/Audio/
        audioClip = Resources.Load<AudioClip>("Audio/" + audioFileName);

        if (audioClip != null)
        {
            Debug.Log("Loaded Audio: " + audioClip.name);
            audioSource.clip = audioClip; // Assign the clip to the AudioSource
            audioSource.Play(); // Play the audio
        }
        else
        {
            Debug.LogWarning("Audio file not found: " + audioFileName);
        }
    }
}