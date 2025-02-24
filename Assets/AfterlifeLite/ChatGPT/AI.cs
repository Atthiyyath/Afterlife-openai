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
            string relativePath = "Assets/Resources/Audio/";
            string filePath = Path.Combine(relativePath, fileName);

            // Ensure the directory exists
            if (!Directory.Exists(relativePath))
            {
                Directory.CreateDirectory(relativePath);
            }

            // Write the file
            File.WriteAllBytes(filePath, audioData);
            Debug.Log($"Audio saved at: {filePath}");

            // Refresh the Unity Editor to detect the new file
            #if UNITY_EDITOR
            UnityEditor.AssetDatabase.ImportAsset(filePath);
            UnityEditor.AssetDatabase.Refresh();
            #endif

            // Load and play the audio
            Invoke("LoadAndAssignAudio", 1.0f);
        }

        private void LoadAndAssignAudio()
        {
            Debug.Log("Play audio!");
            audioLoader.LoadAndPlayAudio();
        }
    }
}
