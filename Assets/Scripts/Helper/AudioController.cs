using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
internal class AudioController : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgMusicSource;
    [SerializeField] private AudioSource gameSoundSource;
    // [SerializeField] private AudioSource uiSource;

    [Header("Background")]
    [SerializeField] private AudioClip bgMusic;
    [SerializeField] private AudioClip beatitbgMusic;
    [SerializeField] private AudioClip smoothCriminalbgMusic;
    [SerializeField] private AudioClip BonusWheelBG;

    [Header("Game Sounds")]
    [SerializeField] private AudioClip normalWin;
    [SerializeField] private AudioClip bigWin;
    [SerializeField] private AudioClip CollosalWin;
    [SerializeField] private AudioClip uiButton;
    [SerializeField] private AudioClip spinButton;
    [SerializeField] private AudioClip WinLine;
    [SerializeField] private AudioClip MichealSnap;
    [SerializeField] private AudioClip StackWild;
    [SerializeField] private AudioClip DanceMoves;
    [SerializeField] private AudioClip OuterWheel;
    [SerializeField] private AudioClip InnerWheel;
    [SerializeField] private AudioClip BonusWheelStart;
    [SerializeField] private AudioClip BonusTrackMultiplier;
    [SerializeField] private AudioClip BonusWheelSparkle;
    [SerializeField] private AudioClip BonusIconPop;
    [SerializeField] private AudioClip BeatItStart;
    [SerializeField] private AudioClip SmoothCriminalStart;


    [Header("Sound Buttons")]
    [SerializeField] private Button SoundButton;
    [SerializeField] private Button MusicButton;
    [SerializeField] private Sprite SoundOnSprite;
    [SerializeField] private Sprite SoundOffSprite;
    [SerializeField] private Sprite MusicOnSprite;
    [SerializeField] private Sprite MusicOffSprite;

    private bool isGameMuted = false;
    private bool isMusicMuted = false;

    private void Start()
    {
        if (SoundButton)
        {
            SoundButton.onClick.RemoveAllListeners();
            SoundButton.onClick.AddListener(ToggleGameSound);
        }

        if (MusicButton)
        {
            MusicButton.onClick.RemoveAllListeners();
            MusicButton.onClick.AddListener(ToggleBackgroundMusic);
        }

        // if (SoundMuteButton)
        // {
        //     SoundMuteButton.onClick.RemoveAllListeners();
        //     SoundMuteButton.onClick.AddListener(ToggleGameSound);
        // }

        // if (MusicMuteButton)
        // {
        //     MusicMuteButton.onClick.RemoveAllListeners();
        //     MusicMuteButton.onClick.AddListener(ToggleBackgroundMusic);
        // }

        PlayBackground();
    }

    private void ToggleGameSound()
    {
        PlayUIButton();
        if (!isGameMuted)
        {
            // SoundMuteButton.gameObject.SetActive(true);
            // SoundButton.gameObject.SetActive(false);
            SoundButton.GetComponent<Image>().sprite = SoundOffSprite;
        }
        else
        {
            // SoundButton.gameObject.SetActive(true);
            // SoundMuteButton.gameObject.SetActive(false);
            SoundButton.GetComponent<Image>().sprite = SoundOnSprite;
        }
        isGameMuted = !isGameMuted;
        MuteGame(isGameMuted);
    }

    private void ToggleBackgroundMusic()
    {
        PlayUIButton();
        if (!isMusicMuted)
        {
            // MusicMuteButton.gameObject.SetActive(true);
            // MusicButton.gameObject.SetActive(false);
            MusicButton.GetComponent<Image>().sprite = MusicOffSprite;
        }
        else
        {
            // MusicButton.gameObject.SetActive(true);
            // MusicMuteButton.gameObject.SetActive(false);
            MusicButton.GetComponent<Image>().sprite = MusicOnSprite;
        }
        isMusicMuted = !isMusicMuted;
        MuteBackground(isMusicMuted);
    }


    internal void PlayBackground()
    {
        if (!bgMusic) return;

        bgMusicSource.clip = bgMusic;
        bgMusicSource.loop = true;
        if (!bgMusicSource.isPlaying)
            bgMusicSource.Play();
    }

    internal void PlayBeatItBackground()
    {
        if (!beatitbgMusic) return;

        bgMusicSource.clip = beatitbgMusic;
        bgMusicSource.loop = true;
        if (!bgMusicSource.isPlaying)
            bgMusicSource.Play();
    }

    internal void PlaySmoothCriminalBackground()
    {
        if (!smoothCriminalbgMusic) return;

        bgMusicSource.clip = smoothCriminalbgMusic;
        bgMusicSource.loop = true;
        if (!bgMusicSource.isPlaying)
            bgMusicSource.Play();
    }

    internal void PlayBonusWheelBackground()
    {
        if (!BonusWheelBG) return;

        bgMusicSource.clip = BonusWheelBG;
        bgMusicSource.loop = true;
        if (!bgMusicSource.isPlaying)
            bgMusicSource.Play();
    }

    internal void StopBackground()
    {
        bgMusicSource.Stop();
    }

    internal void PlayNormalWin()
    {
        PlayGame(normalWin, false);
    }

    internal void PlayBigWin()
    {
        PlayGame(bigWin, false);
    }

    internal void PlayCollosalWin()
    {
        PlayGame(CollosalWin, false);
    }

    internal void PlayUIButton()
    {
        PlayGame(uiButton, false);
    }

    internal void PlaySpinButton()
    {
        PlayGame(spinButton, false);
    }

    internal void PlayWinLine()
    {
        PlayGame(WinLine, false);
    }

    internal void PlayMichealSnap()
    {
        PlayGame(MichealSnap, false);
    }

    internal void PlayStackWild()
    {
        PlayGame(StackWild, false);
    }

    internal void PlayDanceMoves()
    {
        PlayGame(DanceMoves, false);
    }

    internal void PlayOuterWheel()
    {
        PlayGame(OuterWheel, false);
    }

    internal void PlayInnerWheel()
    {
        PlayGame(InnerWheel, false);
    }

    internal void PlayBonusWheelStart()
    {
        PlayGame(BonusWheelStart, false);
    }

    internal void PlayBonusTrackMultiplier()
    {
        PlayGame(BonusTrackMultiplier, false);
    }

    internal void PlayBonusWheelSparkle()
    {
        PlayGame(BonusWheelSparkle, false);
    }

    internal void PlayBonusIconPop()
    {
        PlayGame(BonusIconPop, false);
    }

    internal void PlayBeatItStart()
    {
        PlayGame(BeatItStart, false);
    }

    internal void PlaySmoothCriminalStart()
    {
        PlayGame(SmoothCriminalStart, false);
    }

    private void PlayGame(AudioClip clip, bool loop)
    {
        if (!clip) return;

        gameSoundSource.Stop();
        gameSoundSource.clip = clip;
        gameSoundSource.loop = loop;
        gameSoundSource.Play();
    }

    internal void StopGameAudio()
    {
        gameSoundSource.Stop();
        gameSoundSource.loop = false;
    }

    internal void MuteAll(bool mute)
    {
        bgMusicSource.mute = mute;
        gameSoundSource.mute = mute;
        // uiSource.mute = mute;
    }


    internal void MuteBackground(bool mute) => bgMusicSource.mute = mute;
    internal void MuteGame(bool mute) => gameSoundSource.mute = mute;
    // internal void MuteUI(bool mute) => uiSource.mute = mute;

    private void OnApplicationFocus(bool hasFocus)
    {
        AudioListener.volume = hasFocus ? 1.0f : 0.0f;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        AudioListener.volume = pauseStatus ? 0.0f : 1.0f;
    }
}