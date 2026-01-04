using System.Linq;
using UnityEngine;

public class FirstPersonAudio : MonoBehaviour
{
    public FirstPersonMovement character;
    public GroundCheck groundCheck;

    [Header("Step")]
    public AudioSource stepAudio;
    public AudioSource runningAudio;
    [Tooltip("Minimum velocity for moving audio to play")]
    public float velocityThreshold = .01f;
    private Vector2 lastCharacterPosition;
    private Vector2 CurrentCharacterPosition => new Vector2(character.transform.position.x, character.transform.position.z);

    [Header("Landing")]
    public AudioSource landingAudio;
    public AudioClip[] landingSFX;

    [Header("Jump")]
    public Jump jump;
    public AudioSource jumpAudio;
    public AudioClip[] jumpSFX;

    [Header("Head Bob Sync")]
    [Tooltip("Adim seslerini head bob dongusune gore tetikler.")]
    [SerializeField] private FirstPersonHeadBob headBob;
    [SerializeField] private bool syncStepsWithBob = true;
    [Tooltip("Head bob timeri bu araligi gectiginde yeni bir adim calar (pi/2 = iki adim).")]
    [SerializeField] private float bobStepInterval = Mathf.PI * 0.5f;
    [Tooltip("Head bob sinusu asagi inerken adim cal")]
    [SerializeField] private bool useDownSwingCue = true;
    [SerializeField] private float downSwingThreshold = -0.95f;
    private float nextBobStep;
    private bool stepTriggered;
    private float lastVertSin;

    [Header("Crouch")]
    public Crouch crouch;
    public AudioSource crouchStartAudio, crouchedAudio, crouchEndAudio;
    public AudioClip[] crouchStartSFX, crouchEndSFX;

    private AudioSource[] MovingAudios => new AudioSource[] { stepAudio, runningAudio, crouchedAudio };

    private void Awake()
    {
        EnsureNonLooping(stepAudio);
        EnsureNonLooping(runningAudio);
        EnsureNonLooping(crouchedAudio);
    }

    void Reset()
    {
        character = GetComponentInParent<FirstPersonMovement>();
        groundCheck = (transform.parent ?? transform).GetComponentInChildren<GroundCheck>();
        stepAudio = GetOrCreateAudioSource("Step Audio");
        runningAudio = GetOrCreateAudioSource("Running Audio");
        landingAudio = GetOrCreateAudioSource("Landing Audio");

        jump = GetComponentInParent<Jump>();
        if (jump)
            jumpAudio = GetOrCreateAudioSource("Jump audio");

        crouch = GetComponentInParent<Crouch>();
        if (crouch)
        {
            crouchStartAudio = GetOrCreateAudioSource("Crouch Start Audio");
            crouchedAudio = GetOrCreateAudioSource("Crouched Audio");
            crouchEndAudio = GetOrCreateAudioSource("Crouch End Audio");
        }
    }

    void OnEnable() => SubscribeToEvents();
    void OnDisable() => UnsubscribeToEvents();

    void FixedUpdate()
    {
        if (syncStepsWithBob && headBob != null)
        {
            HandleBobSyncedSteps();
            lastCharacterPosition = CurrentCharacterPosition;
            return;
        }

        float velocity = Vector3.Distance(CurrentCharacterPosition, lastCharacterPosition);
        if (velocity >= velocityThreshold && groundCheck && groundCheck.isGrounded)
        {
            if (crouch && crouch.IsCrouched)
                SetPlayingMovingAudio(crouchedAudio);
            else if (character.IsRunning)
                SetPlayingMovingAudio(runningAudio);
            else
                SetPlayingMovingAudio(stepAudio);
        }
        else
        {
            SetPlayingMovingAudio(null);
        }

        lastCharacterPosition = CurrentCharacterPosition;
    }

