using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

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
    [SerializeField] private RawImage videoDisplayImage; // RawImage whose texture we swap to the active RenderTexture
    [SerializeField] private VideoScenarioData[] videoScenarios;
    [SerializeField] private bool forceUrlPlayback = false;
    [SerializeField] private string videoUrlPrefix = "https://d1lerod2freygq.cloudfront.net/SL-MJ/StreamingAssets/";

    [Header("Preload Settings")]
    [SerializeField] private int preloadTextureWidth = 1920;
    [SerializeField] private int preloadTextureHeight = 1080;

    internal bool isVideoPlaying = false;

    // Fired when a video finishes playing, passes the scenario type that just completed.
    internal event Action<int> OnVideoFinished;

    private int currentType = -1;

    // Preload pool: one dedicated VideoPlayer + RenderTexture per scenario,
    // prepared ahead of time so PlayVideo() can start instantly.
    private VideoPlayer[] pooledPlayers;
    private RenderTexture[] pooledTextures;
    private bool[] preloadReady;
    private bool[] pendingPlay;
    private bool preloadStarted;

    // The VideoPlayer currently driving playback (either the pool player or the fallback videoPlayer).
    private VideoPlayer activePlayer;

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

        if (pooledPlayers != null)
        {
            for (int i = 0; i < pooledPlayers.Length; i++)
            {
                if (pooledPlayers[i] != null)
                {
                    Destroy(pooledPlayers[i].gameObject);
                }
                if (pooledTextures[i] != null)
                {
                    pooledTextures[i].Release();
                    Destroy(pooledTextures[i]);
                }
            }
        }
    }

    #region Preloading

    // Call this as soon as you know which scenarios you'll need (e.g. right when
    // game:init data arrives), so buffering happens in the background well before
    // PlayVideo() is ever called.
    internal void PreloadVideos()
    {
        if (preloadStarted) return;
        preloadStarted = true;

        int count = videoScenarios.Length;
        pooledPlayers = new VideoPlayer[count];
        pooledTextures = new RenderTexture[count];
        preloadReady = new bool[count];
        pendingPlay = new bool[count];

        for (int i = 0; i < count; i++)
        {
            VideoScenarioData scenario = videoScenarios[i];

            if (!forceUrlPlayback && scenario.videoClip == null)
            {
                continue; // nothing to preload for this slot
            }

            var holder = new GameObject($"PreloadVideoPlayer_{scenario.name}");
            holder.transform.SetParent(transform, false);

            VideoPlayer vp = holder.AddComponent<VideoPlayer>();
            vp.playOnAwake = false;
            vp.isLooping = false;
            vp.renderMode = VideoRenderMode.RenderTexture;

            RenderTexture rt = new RenderTexture(preloadTextureWidth, preloadTextureHeight, 0);
            vp.targetTexture = rt;
            pooledTextures[i] = rt;

            if (forceUrlPlayback)
            {
                vp.source = VideoSource.Url;
                vp.url = videoUrlPrefix + scenario.name + ".mp4";
            }
            else
            {
                vp.source = VideoSource.VideoClip;
                vp.clip = scenario.videoClip;
            }

            vp.started += HandleVideoStarted;
            vp.loopPointReached += HandleVideoFinished;

            int index = i; // capture for closure
            vp.prepareCompleted += (source) =>
            {
                preloadReady[index] = true;
                Debug.Log($"[VideoManager] Preloaded and ready: {videoScenarios[index].name}");

                // If PlayVideo() was already called for this scenario while it was
                // still buffering, start it now that it's finally prepared.
                if (pendingPlay[index])
                {
                    pendingPlay[index] = false;
                    ActivatePreparedPlayer(index);
                }
            };

            pooledPlayers[i] = vp;
            vp.Prepare();
        }
    }

    private void ActivatePreparedPlayer(int type)
    {
        activePlayer = pooledPlayers[type];

        // Wipe any stale frame left over from this RenderTexture's last playback
        // (either a previous video, or this same video on replay) so we never
        // briefly flash old content before the new frame is decoded.
        ClearRenderTexture(pooledTextures[type]);

        if (videoDisplayImage != null)
        {
            videoDisplayImage.texture = pooledTextures[type];
        }

        activePlayer.Play(); // already prepared -> starts immediately, no buffering wait
    }

    // Clears a RenderTexture to black so leftover frames from a previous
    // playback don't linger on screen while the next video is buffering.
    private void ClearRenderTexture(RenderTexture rt)
    {
        if (rt == null) return;

        RenderTexture previouslyActive = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = previouslyActive;
    }

    #endregion

    internal void PlayVideo(int type)
    {
        if (type < 0 || type >= videoScenarios.Length)
        {
            Debug.Log("Invalid video type index: " + type);
            return;
        }

        if (videoScenarios[type].videoClip == null && !forceUrlPlayback)
        {
            Debug.Log("Video clip missing for scenario index: " + type);
            //return;
        }

        currentType = type;
        VideoScenarioData scenario = videoScenarios[type];

        // Stop whatever is currently active (main fallback player OR a previously-active
        // pooled player) before switching.
        if (activePlayer != null)
        {
            activePlayer.Stop();
        }
        videoPlayer.Stop();

        if (isVideoPlaying)
        {
            MusicAudioSource.Stop();
        }

        if (scenario.audioClip != null) MusicAudioSource.clip = scenario.audioClip;

        if (videoDisplayPanel != null)
        {
            videoDisplayPanel.SetActive(true);
        }

        bool pooledExists = pooledPlayers != null && pooledPlayers[type] != null;

        if (pooledExists && preloadReady[type])
        {
            // Fast path: already buffered, starts instantly.
            ActivatePreparedPlayer(type);
            return;
        }

        if (pooledExists)
        {
            // Preload for this scenario is still buffering (e.g. called too soon after
            // PreloadVideos()). Don't re-prepare a second player — just flag it to play
            // the moment its own prepareCompleted fires.
            Debug.Log($"[VideoManager] {scenario.name} requested before preload finished, waiting...");
            pendingPlay[type] = true;
            return;
        }

        // Fallback: no preload pool available for this scenario, use the shared inline
        // player exactly as before.
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

        activePlayer = videoPlayer;

        // Same stale-frame issue applies here: this RenderTexture is shared across
        // every fallback playback, so it still holds the previous video's last frame
        // until the new one is prepared and starts rendering.
        ClearRenderTexture(videoPlayer.targetTexture);

        if (videoDisplayImage != null && videoPlayer.targetTexture != null)
        {
            videoDisplayImage.texture = videoPlayer.targetTexture;
        }

        // Prepare before playing so there's no first-frame stutter/desync with the audio.
        videoPlayer.Prepare();
    }

    private void HandlePrepareCompleted(VideoPlayer source)
    {
        // Play() triggers the "started" callback, where we actually start the audio in sync.
        // (Only wired to the fallback videoPlayer; pooled players are handled in PreloadVideos.)
        source.Play();
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