using UnityEngine;

namespace BallRolling.Gameplay
{
    /// <summary>迷路内に配置される取得アイテム。玉が侵入すると GameController へ通知して自己破棄する。</summary>
    public class ItemPickup : MonoBehaviour
    {
        [SerializeField] private float _rotationSpeedDegrees = 90f;

        private GameController _controller;

        /// <summary>回転速度（生成側から設定可能。未設定なら Inspector の既定値を使う）。</summary>
        public float RotationSpeedDegrees
        {
            get { return _rotationSpeedDegrees; }
            set { _rotationSpeedDegrees = value; }
        }

        private void Update()
        {
            // トリガーコライダーは物理応答しないため transform 直接回転で安全
            transform.Rotate(0f, _rotationSpeedDegrees * Time.deltaTime, 0f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Ball"))
            {
                // タグ不一致は無音でスキップされると原因追跡が困難なため警告を出す
                Debug.LogWarning($"ItemPickup: タグ不一致で取得スキップ ({other.name}, {other.tag})", other);
                return;
            }

            // 遅延解決: 生成経路（エディター生成/ランタイム再生成）がGameControllerを知らないため取得時に探す
            if (_controller == null)
                _controller = FindAnyObjectByType<GameController>();
            if (_controller != null)
                _controller.NotifyItemPicked();

            Destroy(gameObject);
        }
    }
}
