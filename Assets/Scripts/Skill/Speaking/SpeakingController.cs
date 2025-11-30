using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SpeakingController : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Button recordButton;
    [SerializeField] private Button speakButton; // Button to hear the reference text
    [SerializeField] private Button replayButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;
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
    private List<AudioClip> ListRecordedClip = new List<AudioClip>();
    private string micDeviceName;
    private bool isRecording = false;

    private List<SpeakingQuestion> listSentences = new List<SpeakingQuestion>();
    private int currentQuestionIndex = 0;

    void Start()
    {
        // Setup AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Setup Buttons
        if (recordButton) recordButton.onClick.AddListener(OnRecordToggle);
        if (speakButton) speakButton.onClick.AddListener(OnSpeakClicked);
        if (replayButton) replayButton.onClick.AddListener(OnReplayClicked);
        if (nextButton) nextButton.onClick.AddListener(OnClickNextBtn);
        if (prevButton) prevButton.onClick.AddListener(OnClickPrevBtn);

        // Fetch questions from API
        int currentTopicId = PlayerPrefs.GetInt("SelectedSpeakingTopic");
        if (currentTopicId > 0)
        {
            UpdateStatus("Loading questions...");
            StartCoroutine(ApiController.Instance.GetSpeakingQuestionsByCategoryId(currentTopicId, OnQuestionsLoaded));
        }
        else
        {
            UpdateStatus("No topic selected!");
            Debug.LogError("SelectedSpeakingTopic not found in PlayerPrefs.");
        }
    }

    void Update()
    {
        if (isRecording)
        {
            UpdateVolumeMeter();
        }
        else
        {
             // Reset slider
             if (volumeSlider && volumeSlider.value > 0)
                volumeSlider.value = Mathf.Lerp(volumeSlider.value, 0, Time.deltaTime * 10);
        }
    }

    // ##################################################################
    // ## Question Navigation
    // ##################################################################

    private void OnQuestionsLoaded(List<SpeakingQuestion> questions)
    {
        if (questions == null || questions.Count == 0)
        {
            UpdateStatus("No questions found for this topic.");
            return;
        }

        listSentences = questions;
        currentQuestionIndex = 0;
        DisplayQuestion(currentQuestionIndex);
        UpdateStatus("Ready to start!");
    }

    private void DisplayQuestion(int index)
    {
        if (listSentences == null || listSentences.Count == 0) return;

        // Clamp index to be safe
        currentQuestionIndex = Mathf.Clamp(index, 0, listSentences.Count - 1);

        SpeakingQuestion currentQuestion = listSentences[currentQuestionIndex];
        if (referenceTextInputEN) referenceTextInputEN.text = currentQuestion.en;
        if (referenceTextInputVI) referenceTextInputVI.text = currentQuestion.vn;

        // Reset UI for new question
        if (transcriptText) transcriptText.text = "Your transcript will appear here.";
        if (scoreText)
        {
            scoreText.text = "Score: ";
            scoreText.color = Color.white;
        }
        if (statusText) statusText.text = $"Question {currentQuestionIndex + 1}/{listSentences.Count}";

        // Update button states
        if (prevButton) prevButton.interactable = (currentQuestionIndex > 0);
        if (nextButton) nextButton.interactable = (currentQuestionIndex < listSentences.Count - 1);
    }

    public void OnClickNextBtn()
    {
        if (currentQuestionIndex < listSentences.Count - 1)
        {
            DisplayQuestion(currentQuestionIndex + 1);
        }
    }

    public void OnClickPrevBtn()
    {
        if (currentQuestionIndex > 0)
        {
            DisplayQuestion(currentQuestionIndex - 1);
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
        ListRecordedClip.Add(recordedClip);
        isRecording = true;

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

        UpdateStatus("Processing...");
        GoogleSpeechService.Instance.SpeechToText(recordedClip, OnSTTSuccess, OnApiError);
    }

    private bool IsAudioValid(AudioClip clip)
    {
        if (clip == null || clip.length < minValidLength)
        {
            UpdateStatus("Recording too short.");
            return false;
        }

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
        if (!volumeSlider || !isRecording) return;

        int micPos = Microphone.GetPosition(micDeviceName);
        if (micPos < 128) return;

        float[] samples = new float[128];
        recordedClip.GetData(samples, micPos - 128);

        float sum = 0;
        foreach (var s in samples) sum += Mathf.Abs(s);
        float avg = sum / 128f;

        volumeSlider.value = Mathf.Lerp(volumeSlider.value, avg * 10f, 0.5f);
    }

    // ##################################################################
    // ## AUDIO PLAYBACK & API CALLBACKS
    // ##################################################################

    public void OnSpeakClicked()
    {
        string textToSpeak = referenceTextInputEN ? referenceTextInputEN.text : "";
        if (string.IsNullOrEmpty(textToSpeak)) return;

        UpdateStatus("Synthesizing audio...");
        GoogleSpeechService.Instance.TextToSpeech(
            textToSpeak,
            (clip) => {
                UpdateStatus("Playing reference audio...");
                audioSource.clip = clip;
                audioSource.Play();
            },
            OnApiError
        );
    }

    private void OnReplayClicked()
    {
        if (HasClipAtIndex(currentQuestionIndex))
        {
            audioSource.clip = ListRecordedClip[currentQuestionIndex];
            audioSource.Play();
            UpdateStatus("Replaying your recording...");
        }
        else
        {
            UpdateStatus("No recording found at this question.");
        }
    }
    
    bool HasClipAtIndex(int index)
    {
        return ListRecordedClip != null &&
               index >= 0 &&
               index < ListRecordedClip.Count &&
               ListRecordedClip[index] != null;
    }


    private void OnSTTSuccess(string transcript, float confidence)
    {
        if (transcriptText) transcriptText.text = $"You said: {transcript}";

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

    // ##################################################################
    // ## SCORING & UTILITIES
    // ##################################################################

    private float CalculateScore(string transcript, string reference, float confidence)
    {
        if (string.IsNullOrEmpty(reference)) return 0;

        transcript = transcript.ToLower().Trim();
        reference = reference.ToLower().Trim();

        int distance = LevenshteinDistance(transcript, reference);
        int maxLen = Mathf.Max(transcript.Length, reference.Length);

        float accuracy = maxLen == 0 ? 100f : (1f - (float)distance / maxLen) * 100f;
        float confScore = confidence * 100f;

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

    public void OnClickHomeButton()
    {
        SceneManager.LoadScene("HomeScene");
    }
}
