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
    [Header("API Configuration")]
    [SerializeField] private string apiKey = ""; 
    
    // Cấu hình mặc định cho API
    private const int STT_SAMPLE_RATE = 16000;
    private const int TTS_SAMPLE_RATE = 24000;
    private const string TTS_LANGUAGE_CODE = "en-US";
    private const string TTS_VOICE_NAME = "en-US-Wavenet-D";

    // Singleton để dễ gọi từ bất cứ đâu (Optional)
    public static GoogleSpeechService Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        apiKey = DotEnv.API_KEY; 
    }

    // ##################################################################
    // ## PUBLIC METHODS (API CALLS)
    // ##################################################################

    /// <summary>
    /// Gửi Audio Clip lên Google để lấy văn bản (STT)
    /// </summary>
    public void SpeechToText(AudioClip clip, Action<string, float> onSuccess, Action<string> onError)
    {
        if (clip == null)
        {
            onError?.Invoke("AudioClip is null");
            return;
        }

        StartCoroutine(ProcessAudioStt(clip, onSuccess, onError));
    }

    /// <summary>
    /// Gửi Text lên Google để lấy file Audio (TTS)
    /// </summary>
    public void TextToSpeech(string text, Action<AudioClip> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrEmpty(text))
        {
            onError?.Invoke("Text is empty");
            return;
        }

        StartCoroutine(SynthesizeSpeech(text, onSuccess, onError));
    }

    // ##################################################################
    // ## INTERNAL PROCESSING (COROUTINES)
    // ##################################################################

    private IEnumerator ProcessAudioStt(AudioClip clip, Action<string, float> onSuccess, Action<string> onError)
    {
        byte[] audioData = ConvertAudioClipToRawPCM(clip);
        string base64Audio = Convert.ToBase64String(audioData);

        GoogleSpeechRequest requestData = new GoogleSpeechRequest
        {
            config = new RecognitionConfig
            {
                encoding = "LINEAR16",
                sampleRateHertz = STT_SAMPLE_RATE,
                languageCode = "en-US", // Mặc định tiếng Anh để chấm điểm phát âm
                enableWordTimeOffsets = true,
                enableWordConfidence = true,
                model = "default",
                useEnhanced = true
            },
            audio = new RecognitionAudio { content = base64Audio }
        };

        string jsonRequest = JsonUtility.ToJson(requestData);
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
                ParseSttResponse(request.downloadHandler.text, onSuccess, onError);
            }
            else
            {
                onError?.Invoke($"STT Error: {request.error}");
            }
        }
    }

    private IEnumerator SynthesizeSpeech(string text, Action<AudioClip> onSuccess, Action<string> onError)
    {
        SynthesisRequest requestData = new SynthesisRequest
        {
            input = new SynthesisInput { text = text },
            voice = new VoiceSelectionParams { languageCode = TTS_LANGUAGE_CODE, name = TTS_VOICE_NAME },
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
                ParseTtsResponse(request.downloadHandler.text, onSuccess, onError);
            }
            else
            {
                onError?.Invoke($"TTS Error: {request.error}");
            }
        }
    }

    // ##################################################################
    // ## HELPERS & PARSING
    // ##################################################################

    private void ParseSttResponse(string json, Action<string, float> onSuccess, Action<string> onError)
    {
        try
        {
            GoogleSpeechResponse response = JsonUtility.FromJson<GoogleSpeechResponse>(json);
            if (response != null && response.results != null && response.results.Length > 0)
            {
                var alternative = response.results[0].alternatives[0];
                onSuccess?.Invoke(alternative.transcript, alternative.confidence);
            }
            else
            {
                onError?.Invoke("No speech detected");
                if(ToastSystem.Instance != null)
                {
                    ToastSystem.Instance.ShowToast("Vui Lòng Phát Âm");
                }
            }
        }
        catch (Exception e)
        {
            onError?.Invoke("JSON Parse Error: " + e.Message);
        }
    }

    private void ParseTtsResponse(string json, Action<AudioClip> onSuccess, Action<string> onError)
    {
        try
        {
            SynthesisResponse response = JsonUtility.FromJson<SynthesisResponse>(json);
            if (string.IsNullOrEmpty(response.audioContent))
            {
                onError?.Invoke("Empty audio content");
                return;
            }

            byte[] pcmData = Convert.FromBase64String(response.audioContent);
            float[] floatData = Convert16BitPcmToFloat(pcmData);

            AudioClip clip = AudioClip.Create("TTS_Clip", floatData.Length, 1, TTS_SAMPLE_RATE, false);
            clip.SetData(floatData, 0);
            onSuccess?.Invoke(clip);
        }
        catch (Exception e)
        {
            onError?.Invoke("TTS Parse Error: " + e.Message);
        }
    }

    // --- Audio Converters (Giữ nguyên vì cần thiết cho API) ---
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
            BitConverter.GetBytes(intData[i]).CopyTo(bytesData, i * 2);
        }
        return bytesData;
    }

    private float[] Convert16BitPcmToFloat(byte[] pcmData)
    {
        int samples = pcmData.Length / 2;
        float[] floatData = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            floatData[i] = BitConverter.ToInt16(pcmData, i * 2) / 32768.0f;
        }
        return floatData;
    }
}

// (Giữ nguyên các Class Model: GoogleSpeechRequest, Response... ở cuối file hoặc tách ra file riêng)}