using BallRolling.Gameplay.Logic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BallRolling.Gameplay
{
    /// <summary>ゲームループの指揮（開始・タイマー・ゴール判定・再生成）とUI更新を行う。状態は GameLoop のみが保持する。</summary>
    public class GameController : MonoBehaviour
    {
        [SerializeField] private Transform _mazeRoot;
        [SerializeField] private GameObject _ball;
        [SerializeField] private Text _timerText;
        [SerializeField] private Text _messageText;
        [SerializeField] private float _timeLimitSeconds = 180f;
        [SerializeField] private float _ballSpawnHeight = 3f;
        [SerializeField] private float _goalFallLocalY = -1.5f;
        [SerializeField] private int _mazeWidth = 10;
        [SerializeField] private int _mazeHeight = 10;
        [SerializeField] private float _cellSize = 1f;
        [SerializeField] private float _wallHeight = 1.2f;
        [SerializeField] private float _wallThickness = 0.2f;
        [SerializeField] private float _floorThickness = 0.5f;
        [SerializeField] private Material _wallMaterial;
        [SerializeField] private Material _floorMaterial;
        [SerializeField] private Material _entranceMarkerMaterial;
        [SerializeField] private Material _exitMarkerMaterial;

        // フォールバックマテリアルの色（Step2_MazeBuildParameters の既定値と同値）
        private static readonly Color WallColor = new Color(0.75f, 0.75f, 0.8f);
        private static readonly Color FloorColor = new Color(0.55f, 0.6f, 0.65f);
        private static readonly Color EntranceColor = new Color(0.2f, 0.85f, 0.3f);
        private static readonly Color ExitColor = new Color(0.9f, 0.25f, 0.25f);

        private GameLoop _loop;
        private MazeBuilder _builder;
        private MazeModel _currentMaze;
        private MazeMaterials? _resolvedMaterials;
        private string _lastTimerText;

        private void Awake()
        {
            _loop = new GameLoop();
            _loop.Configure(_timeLimitSeconds);
            _builder = new MazeBuilder();

            if (_ball == null)
                Debug.LogError("GameController: 玉が設定されていません。Step3 Play Setup を先に実行して下さい。");
            else
                _ball.SetActive(false); // 待機中は玉を隠す（開始時に上から落下して登場）

            SetBoardInputEnabled(false);
            ApplyWaitingUi();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                if (_loop.Phase == GamePhase.WaitingToStart)
                    StartPlay();
                else if (_loop.CanRestart)
                    RestartGame();
            }

            if (_loop.Phase != GamePhase.Playing)
                return;

            _loop.Tick(Time.deltaTime);
            UpdateTimerText();

            if (_loop.Phase == GamePhase.TimeUp)
                OnTimeUp();
            else
                CheckGoal();
        }

        private void StartPlay()
        {
            SpawnBall();
            _loop.StartGame();
            SetBoardInputEnabled(true);
            if (_messageText != null)
                _messageText.text = string.Empty;
            UpdateTimerText();
        }

        private void RestartGame()
        {
            RegenerateMaze();
            _loop.Restart();
            SetBoardInputEnabled(false);
            ApplyWaitingUi();
        }

        private void SpawnBall()
        {
            if (_ball == null)
                return;

            _ball.SetActive(true);
            var entrance = _currentMaze != null ? _currentMaze.Entrance : Vector2Int.zero;
            _ball.transform.localPosition = new Vector3(
                (entrance.x + 0.5f) * _cellSize, _ballSpawnHeight, (entrance.y + 0.5f) * _cellSize);

            var rigidbody = _ball.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
                rigidbody.WakeUp();
            }
        }

        private void CheckGoal()
        {
            if (_ball == null)
                return;

            // MazeRootは最大15°傾くため、判定はローカル座標で行う
            var localPosition = _ball.transform.localPosition;
            if (localPosition.y >= _goalFallLocalY)
                return;

            var exit = _currentMaze != null ? _currentMaze.Exit : new Vector2Int(_mazeWidth - 1, _mazeHeight - 1);
            var minX = exit.x * _cellSize;
            var maxX = (exit.x + 1f) * _cellSize;
            var minZ = exit.y * _cellSize;
            var maxZ = (exit.y + 1f) * _cellSize;
            if (localPosition.x < minX || localPosition.x > maxX || localPosition.z < minZ || localPosition.z > maxZ)
                return;

            _loop.NotifyGoal();
            _ball.SetActive(false); // ゴール後は即非表示（落下演出なし）
            SetBoardInputEnabled(false);

            var score = ScoreCalculator.Calculate(_loop.ElapsedSeconds, hasItem: false);
            if (_messageText != null)
                _messageText.text = ScoreCalculator.RenderStars(score) + "\nPRESS SPACE TO RESTART";
        }

        private void OnTimeUp()
        {
            SetBoardInputEnabled(false);
            if (_messageText != null)
                _messageText.text = "TIME UP\nPRESS SPACE TO RESTART";
        }

        private void RegenerateMaze()
        {
            // リスタート毎にシードはランダム（要件: リスタートでランダム生成）
            var seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            _currentMaze = MazeGenerator.Generate(_mazeWidth, _mazeHeight, seed);

            _builder.Clear(_mazeRoot);
            _builder.Build(_currentMaze, CreateGeometry(), _mazeRoot, ResolveMaterials());

            var board = _mazeRoot != null ? _mazeRoot.GetComponent<BoardController>() : null;
            if (board != null)
                board.ResetRotation();
        }

        private MazeGeometry CreateGeometry()
        {
            return new MazeGeometry
            {
                CellSize = _cellSize,
                WallHeight = _wallHeight,
                WallThickness = _wallThickness,
                FloorThickness = _floorThickness
            };
        }

        private MazeMaterials ResolveMaterials()
        {
            if (_resolvedMaterials.HasValue)
                return _resolvedMaterials.Value;

            // SerializeField参照を優先し、未設定の場合のみ実行時生成する（AssetDatabaseは実行時に使えない）
            var materials = new MazeMaterials
            {
                Wall = _wallMaterial != null ? _wallMaterial : CreateRuntimeMaterial(WallColor),
                Floor = _floorMaterial != null ? _floorMaterial : CreateRuntimeMaterial(FloorColor),
                EntranceMarker = _entranceMarkerMaterial != null ? _entranceMarkerMaterial : CreateRuntimeMaterial(EntranceColor),
                ExitMarker = _exitMarkerMaterial != null ? _exitMarkerMaterial : CreateRuntimeMaterial(ExitColor)
            };
            _resolvedMaterials = materials;
            return materials;
        }

        private static Material CreateRuntimeMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = new Material(shader);
            material.color = color;
            return material;
        }

        private void SetBoardInputEnabled(bool isEnabled)
        {
            var board = _mazeRoot != null ? _mazeRoot.GetComponent<BoardController>() : null;
            if (board != null)
                board.SetInputEnabled(isEnabled);
        }

        private void ApplyWaitingUi()
        {
            _lastTimerText = GameLoop.FormatTime(_timeLimitSeconds);
            if (_timerText != null)
                _timerText.text = _lastTimerText;
            if (_messageText != null)
                _messageText.text = "PRESS SPACE";
        }

        private void UpdateTimerText()
        {
            if (_timerText == null)
                return;

            // 文字列が変わった時だけ書き換え、毎フレームの文字列生成を避ける
            var text = GameLoop.FormatTime(_loop.RemainingSeconds);
            if (text == _lastTimerText)
                return;

            _lastTimerText = text;
            _timerText.text = text;
        }
    }
}
