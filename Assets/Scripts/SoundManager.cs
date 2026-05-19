using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// Simple global audio manager for BGM + SFX.
/// Drop one instance into your initial scene (or create via prefab) and it will persist.
/// </summary>
[DisallowMultipleComponent]
public sealed class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    public enum SoundId
    {
        Interact = 0,
        DialogueContinue = 1, // 对话继续音效
    }

    [System.Serializable]
    public sealed class SfxEntry
    {
        public SoundId id;
        public AudioClip clip;
        [Min(0f)] public float volumeScale = 1f;
    }

    [Header("Volumes")]
    [Range(0f, 1f)] public float bgmVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("BGM")]
    [Tooltip("Optional default BGM played on start.")]
    public AudioClip defaultBgm;
    [Min(0f)] public float defaultBgmFadeSeconds = 0.5f;
    
    [Tooltip("BGM for normal world.")]
    public AudioClip normalWorldBgm;
    [Tooltip("BGM for inner world.")]
    public AudioClip innerWorldBgm;

    [Header("SFX")]
    [Tooltip("Global SFX volume multiplier for typewriter effects, etc.")]
    [Range(0f, 1f)] public float typewriterSfxMultiplier = 1f;

    [Tooltip("SFX lookup table by id. Add new sounds here for easy management.")]
    public List<SfxEntry> sfxTable = new List<SfxEntry>();

    [Header("Input (optional)")]
    [Tooltip("If enabled, SoundManager will listen to Interact key and play SoundId.Interact.")]
    public bool listenForInteractKey = false;
    public KeyCode interactKey = KeyCode.E;

    private AudioSource bgmA;
    private AudioSource bgmB;
    private AudioSource sfx;
    private bool bgmUsingA = true;
    private Coroutine bgmFadeCoroutine;

    private Dictionary<SoundId, SfxEntry> sfxLookup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmA = CreateChildSource("BGM_A", loop: true);
        bgmB = CreateChildSource("BGM_B", loop: true);
        sfx = CreateChildSource("SFX", loop: false);

        BuildSfxLookup();
        ApplyVolumes();
    }

    private void OnEnable()
    {
        // 注册对话系统事件
        DialogueManager.instance.conversationStarted += OnConversationStarted;
        DialogueManager.instance.conversationEnded += OnConversationEnded;
    }

    private void OnDisable()
    {
        // 取消注册对话系统事件
        if (DialogueManager.instance != null)
        {
            DialogueManager.instance.conversationStarted -= OnConversationStarted;
            DialogueManager.instance.conversationEnded -= OnConversationEnded;
        }
    }

    private void Start()
    {
        if (defaultBgm != null)
        {
            PlayBgm(defaultBgm, defaultBgmFadeSeconds);
        }
    }

    private void Update()
    {
        if (!listenForInteractKey) return;
        if (Input.GetKeyDown(interactKey))
        {
            Play(SoundId.Interact);
        }
    }

    // 对话系统事件处理器
    public void OnConversationContinue()
    {
        // 播放对话继续音效
        Play(SoundId.DialogueContinue);
    }

    private void OnConversationStarted(Transform actor)
    {
        // 对话开始时的处理
    }

    private void OnConversationEnded(Transform actor)
    {
        // 对话结束时的处理
    }

    public void SetBgmVolume(float volume01)
    {
        bgmVolume = Mathf.Clamp01(volume01);
        ApplyVolumes();
    }

    public void SetSfxVolume(float volume01)
    {
        sfxVolume = Mathf.Clamp01(volume01);
        ApplyVolumes();
    }

    public void ApplyVolumes()
    {
        var bgmVol = bgmVolume;
        if (bgmA != null) bgmA.volume = bgmVol;
        if (bgmB != null) bgmB.volume = bgmVol;
        if (sfx != null) sfx.volume = sfxVolume;
    }

    public void PlayBgm(AudioClip clip, float fadeSeconds = 0.5f, bool loop = true)
    {
        if (clip == null) return;

        var from = bgmUsingA ? bgmA : bgmB;
        var to = bgmUsingA ? bgmB : bgmA;
        bgmUsingA = !bgmUsingA;

        to.loop = loop;
        to.clip = clip;
        to.volume = 0f;
        to.Play();

        if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
        bgmFadeCoroutine = StartCoroutine(CrossfadeCoroutine(from, to, fadeSeconds));
    }

    public void StopBgm(float fadeSeconds = 0.25f)
    {
        if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
        bgmFadeCoroutine = StartCoroutine(StopBgmCoroutine(fadeSeconds));
    }

    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        if (sfx == null) return;
        sfx.PlayOneShot(clip, Mathf.Clamp01(sfxVolume) * Mathf.Clamp01(volumeScale));
    }

    public void Play(SoundId id, float volumeScale = 1f)
    {
        if (sfxLookup == null || sfxLookup.Count == 0) BuildSfxLookup();
        if (sfxLookup != null && sfxLookup.TryGetValue(id, out var entry) && entry != null)
        {
            PlaySfx(entry.clip, volumeScale * Mathf.Max(0f, entry.volumeScale));
        }
    }

    public void PlayTypewriterSfx(AudioClip clip, float volumeScale = 1f)
    {
        PlaySfx(clip, volumeScale * typewriterSfxMultiplier);
    }

    public void SwitchBgmByWorld(bool isInnerWorld)
    {
        AudioClip targetBgm = isInnerWorld ? innerWorldBgm : normalWorldBgm;
        if (targetBgm != null)
        {
            PlayBgm(targetBgm, defaultBgmFadeSeconds);
        }
    }

    public void RebuildSfxTable()
    {
        BuildSfxLookup();
    }

    private AudioSource CreateChildSource(string name, bool loop)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var a = go.AddComponent<AudioSource>();
        a.playOnAwake = false;
        a.loop = loop;
        a.spatialBlend = 0f; // 2D
        return a;
    }

    private void BuildSfxLookup()
    {
        if (sfxLookup == null) sfxLookup = new Dictionary<SoundId, SfxEntry>();
        else sfxLookup.Clear();

        if (sfxTable == null) return;
        for (int i = 0; i < sfxTable.Count; i++)
        {
            var e = sfxTable[i];
            if (e == null) continue;
            if (sfxLookup.ContainsKey(e.id)) continue; // keep first entry to avoid silent overrides
            sfxLookup.Add(e.id, e);
        }
    }

    private IEnumerator CrossfadeCoroutine(AudioSource from, AudioSource to, float fadeSeconds)
    {
        fadeSeconds = Mathf.Max(0f, fadeSeconds);
        if (fadeSeconds <= 0f)
        {
            if (from != null) { from.Stop(); from.volume = bgmVolume; }
            if (to != null) to.volume = bgmVolume;
            yield break;
        }

        var t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            var k = Mathf.Clamp01(t / fadeSeconds);
            if (to != null) to.volume = bgmVolume * k;
            if (from != null) from.volume = bgmVolume * (1f - k);
            yield return null;
        }
        if (to != null) to.volume = bgmVolume;
        if (from != null) from.Stop();
    }

    private IEnumerator StopBgmCoroutine(float fadeSeconds)
    {
        fadeSeconds = Mathf.Max(0f, fadeSeconds);
        var a = bgmA;
        var b = bgmB;
        if (fadeSeconds <= 0f)
        {
            if (a != null) a.Stop();
            if (b != null) b.Stop();
            yield break;
        }

        var startA = (a != null) ? a.volume : 0f;
        var startB = (b != null) ? b.volume : 0f;
        var t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            var k = 1f - Mathf.Clamp01(t / fadeSeconds);
            if (a != null) a.volume = startA * k;
            if (b != null) b.volume = startB * k;
            yield return null;
        }
        if (a != null) a.Stop();
        if (b != null) b.Stop();
        ApplyVolumes();
    }
}

