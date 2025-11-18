using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Text;
using DG.Tweening;
using TMPro; // Dùng cho TextMeshPro
using UnityEngine.Networking;

// ##################################################################
// DATA CLASSES CHO SPEECH-TO-TEXT (STT) API
// ##################################################################

// --- STT Request (Yêu cầu) ---
// (Các lớp này được thêm vào để tối ưu hóa, thay thế việc tạo JSON bằng tay)
[System.Serializable]
public class GoogleSpeechRequest
{
    public RecognitionConfig config;
    public RecognitionAudio audio;
}

[System.Serializable]
public class RecognitionConfig
{
    public string encoding;
    public int sampleRateHertz;
    public string languageCode;
    public bool enableWordTimeOffsets;
    public bool enableWordConfidence;
    public string model;
    public bool useEnhanced;
}

[System.Serializable]
public class RecognitionAudio
{
    public string content;
}

// --- STT Response (Phản hồi) ---
[System.Serializable]
public class GoogleSpeechResponse
{
    public GoogleSpeechResult[] results;
}

[System.Serializable]
public class GoogleSpeechResult
{
    public GoogleSpeechAlternative[] alternatives;
}

[System.Serializable]
public class GoogleSpeechAlternative
{
    public string transcript;
    public float confidence;
    public GoogleWordInfo[] words;
}

[System.Serializable]
public class GoogleWordInfo
{
    public string word;
    public float confidence;
    public string startTime;
    public string endTime;
}

// ##################################################################
// DATA CLASSES CHO TEXT-TO-SPEECH (TTS) API
// ##################################################################

// --- TTS Request (Yêu cầu) ---
[System.Serializable]
public class SynthesisRequest
{
    public SynthesisInput input;
    public VoiceSelectionParams voice;
    public AudioConfig audioConfig;
}

[System.Serializable]
public class SynthesisInput
{
    public string text;
}

[System.Serializable]
public class VoiceSelectionParams
{
    public string languageCode;
    public string name;
}

[System.Serializable]
public class AudioConfig
{
    public string audioEncoding;
    public int sampleRateHertz;
}

// --- TTS Response (Phản hồi) ---
[System.Serializable]
public class SynthesisResponse
{
    public string audioContent;
}


// ##################################################################
// MAIN MONOBEHAVIOUR CLASS
// ##################################################################

public class GoogleSpeechService : MonoBehaviour
{
    [Header("API Settings")]
    [Tooltip("API Key của Google Cloud (Dùng chung cho cả STT và TTS)")]
    [SerializeField] private string apiKey = "YOUR_GOOGLE_API_KEY"; // <-- !!! THAY API KEY CỦA BẠN VÀO ĐÂY

