using OpenAI;
using UnityEngine;
using UnityEngine.UI;

namespace Samples.Whisper
{
    public class Whisper : MonoBehaviour
    {
        [SerializeField] private Button recordButton;
        [SerializeField] private Image progressBar;
        [SerializeField] private Text message;
        [SerializeField] private Dropdown dropdown;
        
        private readonly string fileName = "output.wav";
        private readonly int duration = 5;
        
        private AudioClip clip;
        private bool isRecording;
        private float time;
        private OpenAIApi openai = new OpenAIApi();

        private void Start()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            dropdown.options.Add(new Dropdown.OptionData("Microphone not supported on WebGL"));
            #else
            foreach (var device in Microphone.devices)
            {
                dropdown.options.Add(new Dropdown.OptionData(device));
            }
            recordButton.onClick.AddListener(StartRecording);
            dropdown.onValueChanged.AddListener(ChangeMicrophone);
            
            var index = PlayerPrefs.GetInt("user-mic-device-index");
            dropdown.SetValueWithoutNotify(index);
            #endif
        }

        private void ChangeMicrophone(int index)
        {
            PlayerPrefs.SetInt("user-mic-device-index", index);
        }
        
        private void StartRecording()
        {
            isRecording = true;
            recordButton.enabled = false;

            var index = PlayerPrefs.GetInt("user-mic-device-index");
            
            #if !UNITY_WEBGL
            clip = Microphone.Start(dropdown.options[index].text, false, duration, 44100);
            #endif
        }

        private async void EndRecording()
        {
            message.text = "Transcripting...";
            /*
            #if !UNITY_WEBGL
            Microphone.End(null);
            #endif
            */

            isRecording = false;
            Microphone.End(null);
            
            if (clip == null)
            {
                Debug.LogError("Audio clip is null! No recording data available.");
                return;
            }

            if (clip.samples == 0)
            {
                Debug.LogError("Audio clip has no samples! Recording might have failed.");
                return;
            }
            
            // Save the recorded audio to a file
            string path = Application.persistentDataPath + "/" + fileName;
            byte[] data = SaveWav.Save(fileName, clip);
            
            // Manually save the file
            System.IO.File.WriteAllBytes(path, data);
            Debug.Log("Audio file saved at: " + path);
            
            //var req = new CreateAudioTranscriptionsRequest --> for request to transcript the audio <--
            var req = new CreateAudioTranslationRequest // -- for request to translate the audio
            {
                FileData = new FileData() {Data = data, Name = "audio.wav"},
                // File = Application.persistentDataPath + "/" + fileName,
                Model = "whisper-1",
                //Language = "en" --> uncomment this if you only wanna transcript the audio <--
            };
            //var res = await openai.CreateAudioTranscription(req); --> for request to transcript the audio <--
            var res = await openai.CreateAudioTranslation(req); // -- for request to translate the audio

            //progressBar.fillAmount = 0;
            message.text = res.Text;
            //recordButton.enabled = true;
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
