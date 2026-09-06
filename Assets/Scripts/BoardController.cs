using UnityEngine;
using UnityEngine.InputSystem;

namespace BallRolling.Gameplay
{
    /// <summary>矢印キーでボード（MazeRoot）を傾け、玉を転がす。カメラはボードの子で正対を保つ。</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BoardController : MonoBehaviour
    {
        [SerializeField] private float _maxTiltAngle = 15f;
        [SerializeField] private float _tiltSpeed = 5f;

        private Rigidbody _rigidbody;
        private Quaternion _initialRotation;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            _initialRotation = _rigidbody.rotation;
        }

        private void FixedUpdate()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            var x = (keyboard.rightArrowKey.isPressed ? 1f : 0f) - (keyboard.leftArrowKey.isPressed ? 1f : 0f);
            var z = (keyboard.upArrowKey.isPressed ? 1f : 0f) - (keyboard.downArrowKey.isPressed ? 1f : 0f);

            // 上キー: 玉を画面奥(+Z)へ → 手前が下がる (X+回転)
            // 右キー: 玉を右(+X)へ → 右端が下がる (Z-回転)
            var target = _initialRotation * Quaternion.Euler(z * _maxTiltAngle, 0f, -x * _maxTiltAngle);
            var next = Quaternion.Slerp(_rigidbody.rotation, target, _tiltSpeed * Time.fixedDeltaTime);

            // Kinematic Rigidbody の回転は MoveRotation で物理エンジンに正しく伝える。
            // transform.rotation の直接書き換えはテレポート扱いになり、接触する玉に傾斜が作用しない。
            _rigidbody.MoveRotation(next);
        }

        public void Configure(float maxTiltAngle, float tiltSpeed)
        {
            _maxTiltAngle = maxTiltAngle;
            _tiltSpeed = tiltSpeed;
        }
    }
}
