using System.IO;
using System.Text;
using UnityEngine;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Collections;

public class OpenAIWrapper : MonoBehaviour
{
    private string openAIKey; // Stores the OpenAI API key
    private readonly string outputFormat = "mp3";

    private void Start()
    {
        StartCoroutine(LoadAPIKey());
    }

    private IEnumerator LoadAPIKey()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "config.json");

        if (path.Contains("://") || path.Contains("jar:")) // Handle Android/iOS
        {
            using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(path))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    string json = request.downloadHandler.text;
                    ProcessAPIKey(json);
                }
                else
                {
                    Debug.LogError("❌ Failed to load API Key: " + request.error);
                }
            }
        }
        else // Handle Windows/macOS/Editor
        {
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                ProcessAPIKey(json);
            }
            else
            {
                Debug.LogError("❌ API Key file missing! Please create 'config.json' in StreamingAssets.");
            }
        }
    }

    private void ProcessAPIKey(string json)
    {
        APIKeyData data = JsonUtility.FromJson<APIKeyData>(json);
        openAIKey = data?.openAIKey;

        if (string.IsNullOrEmpty(openAIKey))
        {
            Debug.LogError("❌ API Key is empty! Check config.json.");
        }
        else
        {
            Debug.Log($"✅ API Key Loaded: {openAIKey.Substring(0, 5)}... (truncated for security)");
        }
    }

    [System.Serializable]
    private class APIKeyData { public string openAIKey; }

    [System.Serializable]
    private class TTSPayload
    {
        public string model;
        public string input;
        public string voice;
        public string response_format;
        public float speed;
    }

    public async Task<byte[]> RequestTextToSpeech(string text, TTSModel model = TTSModel.TTS_1, TTSVoice voice = TTSVoice.Alloy, float speed = 1f)
    {
        if (string.IsNullOrEmpty(openAIKey))
        {
            Debug.LogError("❌ OpenAI API key is missing! Cannot proceed with request.");
            return null;
        }

        Debug.Log("📡 Sending new request to OpenAI TTS.");
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openAIKey);

        TTSPayload payload = new TTSPayload
        {
            model = model.EnumToString(),
            input = text,
            voice = voice.ToString().ToLower(),
            response_format = this.outputFormat,
            speed = speed
        };

        string jsonPayload = JsonUtility.ToJson(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        try
        {
            HttpResponseMessage httpResponse = await httpClient.PostAsync("https://api.openai.com/v1/audio/speech", content);

            if (httpResponse.IsSuccessStatusCode)
            {
                return await httpResponse.Content.ReadAsByteArrayAsync();
            }
            else
            {
                string errorResponse = await httpResponse.Content.ReadAsStringAsync();
                Debug.LogError($"❌ OpenAI API Error: {httpResponse.StatusCode}\n{errorResponse}");
                return null;
            }
        }
        catch (HttpRequestException e)
        {
            Debug.LogError("❌ Network Error: " + e.Message);
            return null;
        }
    }

    public void SetAPIKey(string newKey)
    {
        openAIKey = newKey;
        Debug.Log("✅ API Key updated manually.");
    }
}
