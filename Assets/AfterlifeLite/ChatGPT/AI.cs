using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Samples.Whisper;

namespace OpenAI
{
    public class AI : MonoBehaviour
    {
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private Stt stt;
        [SerializeField] private RectTransform sent;
        [SerializeField] private RectTransform received;

        private float height;
        private OpenAIApi openai = new OpenAIApi();
        private List<ChatMessage> messages = new List<ChatMessage>();
        private string prompt = "You're a deceased person and will help the sender to erase their longing, remember to act naturally";

        private void Start()
        {
            if (stt != null)
            {
                stt.OnTranscriptionComplete += HandleTranscription;
            }
        }

        private void HandleTranscription(string text)
        {
            SendReply(text);
        }

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
            }
            else
            {
                Debug.LogWarning("No response generated.");
            }
        }
    }
}