    [Header("Component References (Shared)")]
    [Tooltip("Dùng để phát âm thanh Text-to-Speech")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Dùng để hiển thị trạng thái chung")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("--- Speech-to-Text (Scorer) ---")]
    [SerializeField] private Button recordButton;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI transcriptText;
    [Tooltip("Văn bản mẫu để so sánh")]
    [SerializeField] private TextMeshProUGUI referenceTextInput;
    [SerializeField] private int recordingLength = 5;
    [Tooltip("Sample rate cho ghi âm STT (16000 là tốt nhất)")]
    [SerializeField] private int sttSampleRate = 16000;

    [Header("--- Text-to-Speech ---")]
    [Tooltip("Văn bản để phát âm thanh")]
    [SerializeField] private TextMeshProUGUI ttsText;
    [SerializeField] private Button speakButton;
    [Tooltip("Ví dụ: en-US, vi-VN")]
    [SerializeField] private string ttsLanguageCode = "vi-VN";
    [Tooltip("Ví dụ: en-US-Wavenet-D, vi-VN-Wavenet-A")]
    [SerializeField] private string ttsVoiceName = "vi-VN-Wavenet-A";
    
    // *** TÍNH NĂNG MỚI: KIỂM TRA ÂM LƯỢNG ***
    [Tooltip("Độ dài tối thiểu (giây) để coi là hợp lệ")]
    [SerializeField] private float minValidLength = 0.5f; 
    [Tooltip("Ngưỡng âm lượng (nếu dưới mức này thì coi là im lặng)")]
    [SerializeField] private float silenceThreshold = 0.035f; 

    [Header("--- Real-time Audio Feedback ---")]
    [Tooltip("Slider để hiển thị âm lượng micro theo thời gian thực")]
    [SerializeField] private Slider volumeSlider;
    
    // Phải khớp với sampleRateHertz của TTS
    private const int TTS_SAMPLE_RATE = 24000; 

    // Biến nội bộ cho STT
    private AudioClip recordedClip;
    private bool isRecording = false;
    private string deviceName;
    private int lastSamplePosition = 0; // Dùng cho
    private float[] tempSampleBuffer; // Dùng cho
    void Start()
    {
        // Setup chung
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        // Setup cho STT (Scorer)
        if (recordButton != null)
            recordButton.onClick.AddListener(OnRecordButtonClick);
        
        if (referenceTextInput != null && string.IsNullOrEmpty(referenceTextInput.text))
            referenceTextInput.text = "Hello, how are you today?";
        
        // Setup cho TTS
        if (speakButton != null)
            speakButton.onClick.AddListener(OnSpeakButtonClick);

        UpdateStatus("Ready");
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
        
        Debug.Log($"[GoogleSpeechService] {message}");
    }

    // ##################################################################
    // ## Speech-to-Text (Pronunciation Scoring)
    // ##################################################################

    public void OnRecordButtonClick()
    {
        if (!isRecording)
        {
            StartRecording();
        }
        else
        {
            DOTween.KillAll();
            DOVirtual.DelayedCall(1f, () =>
            {
                StopRecording();
            });
        }
    }

    void StartRecording()
    {
        if (Microphone.devices.Length > 0)
        {
            deviceName = Microphone.devices[0];
            recordedClip = Microphone.Start(deviceName, false, recordingLength, sttSampleRate);
            isRecording = true;
            lastSamplePosition = 0; // Reset vị trí đọc sample
            
            UpdateStatus("Recording... Click again to stop");
            if (recordButton != null)
                recordButton.GetComponentInChildren<TextMeshProUGUI>().text = "Stop Recording";
        }
        else
        {
            UpdateStatus("No microphone detected!");
        }
    }

    /// <summary>
    /// *** CẬP NHẬT: Thêm bước kiểm tra âm thanh trước khi gửi API ***
    /// </summary>
    void StopRecording()
    {
        if (isRecording)
        {
            Microphone.End(deviceName);
            isRecording = false;
            
            if (recordButton != null)
                recordButton.GetComponentInChildren<TextMeshProUGUI>().text = "Start Recording";

            // *** BƯỚC KIỂM TRA MỚI ***
            // Kiểm tra xem clip có tồn tại và có hợp lệ không
            if (recordedClip == null || !IsAudioClipValid(recordedClip))
            {
                // IsAudioClipValid đã tự gọi UpdateStatus để báo lỗi
                recordedClip = null; 
                return; // Dừng lại, không gửi API
            }
            
            // Nếu audio OK, tiếp tục xử lý
            UpdateStatus("Processing audio...");
            StartCoroutine(ProcessAudioStt());
        }
    }
    
    public void OnReplayButtonClick()
    {
        if (recordedClip != null)
        {
            // Dừng bất cứ thứ gì AudioSource đang phát (có thể là TTS)
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            audioSource.clip = recordedClip;
            audioSource.Play();
            UpdateStatus("Playing last recording...");
        }
        else
        {
            UpdateStatus("No recording available to play.");
        }
    }
    
    bool IsAudioClipValid(AudioClip clip)
    {
        // 1. Kiểm tra độ dài tối thiểu
        if (clip.length < minValidLength)
        {
            UpdateStatus("Recording too short. Please try again.");
            Debug.LogWarning("[STT] Audio clip is too short.");
            return false;
        }

        // 2. Kiểm tra sự im lặng
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        float maxAmplitude = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float amplitude = Mathf.Abs(samples[i]);
            if (amplitude > maxAmplitude)
            {
                maxAmplitude = amplitude;
            }
        }

        if (maxAmplitude < silenceThreshold)
        {
            UpdateStatus("No sound detected. Please speak louder.");
            Debug.LogWarning($"[STT] Audio clip is silent. Max amplitude: {maxAmplitude}");
            return false;
        }

        // Nếu vượt qua cả 2 bài test
        return true;
    }

