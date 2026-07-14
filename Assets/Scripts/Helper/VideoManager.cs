using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;

[System.Serializable]
public struct VideoScenarioData
{
    [SerializeField] internal string name;
    [SerializeField] internal VideoClip videoClip;
    [SerializeField] internal AudioClip audioClip;
}

public class VideoManager : MonoBehaviour
{

    [Header("Audio Configurations")]
    [SerializeField] private AudioSource MusicAudioSource;

    [Header("Video Configurations")]
    [SerializeField] private AudioController audioController;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private GameObject videoDisplayPanel;
    [SerializeField] private VideoScenarioData[] videoScenarios;
    [SerializeField] private bool forceUrlPlayback = false;
    [SerializeField] private string videoUrlPrefix = "https://d1lerod2freygq.cloudfront.net/SL-MJ/StreamingAssets/";

    internal bool isVideoPlaying = false;

    // Fired when a video finishes playing, passes the scenario type that just completed.
    internal event Action<int> OnVideoFinished;

    private int currentType = -1;

    private void Awake()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        // Ensure Unity isn't auto-playing on Prepare; we drive playback manually so
        // we can sync the AudioSource to the exact frame the video starts on.
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;

        videoPlayer.prepareCompleted += HandlePrepareCompleted;
        videoPlayer.started += HandleVideoStarted;
        videoPlayer.loopPointReached += HandleVideoFinished;
    }

    private void OnDestroy()
    {
        videoPlayer.prepareCompleted -= HandlePrepareCompleted;
        videoPlayer.started -= HandleVideoStarted;
        videoPlayer.loopPointReached -= HandleVideoFinished;
    }

    internal void PlayVideo(int type)
    {
        if (type < 0 || type >= videoScenarios.Length)
        {
            Debug.Log("Invalid video type index: " + type);
            return;
        }

        if (videoScenarios[type].videoClip == null)
        {
            Debug.Log("Video clip missing for scenario index: " + type);
            //return;
        }

        currentType = type;
        VideoScenarioData scenario = videoScenarios[type];

        // Always fully stop/reset the player before preparing again — even if nothing is
        // "currently playing" right now. VideoPlayer.isPrepared stays true once a clip has
        // been prepared/played, and Prepare() is a no-op while isPrepared == true. Without
        // this, replaying the same (already-finished) video never fires prepareCompleted,
        // so Play() never gets called and playback appears stuck.
        videoPlayer.Stop();
        if (isVideoPlaying)
        {
            MusicAudioSource.Stop();
        }
        //isVideoPlaying = false;
        Debug.Log("before");
        if (forceUrlPlayback)
        {
            string temp = videoUrlPrefix + scenario.name + ".mp4";
            Debug.Log(temp);
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = temp;
        }
        else
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = scenario.videoClip;
        }

        if(scenario.audioClip != null)MusicAudioSource.clip = scenario.audioClip;

        if (videoDisplayPanel != null)
        {
            videoDisplayPanel.SetActive(true);
        }

        // Prepare before playing so there's no first-frame stutter/desync with the audio.
        videoPlayer.Prepare();
    }

    private void HandlePrepareCompleted(VideoPlayer source)
    {
        // Play() triggers the "started" callback, where we actually start the audio in sync.
        videoPlayer.Play();
    }

    private void HandleVideoStarted(VideoPlayer source)
    {
        isVideoPlaying = true;
        Debug.Log("Video started playing");

        if (MusicAudioSource.clip != null && !audioController.isMusicMuted)
        {
            MusicAudioSource.Play();
        }
    }

    private void HandleVideoFinished(VideoPlayer source)
    {
        isVideoPlaying = false;
        Debug.Log("Video finished playing");

        MusicAudioSource.Stop();

        if (videoDisplayPanel != null)
        {
            videoDisplayPanel.SetActive(false);
        }

        OnVideoFinished?.Invoke(currentType);
        currentType = -1;
    }
}