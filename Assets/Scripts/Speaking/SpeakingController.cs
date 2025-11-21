using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.SceneManagement; // Nếu bạn dùng DOTween

public class SpeakingController : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Button recordButton;
    [SerializeField] private Button speakButton;
    [SerializeField] private Button replayButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI transcriptText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI referenceTextInputEN; // Câu mẫu
    [SerializeField] private TextMeshProUGUI referenceTextInputVI; // Câu mẫu
    [SerializeField] private TextMeshProUGUI ttsTextInput; // Text để đọc
    [SerializeField] private Slider volumeSlider;

    [Header("Settings")]
    [SerializeField] private int recordingLength = 5;
    [SerializeField] private int recordingSampleRate = 16000;
    [SerializeField] private float silenceThreshold = 0.035f;
    [SerializeField] private float minValidLength = 0.5f;

    private AudioSource audioSource;
    private AudioClip recordedClip;
    private string micDeviceName;
    private bool isRecording = false;
    [SerializeField] private List<SentenceItem> listSentences = new List<SentenceItem>();
    void Start()
    {
        // Setup AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Setup Buttons
        if (recordButton) recordButton.onClick.AddListener(OnRecordToggle);
        if (speakButton) speakButton.onClick.AddListener(OnSpeakClicked);
        if (replayButton) replayButton.onClick.AddListener(OnReplayClicked);
        string currentTopic = PlayerPrefs.GetString("CurrentSpeakingTopic");

        GetSentencesByTopic(currentTopic, initProp);
        UpdateStatus("Ready");
    }

    void Update()
    {
        if (isRecording)
        {
            UpdateVolumeMeter();
        }
        else
        {
             // Reset slider từ từ về 0
             if (volumeSlider && volumeSlider.value > 0)
                volumeSlider.value = Mathf.Lerp(volumeSlider.value, 0, Time.deltaTime * 10);
        }
    }

    // ##################################################################
    // ## RECORDING LOGIC
    // ##################################################################

    private void OnRecordToggle()
    {
        if (!isRecording) StartRecording();
        else StopRecording();
    }

    private void StartRecording()
    {
        if (Microphone.devices.Length == 0)
        {
            UpdateStatus("No microphone found!");
            return;
        }

        micDeviceName = Microphone.devices[0];
        recordedClip = Microphone.Start(micDeviceName, false, recordingLength, recordingSampleRate);
        isRecording = true;
        string currentTopic = PlayerPrefs.GetString("CurrentSpeakingTopic");
            
        UpdateStatus("Recording...");
        if (recordButton) recordButton.GetComponentInChildren<TextMeshProUGUI>().text = "Stop";
    }

    private void StopRecording()
    {
        if (!isRecording) return;

        Microphone.End(micDeviceName);
        isRecording = false;
        if (recordButton) recordButton.GetComponentInChildren<TextMeshProUGUI>().text = "Record";

        if (!IsAudioValid(recordedClip)) return;

        UpdateStatus("Processing STT...");
        
        // Gọi qua Service
        GoogleSpeechService.Instance.SpeechToText(
            recordedClip, 
            OnSTTSuccess, 
            OnApiError
        );
    }

    private bool IsAudioValid(AudioClip clip)
    {
        if (clip == null || clip.length < minValidLength)
        {
            UpdateStatus("Recording too short.");
            return false;
        }
        
        // Check silence logic (Simplified)
        float[] samples = new float[128];
        clip.GetData(samples, 0);
        float maxAmp = 0;
        foreach (var s in samples) if (Mathf.Abs(s) > maxAmp) maxAmp = Mathf.Abs(s);
        
        if (maxAmp < silenceThreshold)
        {
            UpdateStatus("Too quiet. Speak louder.");
            return false;
        }
        return true;
    }

    private void UpdateVolumeMeter()
    {
        if (!volumeSlider) return;
        
        int micPos = Microphone.GetPosition(micDeviceName) - 128;
        if (micPos < 0) return;

        float[] samples = new float[128];
        recordedClip.GetData(samples, micPos);
        
        float sum = 0;
        foreach (var s in samples) sum += Mathf.Abs(s);
        float avg = sum / 128f;

        volumeSlider.value = Mathf.Lerp(volumeSlider.value, avg * 10f, 0.5f);
        
        // Đổi màu slider dựa trên độ lớn (Optional)
        Color c = avg < 0.02f ? Color.red : (avg < 0.08f ? Color.yellow : Color.green);
        if(volumeSlider.targetGraphic) volumeSlider.targetGraphic.color = c;
    }

    // ##################################################################
    // ## TTS LOGIC
    // ##################################################################

    public void OnSpeakClicked()
    {
        string text = ttsTextInput ? ttsTextInput.text : "Hello World";
        UpdateStatus("Synthesizing TTS...");
        
        GoogleSpeechService.Instance.TextToSpeech(
            text,
            (clip) => {
                UpdateStatus("Playing TTS...");
                audioSource.clip = clip;
                audioSource.Play();
            },
            OnApiError
        );
    }

    private void OnReplayClicked()
    {
        if (recordedClip)
        {
            audioSource.clip = recordedClip;
            audioSource.Play();
            UpdateStatus("Replaying...");
        }
    }

    // ##################################################################
    // ## CALLBACKS & SCORING
    // ##################################################################

    private void OnSTTSuccess(string transcript, float confidence)
    {
        if (transcriptText) transcriptText.text = $"You said: {transcript}";
        
        // Tính điểm
        string reference = referenceTextInputEN ? referenceTextInputEN.text : "";
        float score = CalculateScore(transcript, reference, confidence);
        
        if (scoreText)
        {
            scoreText.text = $"Score: {score:F1}/100";
            scoreText.color = score > 80 ? Color.green : (score > 50 ? Color.yellow : Color.red);
        }
        
        UpdateStatus("Done!");
    }

    private void OnApiError(string error)
    {
        UpdateStatus($"Error: {error}");
        Debug.LogError(error);
    }

    private void UpdateStatus(string msg)
    {
        if (statusText) statusText.text = msg;
        Debug.Log($"[Status] {msg}");
    }

    // --- Scoring Logic (Chuyển từ Service sang) ---
    private float CalculateScore(string transcript, string reference, float confidence)
    {
        if (string.IsNullOrEmpty(reference)) return 0;

        transcript = transcript.ToLower().Trim();
        reference = reference.ToLower().Trim();

        int distance = LevenshteinDistance(transcript, reference);
        int maxLen = Mathf.Max(transcript.Length, reference.Length);
        
        float accuracy = maxLen == 0 ? 100f : (1f - (float)distance / maxLen) * 100f;
        float confScore = confidence * 100f;

        // Công thức cũ của bạn: 70% độ chính xác + 30% độ tự tin
        return Mathf.Clamp((accuracy * 0.7f) + (confScore * 0.3f), 0, 100);
    }

    private int LevenshteinDistance(string s1, string s2)
    {
        int[,] d = new int[s1.Length + 1, s2.Length + 1];
        for (int i = 0; i <= s1.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= s2.Length; j++) d[0, j] = j;
        for (int j = 1; j <= s2.Length; j++)
        {
            for (int i = 1; i <= s1.Length; i++)
            {
                int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                d[i, j] = Mathf.Min(Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }
        return d[s1.Length, s2.Length];
    }

    void initProp(List<SentenceItem> listItem)
    {
        listSentences.AddRange(listItem);
        updateUI(listSentences[0]);
    }
    
    void updateUI(SentenceItem item)
    {
        referenceTextInputVI.text = item.vn;
        referenceTextInputEN.text = item.en;
    }
    
    public void GetSentencesByTopic(string topic, System.Action<List<SentenceItem>> callback)
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("speaking") // root json
            .Child(topic)                     // load đúng topic
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompleted)
                {
                    Debug.LogError("Cannot load topic: " + topic);
                    callback?.Invoke(null);
                    return;
                }

                DataSnapshot snapshot = task.Result;

                List<SentenceItem> list = new List<SentenceItem>();

                foreach (var child in snapshot.Children)
                {
                    SentenceItem item = new SentenceItem();
                    item.vn = child.Child("vn").Value.ToString();
                    item.en = child.Child("en").Value.ToString();

                    list.Add(item);
                }

                callback?.Invoke(list);
            });
    }

    public void onClickHomeButton()
    {
        SceneManager.LoadScene("HomeScene");
    }
}