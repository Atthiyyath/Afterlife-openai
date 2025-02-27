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
        [SerializeField] private TTSModel model = TTSModel.TTS_1;
        [SerializeField] private TTSVoice voice = TTSVoice.Alloy;
        [SerializeField, Range(0.25f, 4.0f)] private float speed = 1f;
        
        [SerializeField] private AudioLoader audioLoader; // Reference to AudioLoader
        
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
        }

        private void HandleTranscription(string text)
        {
            SendReply(text);
        }
        
        private void OnEnable()
        {
            if (!openAIWrapper) openAIWrapper = FindObjectOfType<OpenAIWrapper>();
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
                
                // Send text to TTS for playback
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
    
            if (openAIWrapper == null)
            {
                Debug.LogError("Missing OpenAIWrapper reference.");
                return;
            }
    
            byte[] audioData = await openAIWrapper.RequestTextToSpeech(text, model, voice, speed);
    
            if (audioData != null && audioData.Length > 0)
            {
                Debug.Log("Saving synthesized speech.");
                SaveAudioToFile(audioData, "TTS_Output.mp3"); // OpenAI gives MP3
                
                // Play audio using AudioLoader after saving
                if (audioLoader != null)
                {
                    audioLoader.LoadAndPlayAudio();
                }
                else
                {
                    Debug.LogError("AudioLoader component not found!");
                }
            }
            else
            {
                Debug.LogError("Failed to synthesize speech from OpenAI.");
            }
        }

        private void SaveAudioToFile(byte[] audioData, string fileName)
        {
            string directoryPath = Path.Combine(Application.persistentDataPath, "Audio");
            
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string filePath = Path.Combine(directoryPath, fileName);
            File.WriteAllBytes(filePath, audioData);
            Debug.Log($"Audio saved at: {filePath}");
        }
    }
}
