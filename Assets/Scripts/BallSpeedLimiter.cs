using UnityEngine;

namespace BallRolling.Gameplay
{
    /// <summary>玉の速度上限を毎物理ステップで制限し、物理爆発・すり抜けを防ぐ。</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BallSpeedLimiter : MonoBehaviour
    {
        [SerializeField] private float _maxLinearSpeed = 12f;
        [SerializeField] private float _maxAngularSpeed = 25f;

        private Rigidbody _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            var velocity = _rigidbody.linearVelocity;
            if (velocity.sqrMagnitude > _maxLinearSpeed * _maxLinearSpeed)
                _rigidbody.linearVelocity = velocity.normalized * _maxLinearSpeed;

            var angularVelocity = _rigidbody.angularVelocity;
            if (angularVelocity.sqrMagnitude > _maxAngularSpeed * _maxAngularSpeed)
                _rigidbody.angularVelocity = angularVelocity.normalized * _maxAngularSpeed;
        }

        public void Configure(float maxLinearSpeed, float maxAngularSpeed)
        {
            _maxLinearSpeed = maxLinearSpeed;
            _maxAngularSpeed = maxAngularSpeed;
        }
    }
}
