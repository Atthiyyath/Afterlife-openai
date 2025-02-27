using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CallPerson : MonoBehaviour
{
    public TextMeshProUGUI contactName;
    public TextMeshProUGUI gender;

    // ✅ Metode ini akan muncul di OnClick
    public void LoadVideoCallSceneFromButton()
    {
        if (contactName == null || gender == null)
        {
            Debug.LogError("[CallPerson] Error: Contact Name or Gender is not assigned.");
            return;
        }

        LoadVideoCallScene(contactName, gender);
    }

    private void LoadVideoCallScene(TextMeshProUGUI contactName, TextMeshProUGUI gender)
    {
        Debug.Log("[CallPerson] Attempting to load the VideoCall scene...");

        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            Debug.Log("[CallPerson] Microphone permission not granted. Requesting permission...");
            StartCoroutine(RequestMicrophonePermission(contactName, gender));
        }
        else
        {
            Debug.Log("[CallPerson] Microphone permission already granted. Proceeding to load scene.");
            LoadScene(contactName, gender);
        }
    }

    private IEnumerator RequestMicrophonePermission(TextMeshProUGUI contactName, TextMeshProUGUI gender)
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);

        if (Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            Debug.Log("[CallPerson] Microphone permission granted. Loading VideoCall scene.");
            LoadScene(contactName, gender);
        }
        else
        {
            Debug.LogWarning("[CallPerson] Microphone permission denied. Unable to proceed.");
        }
    }

    private void LoadScene(TextMeshProUGUI contactName, TextMeshProUGUI gender)
    {
        Debug.Log("[CallPerson] Saving contact details before loading VideoCall scene.");

        PlayerPrefs.SetString("ContactName", contactName.text);
        PlayerPrefs.SetString("Gender", gender.text);
        PlayerPrefs.Save();

        Debug.Log("[CallPerson] Contact details saved. Loading VideoCall scene...");
        Debug.Log($"[CallPerson] Contact Name: {contactName.text}, Gender: {gender.text}");
        SceneManager.LoadScene("Main");
    }

    public void MoveToListContact()
    {
        Debug.Log("[CallPerson] Navigating to Contact scene.");
        SceneManager.LoadScene("Contact");
    }
}
