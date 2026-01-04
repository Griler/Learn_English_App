using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardItem : BaseCode
{
    public Image petImage;
    public TextMeshProUGUI vocabNameEn;
    public TextMeshProUGUI vocabNameVi;
    public TextMeshProUGUI vocabExamEn;
    public TextMeshProUGUI vocabExamVi;
    
    private string wordToVoiceExmaple = "";
    private string wordToVoiceVocabulary = "";
    
    public AudioSource audioSource;
    private void Start()
    {
        //setUpCard();
    }
    
    public void setUpCard(VocabItem word = null)
    {
        petImage.sprite = assetManager.getSpriteAnimal(word.text.en.ToLower());
        vocabNameEn.text = word.text.en;
        vocabNameVi.text = word.text.vi;
        wordToVoiceVocabulary = word.text.en;
    }
    public void setUpExample(VocabItem word = null)
    {
        vocabExamEn.text = word.example.en;
        vocabExamVi.text = word.example.vi;
        wordToVoiceExmaple = word.example.en;
    }

    public void playVoice()
    {
        LoadWordAudio(wordToVoiceVocabulary, playVoiceExmaple);
    }
    
    public void playVoiceExmaple()
    {
        LoadWordAudio(wordToVoiceExmaple);
    }
    
    // Coroutine xử lý chờ
    private IEnumerator PlayAndCallbackRoutine(AudioClip clip, Action onComplete)
    {
        // 1. Chơi âm thanh
        audioSource.PlayOneShot(clip);

        // 2. Chờ đúng bằng độ dài của clip
        yield return new WaitForSeconds(clip.length);

        // 3. Chạy callback (nếu có)
        onComplete?.Invoke();
    }
    
    void LoadWordAudio(string word, Action onComplete = null)
    {
        GoogleSpeechService.Instance.TextToSpeech(word,
            (clip =>
            {
                AudioSource audioSource = gameObject.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
                StartCoroutine(PlayAndCallbackRoutine(clip,onComplete));
            }),
            s => {
                Debug.LogError(s);
            });
    }
}