    private void HandleBobSyncedSteps()
    {
        if (groundCheck == null || !groundCheck.isGrounded || headBob == null)
        {
            SetPlayingMovingAudio(null);
            nextBobStep = 0f;
            stepTriggered = false;
            return;
        }

        if (!headBob.IsBobbing)
        {
            SetPlayingMovingAudio(null);
            nextBobStep = headBob.BobTimer + bobStepInterval;
            stepTriggered = false;
            return;
        }

        float vertSin = Mathf.Sin(headBob.BobTimer * 2f);

        if (useDownSwingCue)
        {
            if (!stepTriggered && vertSin <= downSwingThreshold && lastVertSin > downSwingThreshold)
            {
                PlayStepForState();
                stepTriggered = true;
            }
            if (vertSin > downSwingThreshold)
                stepTriggered = false;
            lastVertSin = vertSin;
            return;
        }

        if (nextBobStep <= 0f)
            nextBobStep = headBob.BobTimer + bobStepInterval;

        if (headBob.BobTimer >= nextBobStep)
        {
            PlayStepForState();
            nextBobStep += bobStepInterval;
        }
    }

    private void PlayStepForState()
    {
        AudioSource stepSrc = stepAudio;
        if (crouch && crouch.IsCrouched)
            stepSrc = crouchedAudio;
        else if (character != null && character.IsRunning)
            stepSrc = runningAudio;

        PlayStepOnce(stepSrc);
    }

    private void PlayStepOnce(AudioSource src)
    {
        if (src == null || src.clip == null)
            return;

        foreach (var audio in MovingAudios.Where(a => a != src && a != null))
            audio.Pause();

        src.PlayOneShot(src.clip);
    }

    void SetPlayingMovingAudio(AudioSource audioToPlay)
    {
        EnsureNonLooping(stepAudio);
        EnsureNonLooping(runningAudio);
        EnsureNonLooping(crouchedAudio);

        foreach (var audio in MovingAudios.Where(audio => audio != audioToPlay && audio != null))
            audio.Pause();

        if (audioToPlay && !audioToPlay.isPlaying)
            audioToPlay.Play();
    }

    void PlayLandingAudio() => PlayRandomClip(landingAudio, landingSFX);
    void PlayJumpAudio() => PlayRandomClip(jumpAudio, jumpSFX);
    void PlayCrouchStartAudio() => PlayRandomClip(crouchStartAudio, crouchStartSFX);
    void PlayCrouchEndAudio() => PlayRandomClip(crouchEndAudio, crouchEndSFX);

    void SubscribeToEvents()
    {
        if (groundCheck != null)
            groundCheck.Grounded += PlayLandingAudio;

        if (jump)
            jump.Jumped += PlayJumpAudio;

        if (crouch)
        {
            crouch.CrouchStart += PlayCrouchStartAudio;
            crouch.CrouchEnd += PlayCrouchEndAudio;
        }
    }

    void UnsubscribeToEvents()
    {
        if (groundCheck != null)
            groundCheck.Grounded -= PlayLandingAudio;

        if (jump)
            jump.Jumped -= PlayJumpAudio;

        if (crouch)
        {
            crouch.CrouchStart -= PlayCrouchStartAudio;
            crouch.CrouchEnd -= PlayCrouchEndAudio;
        }
    }

    AudioSource GetOrCreateAudioSource(string name)
    {
        AudioSource result = System.Array.Find(GetComponentsInChildren<AudioSource>(), a => a.name == name);
        if (result)
            return result;

        result = new GameObject(name).AddComponent<AudioSource>();
        result.spatialBlend = 1;
        result.playOnAwake = false;
        result.transform.SetParent(transform, false);
        return result;
    }

    static void PlayRandomClip(AudioSource audio, AudioClip[] clips)
    {
        if (!audio || clips == null || clips.Length <= 0)
            return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clips.Length > 1)
            while (clip == audio.clip)
                clip = clips[Random.Range(0, clips.Length)];

        audio.clip = clip;
        audio.Play();
    }

    private void EnsureNonLooping(AudioSource src)
    {
        if (src != null && src.loop)
            src.loop = false;
    }
}
