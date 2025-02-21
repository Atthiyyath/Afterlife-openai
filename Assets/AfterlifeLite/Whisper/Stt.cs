using OpenAI;
using UnityEngine;
using UnityEngine.UI;

namespace Samples.Whisper
{
    public class Stt : MonoBehaviour
    {
        [SerializeField] private Button recordButton;
        [SerializeField] private Image progressBar;
        
        private readonly string fileName = "output.wav";
        private readonly int duration = 5;
        
        private AudioClip clip;
        private bool isRecording;
        private float time;
        private OpenAIApi openai = new OpenAIApi();
        private string selectedMicrophone;
        
        public delegate void TranscriptionHandler(string text);
        public event TranscriptionHandler OnTranscriptionComplete;

        private void Start()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            Debug.LogError("Microphone not supported on WebGL");
            #else
            if (Microphone.devices.Length > 0)
            {
                selectedMicrophone = Microphone.devices[0]; // Auto-select first available microphone
            }
            else
            {
                Debug.LogError("No microphone detected!");
            }
            recordButton.onClick.AddListener(StartRecording);
            #endif
        }

        private void StartRecording()
        {
            if (string.IsNullOrEmpty(selectedMicrophone))
            {
                Debug.LogError("No microphone available to start recording.");
                return;
            }
            isRecording = true;
            recordButton.enabled = false;
            progressBar.fillAmount = 0;
            time = 0;
            #if !UNITY_WEBGL
            clip = Microphone.Start(selectedMicrophone, false, duration, 44100);
            #endif
        }

        private async void EndRecording()
        {
            isRecording = false;
            Microphone.End(selectedMicrophone);
            recordButton.enabled = true;
            progressBar.fillAmount = 0;
            
            if (clip == null || clip.samples == 0)
            {
                Debug.LogError("Recording failed or no audio captured.");
                return;
            }
            
            string path = Application.persistentDataPath + "/" + fileName;
            byte[] data = SaveWav.Save(fileName, clip);
            System.IO.File.WriteAllBytes(path, data);
            
            var req = new CreateAudioTranslationRequest
            {
                FileData = new FileData() {Data = data, Name = "audio.wav"},
                Model = "whisper-1",
            };
            
            var res = await openai.CreateAudioTranslation(req);
            OnTranscriptionComplete?.Invoke(res.Text);
        }

        private void Update()
        {
            if (isRecording)
            {
                time += Time.deltaTime;
                progressBar.fillAmount = time / duration;
                
                if (time >= duration)
                {
                    time = 0;
                    isRecording = false;
                    EndRecording();
                }
            }
        }
    }
}
