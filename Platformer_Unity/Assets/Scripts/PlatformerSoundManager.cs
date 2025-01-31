using UnityEngine;
using FMODUnity;
using FMOD.Studio;
namespace TarodevController
{
    public class PlatformerSoundManager : MonoBehaviour
    {
        // Singleton pattern
        public static PlatformerSoundManager Instance { get; private set; }

        [Header("FMOD Events")]
        [SerializeField] private EventReference backgroundMusicRef;
        [SerializeField] private EventReference jumpSoundRef;
        [SerializeField] private EventReference landSoundRef;

        [Header("Sound Settings")]
        [SerializeField] private float landSoundCooldown = 0.1f;
        [SerializeField] private bool enableBackgroundMusic = true; // New toggle for background music

        private IPlayerController _playerController;
        private EventInstance musicEventInstance;
        private EventInstance jumpEventInstance;
        private float lastLandTime;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            _playerController = FindObjectOfType<PlayerController>();
            if (_playerController == null)
            {
                Debug.LogError("No PlayerController found in the scene!");
                return;
            }

            // Only start background music if enabled
            if (enableBackgroundMusic)
            {
                StartBackgroundMusic();
            }

            jumpEventInstance = RuntimeManager.CreateInstance(jumpSoundRef);
        }

        private void OnEnable()
        {
            if (_playerController != null)
            {
                _playerController.Jumped += OnPlayerJump;
                _playerController.GroundedChanged += OnPlayerGroundedChanged;
            }
        }

        private void OnDisable()
        {
            if (_playerController != null)
            {
                _playerController.Jumped -= OnPlayerJump;
                _playerController.GroundedChanged -= OnPlayerGroundedChanged;
            }
        }

        private void StartBackgroundMusic()
        {
            musicEventInstance = RuntimeManager.CreateInstance(backgroundMusicRef);
            musicEventInstance.start();
        }

        private void OnPlayerJump()
        {
            jumpEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            jumpEventInstance.start();
        }

        private void OnPlayerGroundedChanged(bool isGrounded, float impactVelocity)
        {
            if (isGrounded && impactVelocity > 5f)
            {
                // Check if enough time has passed since the last land sound
                if (Time.time - lastLandTime >= landSoundCooldown)
                {
                    jumpEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                    RuntimeManager.PlayOneShot(landSoundRef);
                    lastLandTime = Time.time;
                }
            }
        }

        private void OnDestroy()
        {
            if (musicEventInstance.isValid()) // Check if instance exists before stopping
            {
                musicEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                musicEventInstance.release();
            }

            jumpEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            jumpEventInstance.release();
        }

        public void PlayJumpSound()
        {
            jumpEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            jumpEventInstance.start();
        }

        public void PlayLandSound()
        {
            if (Time.time - lastLandTime >= landSoundCooldown)
            {
                RuntimeManager.PlayOneShot(landSoundRef);
                lastLandTime = Time.time;
            }
        }
    }
}