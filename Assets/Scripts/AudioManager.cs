using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Clips")]
    public AudioClip bgmClip;
    public AudioClip arrowShootClip;
    public AudioClip fireShootClip;
    public AudioClip frostShootClip;
    public AudioClip enemyHitClip;
    public AudioClip clickClip; 
    public AudioClip baseDamageClip;
    public AudioClip coinCollectClip; 

    [Header("UI Setup")]
    [Tooltip("Drop your UI Panels or Manager GameObjects here. This script will find all buttons inside them automatically.")]
    public GameObject[] uiManagersToScan;

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    private bool isMuted = false;

    void Awake()
    {
        // Singleton Setup
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Setup Music Source
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = 0.5f;
        musicSource.playOnAwake = false;

        // Setup SFX Source
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.volume = 1.0f;

        StartMusic();
    }

    void Start()
    {
        // Automatically hook into all buttons found in the provided managers
        AssignButtonSounds();
    }

    void StartMusic()
    {
        if (bgmClip != null)
        {
            musicSource.clip = bgmClip;
            musicSource.Play();
        }
    }

    // Scans the uiManagersToScan array and adds a click sound listener to every button found.
    public void AssignButtonSounds()
    {
        if (uiManagersToScan == null) return;

        foreach (GameObject manager in uiManagersToScan)
        {
            if (manager == null) continue;

            // Find all Button components in this manager and its children
            Button[] buttons = manager.GetComponentsInChildren<Button>(true);
            foreach (Button btn in buttons)
            {
                // We add a listener that triggers our PlayClickSound method
                btn.onClick.AddListener(PlayClickSound);
            }
        }
    }

    public void PlayClickSound()
    {
        // Randomize pitch specifically for clicks to keep it from feeling repetitive
        float randomPitch = Random.Range(0.85f, 1.15f);
        PlaySFX(clickClip, 0.7f, randomPitch);
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
        AudioListener.volume = isMuted ? 0f : 1f;
    }

    public void PlayTowerShot(TowerType type)
    {
        switch (type)
        {
            case TowerType.Arrow:
                PlaySFX(arrowShootClip);
                break;
            case TowerType.Fire:
                PlaySFX(fireShootClip, 0.35f); 
                break;
            case TowerType.Frost:
                PlaySFX(frostShootClip);
                break;
        }
    }

    public void PlayEnemyHit()
    {
        PlaySFX(enemyHitClip);
    }

    public void PlayBaseDamageSound()
    {
        // Play with a slightly lower pitch range to make it feel "heavy"
        float randomPitch = Random.Range(0.9f, 1.0f);
        PlaySFX(baseDamageClip, 1.0f, randomPitch);
    }

    // Method to play the coin collection sound
    public void PlayCoinCollectSound()
    {
        // Higher pitch for positive feedback
        float randomPitch = Random.Range(1.1f, 1.3f);
        PlaySFX(coinCollectClip, 0.8f, randomPitch);
    }

    private void PlaySFX(AudioClip clip, float volumeScale = 1.0f, float pitchOverride = -1f)
    {
        if (clip == null) return;

        if (pitchOverride > 0)
        {
            sfxSource.pitch = pitchOverride;
        }
        else
        {
            // Default tight randomization for towers/enemy hits
            sfxSource.pitch = Random.Range(0.95f, 1.05f);
        }

        sfxSource.PlayOneShot(clip, volumeScale);
    }
}