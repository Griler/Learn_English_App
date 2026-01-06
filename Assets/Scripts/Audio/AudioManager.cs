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

    public void StopVoice()
    {
        AudioSource audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    public void playVoiceWord(string word)
    {
       LoadWordAudio(word);
    }

    void LoadWordAudio(string word)
    {
        GoogleSpeechService.Instance.TextToSpeech(word,
        (clip =>
        {
            AudioSource audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
           audioSource.clip = clip;
           audioSource.Play();
        }),
        s => {
            Debug.LogError(s);
        });
    }
    
    public void SpeakToText(string text, string languageCode = "EN")
    {
        StartCoroutine(GetAudioFromGoogle(text, languageCode));
    }

    IEnumerator GetAudioFromGoogle(string text, string languageCode = "EN")
    {
        string textEncoded = UnityWebRequest.EscapeURL(text);


        string url = $"https://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&q={textEncoded}&tl={languageCode}";

        Debug.Log("Đang tải audio từ: " + url);

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Lỗi tải audio: " + www.error);
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                AudioSource audioSource = gameObject.GetComponent<AudioSource>();
                audioSource.clip = clip;
                audioSource.Play();
                Debug.Log("Phát audio thành công!");
            }
        }
    }
}