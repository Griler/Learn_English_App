using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using System;

public class PronunciationTrainer : MonoBehaviour
{
    [Header("UI Elements")]
    public Button playReferenceButton;
    public Button recordButton;
    public Button stopRecordButton;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI wordText;
    public TextMeshProUGUI detailText;
    public Slider volumeMeter;
    public Image scoreBar;
    public TMP_InputField wordInput;
    public Button changeWordButton;
    
    [Header("Practice Settings")]
    public string practiceWord = "hello";
    public string[] suggestedWords = { "hello", "world", "beautiful", "practice", "computer", "amazing", "wonderful", "dictionary" };
    
    [Header("Recording Settings")]
    public int recordingDuration = 3;
    public float volumeThreshold = 0.01f;
    
    private AudioSource audioSource;
    private AudioClip recordedClip;
    private AudioClip referenceClip;
    private string microphone;
    private bool isRecording = false;
    private float[] referenceData;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (Microphone.devices.Length > 0)
        {
            microphone = Microphone.devices[0];
            statusText.text = "Sẵn sàng! Nhấn 'Nghe giọng chuẩn' để bắt đầu";
        }
        else
        {
            statusText.text = "Không tìm thấy microphone!";
            recordButton.interactable = false;
        }
        
        playReferenceButton.onClick.AddListener(PlayReference);
        recordButton.onClick.AddListener(StartRecording);
        stopRecordButton.onClick.AddListener(StopRecording);
        
        if (changeWordButton != null)
        {
            changeWordButton.onClick.AddListener(ChangeWord);
        }
        
        stopRecordButton.gameObject.SetActive(false);
        wordText.text = practiceWord.ToUpper();
        
