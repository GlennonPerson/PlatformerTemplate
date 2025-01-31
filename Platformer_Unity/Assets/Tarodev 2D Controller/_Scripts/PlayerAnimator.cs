using UnityEngine;

namespace TarodevController
{
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator _anim;
        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private Transform _characterRoot;

        [Header("Scale Settings")]
        [SerializeField] private Vector3 _baseScale = Vector3.one;

        [Header("Idle Scale Effect")]
        [SerializeField] private float _idleScaleFrequency = 2f;
        [SerializeField] private float _idleScaleAmplitude = 0.05f;
        [SerializeField] private float _idleScaleOffset = 0.95f;

        [Header("Particles")]
        [SerializeField] private ParticleSystem _jumpParticles;
        [SerializeField] private ParticleSystem _launchParticles;
        [SerializeField] private ParticleSystem _moveParticles;
        [SerializeField] private ParticleSystem _landParticles;

        private IPlayerController _player;
        private bool _grounded;
        private bool _facingRight = true;
        private ParticleSystem.MinMaxGradient _currentGradient;

        // Animation State Names
        private static readonly int RunLeft = Animator.StringToHash("RunLeft");
        private static readonly int RunRight = Animator.StringToHash("RunRight");
        private static readonly int JumpLeft = Animator.StringToHash("JumpLeft");
        private static readonly int JumpRight = Animator.StringToHash("JumpRight");
        private static readonly int Stand = Animator.StringToHash("Stand");

        private void Awake()
        {
            // If animator not set, try to get it from child object
            if (_anim == null)
                _anim = GetComponentInChildren<Animator>();

            // If sprite not set, try to get it from the same object as animator
            if (_sprite == null && _anim != null)
                _sprite = _anim.GetComponent<SpriteRenderer>();

            // If character root not set, use the animator's transform
            if (_characterRoot == null && _anim != null)
                _characterRoot = _anim.transform;

            _player = GetComponentInParent<IPlayerController>();
        }

        private void OnEnable()
        {
            _player.Jumped += OnJumped;
            _player.GroundedChanged += OnGroundedChanged;
            _moveParticles.Play();
        }

        private void OnDisable()
        {
            _player.Jumped -= OnJumped;
            _player.GroundedChanged -= OnGroundedChanged;
            _moveParticles.Stop();
        }

        private void Update()
        {
            if (_player == null) return;

            DetectGroundColor();
            HandleAnimationStates();
            HandleIdleEffect();
            UpdateMoveParticles();
        }

        private void HandleAnimationStates()
        {
            float inputX = _player.FrameInput.x;

            // Update facing direction
            if (inputX != 0)
            {
                _facingRight = inputX > 0;
            }

            // If in the air, play jump animations
            if (!_grounded)
            {
                _anim.Play(_facingRight ? JumpRight : JumpLeft);
            }
            // If on ground, handle running and standing
            else
            {
                if (Mathf.Abs(inputX) > 0.1f)
                {
                    // Running
                    _anim.Play(_facingRight ? RunRight : RunLeft);
                }
                else
                {
                    // Standing idle
                    _anim.Play(Stand);
                }
            }
        }

        private void HandleIdleEffect()
        {
            var inputStrength = Mathf.Abs(_player.FrameInput.x);

            if (_grounded && inputStrength < 0.1f)
            {
                float scaleY = Mathf.Sin(Time.time * _idleScaleFrequency) * _idleScaleAmplitude + _idleScaleOffset;
                _characterRoot.localScale = new Vector3(
                    _baseScale.x / scaleY,
                    _baseScale.y * scaleY,
                    _baseScale.z
                );
            }
            else
            {
                _characterRoot.localScale = Vector3.MoveTowards(_characterRoot.localScale, _baseScale, 2f * Time.deltaTime);
            }
        }

        private void UpdateMoveParticles()
        {
            var inputStrength = Mathf.Abs(_player.FrameInput.x);
            _moveParticles.transform.localScale = Vector3.MoveTowards(
                _moveParticles.transform.localScale,
                Vector3.one * inputStrength,
                2 * Time.deltaTime
            );
        }

        private void OnJumped()
        {
            if (_grounded)
            {
                SetColor(_jumpParticles);
                SetColor(_launchParticles);
                _jumpParticles.Play();
            }
        }

        private void OnGroundedChanged(bool grounded, float impact)
        {
            _grounded = grounded;

            if (grounded)
            {
                DetectGroundColor();
                SetColor(_landParticles);
                _moveParticles.Play();
                _landParticles.transform.localScale = Vector3.one * Mathf.InverseLerp(0, 40, impact);
                _landParticles.Play();
            }
            else
            {
                _moveParticles.Stop();
            }
        }

        private void DetectGroundColor()
        {
            var hit = Physics2D.Raycast(transform.position, Vector3.down, 2);

            if (!hit || hit.collider.isTrigger || !hit.transform.TryGetComponent(out SpriteRenderer r)) return;
            var color = r.color;
            _currentGradient = new ParticleSystem.MinMaxGradient(color * 0.9f, color * 1.2f);
            SetColor(_moveParticles);
        }

        private void SetColor(ParticleSystem ps)
        {
            var main = ps.main;
            main.startColor = _currentGradient;
        }
    }
}