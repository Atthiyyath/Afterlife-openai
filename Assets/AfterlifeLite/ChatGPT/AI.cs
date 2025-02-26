using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Samples.Whisper;
using System.IO;
using UnityEngine.Networking;
using System.Threading.Tasks;

namespace OpenAI
{
    public class AI : MonoBehaviour
    {
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private Stt stt;
        [SerializeField] private RectTransform sent;
        [SerializeField] private RectTransform received;
        
        private OpenAIWrapper openAIWrapper;
        [SerializeField] private TTSManager ttsManager;
        [SerializeField] private AudioPlayer audioPlayer;
        [SerializeField] private TTSModel model = TTSModel.TTS_1;
        [SerializeField] private TTSVoice voice = TTSVoice.Alloy;
        [SerializeField, Range(0.25f, 4.0f)] private float speed = 1f;
        
        [SerializeField] private AudioLoader audioLoader;

        private float height;
        private OpenAIApi openai = new OpenAIApi();
        private List<ChatMessage> messages = new List<ChatMessage>();
        private string prompt = "You're a deceased person and will help the sender to erase their longing, remember to act naturally and answer shortly";

        private void Start()
        {
            if (stt != null)
            {
                stt.OnTranscriptionComplete += HandleTranscription;
            }
            
            if (!openAIWrapper) openAIWrapper = FindObjectOfType<OpenAIWrapper>();
            if (!audioPlayer) audioPlayer = GetComponentInChildren<AudioPlayer>();
        }

        private void HandleTranscription(string text)
        {
            SendReply(text);
        }
        
        private void OnEnable()
        {
            if (!openAIWrapper) openAIWrapper = FindObjectOfType<OpenAIWrapper>();
            if (!audioPlayer) audioPlayer = GetComponentInChildren<AudioPlayer>();
        }
        
        private void OnValidate() => OnEnable();

        private void AppendMessage(ChatMessage message)
        {
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0);
            var item = Instantiate(message.Role == "user" ? sent : received, scroll.content);
            item.GetChild(0).GetChild(0).GetComponent<Text>().text = message.Content;
            item.anchoredPosition = new Vector2(0, -height);
            LayoutRebuilder.ForceRebuildLayoutImmediate(item);
            height += item.sizeDelta.y;
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            scroll.verticalNormalizedPosition = 0;
        }

        private async void SendReply(string userInput)
        {
            var newMessage = new ChatMessage() { Role = "user", Content = userInput };
            AppendMessage(newMessage);
            if (messages.Count == 0) newMessage.Content = prompt + "\n" + userInput;
            messages.Add(newMessage);

            var completionResponse = await openai.CreateChatCompletion(new CreateChatCompletionRequest()
            {
                Model = "gpt-4o-mini",
                Messages = messages
            });

            if (completionResponse.Choices != null && completionResponse.Choices.Count > 0)
            {
                var message = completionResponse.Choices[0].Message;
                message.Content = message.Content.Trim();
                messages.Add(message);
                AppendMessage(message);
                
                // Ensure text-to-speech conversion for AI responses
                ConvertTextToSpeech(message.Content);
            }
            else
            {
                Debug.LogWarning("No response generated.");
            }
        }
        
        public async void ConvertTextToSpeech(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
    
            Debug.Log("Trying to synthesize: " + text);
    
            if (openAIWrapper == null || audioPlayer == null)
            {
                Debug.LogError("Missing OpenAIWrapper or AudioPlayer reference.");
                return;
            }
    
            byte[] audioData = await openAIWrapper.RequestTextToSpeech(text, model, voice, speed);
    
            if (audioData != null && audioData.Length > 0)
            {
                Debug.Log("Playing synthesized speech.");
                //audioPlayer.ProcessAudioBytes(audioData);

                // Save the audio file
                SaveAudioToFile(audioData, "TTS_Output.wav");
            }
            else
            {
                Debug.LogError("Failed to synthesize speech from OpenAI.");
            }
        }

        private void SaveAudioToFile(byte[] audioData, string fileName)
{
    string directoryPath;
    
    #if UNITY_EDITOR
        directoryPath = Path.Combine(Application.dataPath, "Resources/Audio"); // Editor
    #else
        directoryPath = Path.Combine(Application.persistentDataPath, "Audio"); // Android
    #endif

    if (!Directory.Exists(directoryPath))
    {
        Directory.CreateDirectory(directoryPath);
    }

    string filePath = Path.Combine(directoryPath, fileName);
    File.WriteAllBytes(filePath, audioData);
    Debug.Log($"Audio saved at: {filePath}");

    #if UNITY_EDITOR
        UnityEditor.AssetDatabase.ImportAsset("Assets/Resources/Audio/" + fileName);
        UnityEditor.AssetDatabase.Refresh();
    #endif

    // Load and play the audio
    Invoke("LoadAndPlayAudio", 1.0f);
}

public void LoadAndPlayAudio()
{
    #if UNITY_EDITOR
        // Load from Resources folder in the Editor
        AudioLoader audioLoader = FindObjectOfType<AudioLoader>();
        if (audioLoader != null)
        {
            audioLoader.LoadAndPlayAudio();
        }
        else
        {
            Debug.LogError("AudioLoader component not found!");
        }
    #else
        // Load from persistentDataPath on Android
        StartCoroutine(LoadAudioFromFile());
    #endif
}

private IEnumerator LoadAudioFromFile()
{
    string filePath = Path.Combine(Application.persistentDataPath, "Audio/TTS_Output.wav");

    if (!File.Exists(filePath))
    {
        Debug.LogError("Audio file not found!");
        yield break;
    }

    using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.WAV))
    {
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);

            AudioLoader audioLoader = FindObjectOfType<AudioLoader>();
            if (audioLoader != null && audioLoader.TryGetComponent(out AudioSource audioSource))
            {
                audioSource.clip = clip;
                audioSource.Play();
            }
            else
            {
                Debug.LogError("AudioLoader or AudioSource component missing!");
            }
        }
        else
        {
            Debug.LogError("Failed to load audio: " + www.error);
        }
    }
}
    }
}