        if (wordInput != null)
        {
            wordInput.text = practiceWord;
        }
    }
    
    void Update()
    {
        if (isRecording)
        {
            UpdateVolumeMeter();
        }
    }
    
    void ChangeWord()
    {
        if (wordInput != null && !string.IsNullOrEmpty(wordInput.text))
        {
            practiceWord = wordInput.text.Trim().ToLower();
            wordText.text = practiceWord.ToUpper();
            statusText.text = $"Đã đổi từ thành: {practiceWord}";
            scoreText.text = "---";
            referenceData = null;
            referenceClip = null;
        }
    }
    
    public void LoadRandomWord()
    {
        practiceWord = suggestedWords[UnityEngine.Random.Range(0, suggestedWords.Length)];
        wordText.text = practiceWord.ToUpper();
        if (wordInput != null)
        {
            wordInput.text = practiceWord;
        }
        statusText.text = $"Từ mới: {practiceWord}";
        scoreText.text = "---";
        referenceData = null;
        referenceClip = null;
    }
    
    void PlayReference()
    {
        StartCoroutine(LoadWordAudio(practiceWord));
    }
    
    IEnumerator LoadWordAudio(string word)
    {
        word = word.ToLower();
        statusText.text = $"Đang tải giọng chuẩn cho '{word}'...";
        playReferenceButton.interactable = false;
        
        // Thử các nguồn phát âm khác nhau
        string[] urls = {
            $"https://api.dictionaryapi.dev/media/pronunciations/en/{word}-us.mp3",
            $"https://api.dictionaryapi.dev/media/pronunciations/en/{word}-uk.mp3",
            $"https://api.dictionaryapi.dev/media/pronunciations/en/{word}-au.mp3"
        };
        
        bool success = false;
        
        foreach (string url in urls)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    audioSource.clip = clip;
                    audioSource.Play();
                    
                    referenceClip = clip;
                    
                    // Lưu dữ liệu để so sánh sau
                    referenceData = new float[clip.samples * clip.channels];
                    clip.GetData(referenceData, 0);
                    
                    statusText.text = $"Đang phát giọng chuẩn: {word}";
                    success = true;
                    break;
                }
                else
                {
                    Debug.Log($"Lỗi tải từ {url}: {www.error}");
                }
            }
        }
        
        if (!success)
        {
            statusText.text = $"Không tìm thấy phát âm cho từ '{word}'! Thử từ khác.";
            if (detailText != null)
            {
                detailText.text = "Gợi ý: hello, world, beautiful, practice, computer, amazing";
            }
        }
        
        playReferenceButton.interactable = true;
    }
    
    void StartRecording()
    {
        if (referenceData == null)
        {
            statusText.text = "Hãy nghe giọng chuẩn trước!";
            return;
        }
        
        recordedClip = Microphone.Start(microphone, false, recordingDuration, 44100);
        isRecording = true;
        
        recordButton.gameObject.SetActive(false);
        stopRecordButton.gameObject.SetActive(true);
        playReferenceButton.interactable = false;
        
        statusText.text = $"🎤 Đang ghi âm... Hãy nói: {practiceWord.ToUpper()}";
        scoreText.text = "---";
        
        StartCoroutine(RecordingTimer());
    }
    
    IEnumerator RecordingTimer()
    {
        yield return new WaitForSeconds(recordingDuration);
        if (isRecording)
        {
            StopRecording();
        }
    }
    
    void StopRecording()
    {
        if (!isRecording) return;
        
        Microphone.End(microphone);
        isRecording = false;
        
        recordButton.gameObject.SetActive(true);
        stopRecordButton.gameObject.SetActive(false);
        playReferenceButton.interactable = true;
        
        statusText.text = "⏳ Đang phân tích phát âm...";
        
        StartCoroutine(AnalyzeRecording());
    }
    
    IEnumerator AnalyzeRecording()
    {
        yield return new WaitForSeconds(0.5f);
        
        float[] recordedData = new float[recordedClip.samples * recordedClip.channels];
        recordedClip.GetData(recordedData, 0);
        
        float score = CalculateSimilarityScore(referenceData, recordedData);
        
        DisplayScore(score);
    }
    
    float CalculateSimilarityScore(float[] reference, float[] recorded)
    {
        if (reference == null || recorded == null)
        {
            return 0f;
        }
        
        // Kiểm tra âm lượng
        float recVolume = CalculateAverageVolume(recorded);
        
        if (recVolume < volumeThreshold)
        {
            statusText.text = "❌ Âm thanh quá nhỏ! Hãy nói to hơn.";
            return 0f;
        }
        
        // Chuẩn hóa độ dài
        int minLength = Mathf.Min(reference.Length, recorded.Length);
        int compareLength = Mathf.Min(minLength, 50000); // Giới hạn để tính nhanh hơn
        
        // 1. Tính Correlation (độ tương quan sóng âm)
        float correlation = CalculateCorrelation(reference, recorded, compareLength);
        
        // 2. Tính độ tương đồng về Zero Crossing Rate (đặc trưng âm thanh)
        float refZCR = CalculateZeroCrossingRate(reference);
        float recZCR = CalculateZeroCrossingRate(recorded);
        float zcrSimilarity = 1f - Mathf.Abs(refZCR - recZCR) / Mathf.Max(refZCR, recZCR);
        
        // 3. Tính độ tương đồng về Energy (năng lượng)
        float refEnergy = CalculateEnergy(reference);
        float recEnergy = CalculateEnergy(recorded);
        float energySimilarity = 1f - Mathf.Abs(refEnergy - recEnergy) / Mathf.Max(refEnergy, recEnergy);
        
        // 4. Tính Spectral Centroid (trọng tâm phổ tần)
        float refCentroid = CalculateSpectralCentroid(reference);
        float recCentroid = CalculateSpectralCentroid(recorded);
        float centroidSimilarity = 1f - Mathf.Abs(refCentroid - recCentroid) / Mathf.Max(refCentroid, recCentroid);
        
        // 5. Tính độ dài tương đối
        float lengthRatio = Mathf.Min(reference.Length, recorded.Length) / (float)Mathf.Max(reference.Length, recorded.Length);
        
        // Tổng hợp điểm (có trọng số)
        float finalScore = (
            correlation * 0.35f +           // Độ tương quan sóng âm
            zcrSimilarity * 0.20f +          // Đặc trưng âm thanh
            energySimilarity * 0.15f +       // Năng lượng
            centroidSimilarity * 0.20f +     // Phổ tần
            lengthRatio * 0.10f              // Độ dài
        ) * 100f;
        
        return Mathf.Clamp(finalScore, 0f, 100f);
    }
    
    float CalculateAverageVolume(float[] data)
    {
        float sum = 0f;
        foreach (float sample in data)
        {
            sum += Mathf.Abs(sample);
        }
        return sum / data.Length;
    }
    
    float CalculateCorrelation(float[] ref1, float[] ref2, int length)
    {
        float sum = 0f;
        float refSum = 0f;
        float recSum = 0f;
        
        for (int i = 0; i < length; i++)
        {
            sum += ref1[i] * ref2[i];
            refSum += ref1[i] * ref1[i];
            recSum += ref2[i] * ref2[i];
        }
        
        float denominator = Mathf.Sqrt(refSum * recSum);
        if (denominator < 0.0001f) return 0f;
        
        return Mathf.Abs(sum / denominator);
    }
    
    float CalculateZeroCrossingRate(float[] data)
    {
        int crossings = 0;
        for (int i = 1; i < data.Length; i++)
        {
            if ((data[i] >= 0 && data[i - 1] < 0) || (data[i] < 0 && data[i - 1] >= 0))
            {
                crossings++;
            }
        }
        return (float)crossings / data.Length;
    }
    
    float CalculateEnergy(float[] data)
    {
        float energy = 0f;
        foreach (float sample in data)
        {
            energy += sample * sample;
        }
        return Mathf.Sqrt(energy / data.Length);
    }
    
    float CalculateSpectralCentroid(float[] data)
    {
        // Tính trọng tâm phổ tần đơn giản
        float weightedSum = 0f;
        float sum = 0f;
        
        for (int i = 0; i < data.Length; i++)
        {
            float magnitude = Mathf.Abs(data[i]);
            weightedSum += i * magnitude;
            sum += magnitude;
        }
        
        if (sum < 0.0001f) return 0f;
        return weightedSum / sum;
    }
    
    void DisplayScore(float score)
    {
        scoreText.text = score.ToString("F1") + " điểm";
        
        string feedback = "";
        Color barColor = Color.white;
        
        if (score >= 85)
        {
            barColor = new Color(0f, 0.8f, 0f); // Xanh đậm
            feedback = "🌟 XUẤT SẮC! Phát âm hoàn hảo!";
        }
        else if (score >= 70)
        {
            barColor = new Color(0.5f, 1f, 0f); // Xanh lá
            feedback = "👏 RẤT TỐT! Gần như chuẩn rồi!";
        }
        else if (score >= 55)
        {
            barColor = Color.yellow;
            feedback = "👍 TỐT! Tiếp tục luyện tập!";
        }
        else if (score >= 40)
        {
            barColor = new Color(1f, 0.6f, 0f); // Cam
            feedback = "💪 KHÁ! Cần cải thiện thêm!";
        }
        else if (score >= 25)
        {
            barColor = new Color(1f, 0.4f, 0f); // Cam đậm
            feedback = "🎯 CỐ GẮNG! Nghe lại giọng chuẩn!";
        }
        else
        {
            barColor = Color.red;
            feedback = "🔄 THỬ LẠI! Nghe kỹ và nói rõ hơn!";
        }
        
        if (scoreBar != null)
        {
            scoreBar.fillAmount = score / 100f;
            scoreBar.color = barColor;
        }
        
        statusText.text = feedback;
        
        if (detailText != null)
        {
            string tips = GetTips(score);
            detailText.text = $"Từ: '{practiceWord.ToUpper()}'\n{tips}";
        }
    }
    
    string GetTips(float score)
    {
        if (score >= 85)
            return "✨ Bạn đã phát âm rất chuẩn!";
        else if (score >= 70)
            return "💡 Mẹo: Chú ý ngữ điệu để đạt điểm cao hơn";
        else if (score >= 55)
            return "💡 Mẹo: Nghe kỹ giọng chuẩn và bắt chước";
        else if (score >= 40)
            return "💡 Mẹo: Nói rõ ràng và đúng trọng âm";
        else
            return "💡 Mẹo: Nghe nhiều lần trước khi ghi âm";
    }
    
    void UpdateVolumeMeter()
    {
        if (recordedClip == null) return;
        
        float[] samples = new float[128];
        int micPosition = Microphone.GetPosition(microphone) - 128;
        if (micPosition < 0) return;
        
        recordedClip.GetData(samples, micPosition);
        
        float sum = 0f;
        foreach (float sample in samples)
        {
            sum += Mathf.Abs(sample);
        }
        
        volumeMeter.value = Mathf.Lerp(volumeMeter.value, sum / samples.Length * 10f, 0.5f);
    }
    
    public void PlayRecordedAudio()
    {
        if (recordedClip != null)
        {
            audioSource.clip = recordedClip;
            audioSource.Play();
            statusText.text = "Đang phát lại giọng của bạn...";
        }
    }
}