using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class LoadingController : MonoBehaviour
{
    public static LoadingController Instance;

    [Header("UI")]
    public GameObject loadingPanel;
    public VideoPlayer videoPlayer;
    public RawImage videoDisplay;

    [Header("Video")]
    public VideoClip loadingVideo;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        SetupVideo();
        Hide(); // Ẩn ban đầu
    }

    void SetupVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = true;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            

            if (loadingVideo != null)
                videoPlayer.clip = loadingVideo;

            videoPlayer.prepareCompleted += (vp) => vp.Play();
        }
    }

    // Hiện loading
    public void Show()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            
            if (videoPlayer != null)
            {
                videoPlayer.Prepare();
            }
        }
    }

    // Ẩn loading
    public void Hide()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
            
            if (videoPlayer != null && videoPlayer.isPlaying)
            {
                videoPlayer.Stop();
            }
        }
    }
}