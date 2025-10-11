using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public string appId = "20d829cb";
    public string appKey = "60a2cd21308b8fe620ad52be2cc2637a";
    public static AudioManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // giữ qua scene
    }

    public void playVoiceWord(string word)
    {
        StartCoroutine(LoadWordAudio(word));
    }

    IEnumerator LoadWordAudio(string word)
    {
        word = word.ToLower();
        string url = $"https://api.dictionaryapi.dev/media/pronunciations/en/{word}-us.mp3";
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                GetComponent<AudioSource>().clip = clip;
                GetComponent<AudioSource>().Play();
            }

            else
            {
                Debug.Log(www.error);
            }
        }
    }
}