    IEnumerator ProcessAudioStt()
    {
        if (recordedClip == null)
        {
            UpdateStatus("Error: No recorded clip.");
            yield break;
        }

        byte[] audioData = ConvertAudioClipToRawPCM(recordedClip);
        string base64Audio = Convert.ToBase64String(audioData);
        
        string referenceText = "Hello, how are you today";
        string jsonRequest = CreateGoogleSpeechRequest(base64Audio, referenceText);
        
        string url = $"https://speech.googleapis.com/v1/speech:recognize?key={apiKey}";
        
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                ProcessSttResponse(request.downloadHandler.text, referenceText);
            }
            else
            {
                UpdateStatus($"STT Error: {request.error}");
                Debug.LogError($"[STT] Response: {request.downloadHandler.text}");
            }
        }
    }

    // *** TỐI ƯU HÓA: Dùng JsonUtility thay vì tạo string thủ công ***
    string CreateGoogleSpeechRequest(string base64Audio, string referenceText)
    {
        GoogleSpeechRequest requestData = new GoogleSpeechRequest
        {
            config = new RecognitionConfig
            {
                encoding = "LINEAR16",
                sampleRateHertz = sttSampleRate, // Dùng biến sttSampleRate
                languageCode = "en-US",
                enableWordTimeOffsets = true,
                enableWordConfidence = true,
                model = "default",
                useEnhanced = true
            },
            audio = new RecognitionAudio
            {
                content = base64Audio
            }
        };
        
        return JsonUtility.ToJson(requestData);
    }

    void ProcessSttResponse(string jsonResponse, string referenceText)
    {
        try
        {
            GoogleSpeechResponse response = JsonUtility.FromJson<GoogleSpeechResponse>(jsonResponse);
            
            if (response != null && response.results != null && response.results.Length > 0)
            {
                if (response.results[0].alternatives != null && response.results[0].alternatives.Length > 0)
                {
                    string transcript = response.results[0].alternatives[0].transcript;
                    
                    if (string.IsNullOrEmpty(transcript))
                    {
                        UpdateStatus("No speech detected. Please try again.");
                        if (transcriptText != null) transcriptText.text = "You said: (nothing)";
                        if (scoreText != null) scoreText.text = "Score: 0/100";
                        return;
                    }
                    
                    float confidence = response.results[0].alternatives[0].confidence;
                    float score = CalculatePronunciationScore(transcript, referenceText, confidence);
                    
                    if (transcriptText != null)
                        transcriptText.text = $"You said: {transcript}";
                    
                    if (scoreText != null)
                    {
                        scoreText.text = $"Score: {score:F1}/100";
                        scoreText.color = GetScoreColor(score);
                    }
                    UpdateStatus("Assessment complete!");
                }
                else
                {
                    UpdateStatus("No speech detected (empty alternatives).");
                }
            }
            else
            {
                UpdateStatus("No speech detected. Please try again.");
            }
        }
        catch (Exception e)
        {
            UpdateStatus($"Error processing STT response: {e.Message}");
            Debug.LogError($"[STT] Error parsing JSON: {e.Message}");
            Debug.LogError($"[STT] Full response: {jsonResponse}");
        }
    }

    // --- Các hàm tính điểm (Giữ nguyên) ---
    float CalculatePronunciationScore(string transcript, string reference, float confidence)
    {
        transcript = transcript.ToLower().Trim();
        reference = reference.ToLower().Trim();
        float accuracyScore = CalculateAccuracy(transcript, reference);
        float confidenceScore = confidence * 100f;
        Debug.Log($"[STT] reference: {reference},[STT] transcript: {transcript} ");
        Debug.Log(confidenceScore);
        float finalScore = (accuracyScore * 0.7f) + (confidenceScore * 0.3f);
        return Mathf.Clamp(finalScore, 0f, 100f);
    }

    float CalculateAccuracy(string transcript, string reference)
    {
        int distance = LevenshteinDistance(transcript, reference);
        int maxLength = Mathf.Max(transcript.Length, reference.Length);
        if (maxLength == 0) return 100f;
        float similarity = 1f - ((float)distance / maxLength);
        return similarity * 100f;
    }

    int LevenshteinDistance(string s1, string s2)
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

    Color GetScoreColor(float score)
    {
        if (score >= 80f) return Color.green;
        if (score >= 60f) return Color.yellow;
        return Color.red;
    }

    // ##################################################################
    // ## Text-to-Speech (TTS)
    // ##################################################################

    public void OnSpeakButtonClick()
    {
        
        StartSpeaking(ttsText.text);
        UpdateStatus("Please enter text to speak.");

    }

    /// <summary>
    /// Bắt đầu quá trình gọi API và phát âm thanh
    /// </summary>
    public void StartSpeaking(string textToSpeak)
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_GOOGLE_API_KEY")
        {
            UpdateStatus("ERROR: API Key is not set.");
            Debug.LogError("[TTS] API Key is not set.");
            return;
        }

        UpdateStatus("Synthesizing audio...");
        StartCoroutine(SynthesizeSpeech(textToSpeak));
    }

    private IEnumerator SynthesizeSpeech(string text)
    {
        SynthesisRequest requestData = new SynthesisRequest
        {
            input = new SynthesisInput { text = text },
            voice = new VoiceSelectionParams { languageCode = ttsLanguageCode, name = ttsVoiceName },
            audioConfig = new AudioConfig { audioEncoding = "LINEAR16", sampleRateHertz = TTS_SAMPLE_RATE }
        };

        string jsonRequest = JsonUtility.ToJson(requestData);
        string url = $"https://texttospeech.googleapis.com/v1/text:synthesize?key={apiKey}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                UpdateStatus("Processing TTS audio...");
                ProcessTtsResponse(request.downloadHandler.text);
            }
            else
            {
                UpdateStatus($"TTS Error: {request.error}");
                Debug.LogError($"[TTS] Error: {request.error}");
                Debug.LogError($"[TTS] Response: {request.downloadHandler.text}");
            }
        }
    }

    private void ProcessTtsResponse(string jsonResponse)
    {
        try
        {
            SynthesisResponse response = JsonUtility.FromJson<SynthesisResponse>(jsonResponse);

            if (string.IsNullOrEmpty(response.audioContent))
            {
                UpdateStatus("Error: Received empty audio content.");
                Debug.LogWarning($"[TTS] Received empty audio content. Full response: {jsonResponse}");
                return;
            }

            byte[] pcmData = Convert.FromBase64String(response.audioContent);
            float[] floatData = Convert16BitPcmToFloat(pcmData);

            AudioClip clip = AudioClip.Create("TTS_Clip", floatData.Length, 1, TTS_SAMPLE_RATE, false);
            clip.SetData(floatData, 0);

            audioSource.clip = clip;
            audioSource.Play();
            UpdateStatus("Playing audio...");
        }
        catch (Exception e)
        {
            UpdateStatus($"Error processing TTS response: {e.Message}");
            Debug.LogError($"[TTS] Error: {e.Message}. Response: {jsonResponse}");
        }
    }

    // ##################################################################
    // ## Audio Conversion Helpers (Dùng chung)
    // ##################################################################

    /// <summary>
    /// Chuyển AudioClip (float[]) sang 16-bit PCM (byte[]) để gửi cho STT.
    /// </summary>
    byte[] ConvertAudioClipToRawPCM(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);
        
        short[] intData = new short[samples.Length];
        byte[] bytesData = new byte[samples.Length * 2];
        
        float rescaleFactor = 32767;
        
        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            byte[] byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }
        
        return bytesData; // Trả về raw PCM data, không có WAV header
    }

    /// <summary>
    /// Chuyển đổi mảng byte (16-bit PCM) nhận từ TTS sang mảng float (-1.0 đến 1.0)
    /// </summary>
    private float[] Convert16BitPcmToFloat(byte[] pcmData)
    {
        int samples = pcmData.Length / 2; // 2 bytes per 16-bit sample
        float[] floatData = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            short pcmSample = BitConverter.ToInt16(pcmData, i * 2);
            floatData[i] = pcmSample / 32768.0f;
        }

        return floatData;
    }
    
    void Update()
    {
        if (isRecording)
        {
            UpdateVolumeMeter();
        }
        else
        {
            if (volumeSlider != null && volumeSlider.value > 0)
                volumeSlider.value = Mathf.Lerp(volumeSlider.value, 0, Time.deltaTime * 10);
        }
    }
    
    private void UpdateVolumeMeter()
    {
        if (recordedClip == null) return;
        
        float[] samples = new float[128];
        int micPosition = Microphone.GetPosition(deviceName) - 128;
        if (micPosition < 0) return;
        
        recordedClip.GetData(samples, micPosition);
        
        float sum = 0f;
        foreach (float sample in samples)
        {
            sum += Mathf.Abs(sample);
        }

        volumeSlider.value = Mathf.Lerp(volumeSlider.value, sum / samples.Length * 10f, 0.5f);
        
        if ( volumeSlider.value < 0.2)
        {
            volumeSlider.gameObject.GetComponent<Image>().color = Color.red; // Quá nhỏ
        }
        else if ( volumeSlider.value < 0.4)
        {
            volumeSlider.gameObject.GetComponent<Image>().color = new Color(1f, 0.5f, 0f); // Orange - hơi nhỏ
        }
        else if ( volumeSlider.value < 0.8)
        {
            volumeSlider.gameObject.GetComponent<Image>().color = Color.yellow; // Hơi lớn
        }
        else 
        {
            volumeSlider.gameObject.GetComponent<Image>().color = Color.green; // Tốt
        }
    }
}