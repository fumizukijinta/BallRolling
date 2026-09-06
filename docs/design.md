# 詳細設計 / プログラム設計 — BallRolling

要件: [requirements.md](requirements.md) 参照。実装変更時は本書も更新する。

## アーキテクチャ

純粋C#ロジック（EditModeテスト対象）とUnity固有（MonoBehaviour）を分離し、`Assets/Scripts/`配下に配置（名前空間 `BallRolling.Gameplay` / `BallRolling.Gameplay.Logic`）。

```
Assets/Scripts/
├── Logic/          … 純粋C#（テスト可能）
│   ├── GameLoop.cs
│   ├── MazeGenerator.cs
│   ├── MazeModel.cs
│   └── ScoreCalculator.cs
├── GameController.cs
├── BoardController.cs
├── MazeBuilder.cs
└── ItemPickup.cs
Assets/Tests/EditMode/ … Logicのテスト（asmdef: BallRolling.Tests.EditMode）
```

## 純粋ロジック（Logic/）

### MazeModel
- `Cell { bool North, South, East, West; }`（壁の有無）を `Cell[10,10]` で保持
- `Width/Height`、`Entrance = (0,0)`、`Exit = (9,9)`

### MazeGenerator
- 再帰的バックトラッカー（スタック実装）。乱数は `System.Random(seed)` 注入（テスト再現性）
- 保証: 全セル連結（到達可能）、入り口→出口の経路存在
- `List<Vector2Int> FindDeadEnds()` — 壁3方向が閉じたセル

### ScoreCalculator
```
score = (t <= 60 ? 2 : t <= 120 ? 1 : 0) + (item ? 1 : 0)   // 最大3
stars = "★"×score + "☆"×(3-score)
```

## エディターツール（Assets/Editor/、ユーザー制約: EditorWindowベース）

- `MazeBuilderWindow`（EditorWindow）: パラメータUI（迷路サイズ・シード・色・玉物理・アイテム設定）＋「迷路生成」「再生成」ボタン
- 生成・変更は **Undo対応**（`Undo.RegisterCreatedObjectUndo` / `Undo.RecordObject`）→ Ctrl+Zで戻せる
- シーン変更は **`EditorSceneManager.MarkSceneDirty` + `EditorUtility.SetDirty`** で確実に保存対象化
- 目的: ユーザーが後からパラメータを調整して迷路を再現・差し替えできるようにする

## Unity側（MonoBehaviour）

### GameController（状態マシン・単一責任の指揮）
```
WaitingToStart --Space--> Playing   （玉spawn=入り口上から落下、タイマー180s開始）
Playing --玉が出口から落下--> Goal   （ScoreCalculator評価、★表示）
Playing --timer<=0--> TimeUp        （"TIME UP"表示）
Goal / TimeUp --Space--> Regenerate （壁破棄→迷路再生成→WaitingToStart）
```
- UI更新（残り時間mm:ss、メッセージ、★、アイテム取得表示）もここで

### BoardController（傾け）
- 矢印キー入力 → 目標回転（X/Z軸、最大15°）→ `Quaternion.Slerp`（係数5/s）
- `MazeRoot`（ボード全体の親）を回転。**カメラはMazeRootの子**（正対維持）

### MazeBuilder
- `Cell[,]`から壁を生成: 1セル=1ユニット、壁=BoxCollider付きCube、板=グリッド単位Cube。**出口セルのみ穴（Cubeなし）**。入り口セルは床あり（玉が入り口上に落下して着地し、迷路を転がる）
- `Build(MazeModel)` / `Clear()`（再生成用）
- アイテム: 行き止まり座標から**入り口・出口を除いて**乱数で1箇所選択し回転する小Cube+トリガー

### ItemPickup
- `OnTriggerEnter`（玉のタグ判定）→ 取得フラグをGameControllerへ、アイテム消滅

- 壁の高さデフォルト1.2（玉直径0.8より十分高く飛び越えを防ぐ）。傾け時のバウンドで壁越えしないよう、迷路全体に**透明な天井（BoxColliderのみ、下面=壁上端-0.3）**を配置。玉の跳ね返りは0.02に抑制

### 玉（Ball）
- **Sphere** + Rigidbody（mass 1, linearDamping 0.1, angularDamping 0.05）+ SphereCollider
- PhysicMaterial: 摩擦0.6 / バウンス0.1（調整余地）
- ゴール判定: 玉のY座標がボード面より一定下で、かつXZが出口セル範囲内

## 入力（Input System）
- Keyboard: 矢印4キー（ReadValue）、Space（performed）
- アクションマップはコード生成ではなく `InputAction` をスクリプトで定義（シーン资产を増やさない）

## シーン構成（GameScene）
```
GameScene
├── MazeRoot            … BoardControllerが回す
│   ├── Board(動的生成) / Walls(動的生成) / Item(動的生成) / Ball(動的生成)
│   └── Main Camera     … MazeRootの子（傾き同期、上から見下ろし）
├── Directional Light
└── UI Canvas           … Timer / Message / Stars / ItemIndicator
```

## UI仕様
- 上部中央: `mm:ss` カウントダウン（180→0）
- 待機中: `PRESS SPACE`
- Goal: `★★☆`（ScoreCalculator）+ `PRESS SPACE TO RESTART`
- TimeUp: `TIME UP` + 同上

## テスト計画（EditMode）
- MazeGenerator: 10×10生成・全セル到達・壁整合性（隣接セルで壁の対称性）・seed再現性
- FindDeadEnds: 既知パターンで正しい行き止まり集合
- ScoreCalculator: 境界（59s/60s/61s/119s/120s/121s × アイテム有無）・最大3・星文字列

## 実装ステップ計画（Step 1〜6）

実装は「ユーザーがエディター/プレイで操作して確認できる単位」に分割する。各Stepの内容は本書の各セクション（純粋ロジック／エディターツール／Unity側／UI仕様）を参照。完了済みStepの詳細な作業記録は `C:\Unity\templates\lessons-learned.md` の各Stepセクション参照。

| Step | 目的 | 主要内容 | 成果物 | 状態 |
|---|---|---|---|---|
| 1 | 迷路ロジックの確立 | MazeModel / MazeGenerator（再帰的バックトラッカー・seed注入）+ EditModeテスト（全セル到達・壁整合性・seed再現性） | `Assets/Scripts/Logic/` + `Assets/Tests/EditMode/` | 完了 |
| 2 | 迷路生成エディターツール | MazeBuilderWindow（パラメータUI・生成/再生成・Undo対応・シーンdirty化）、Cell[,]から壁・板をシーンへ生成 | `Assets/Editor/Step2_*` | 完了 |
| 3 | プレイ環境の構築 | 玉（Sphere+Rigidbody+PhysicMaterial）、BoardController（傾け・カメラはMazeRootの子）、物理調整（トンネリング・壁越え対策・透明な天井・入り口の天井穴） | `Assets/Editor/Step3_*` + GameScene | 完了 |
| 4 | ゲームループの実装 | GameController状態マシン（WaitingToStart/Playing/Goal/TimeUp）: Space開始（玉spawn・タイマー180s開始）、ゴール判定（出口からの落下）、タイムアップ、Spaceで迷路再生成。UI（Timer / Message）更新 | `Assets/Scripts/GameController.cs` + UI Canvas | 完了 |
| 5 | アイテムと評価画面 | 行き止まりから入り口・出口を除き乱数で1箇所へアイテム配置、ItemPickup（トリガー取得）、ScoreCalculator評価の★表示（最大★★★）+ ItemIndicator | `Assets/Scripts/ItemPickup.cs`、Stars表示、ScoreCalculator接続 | 未着手 |
| 6 | 調整と仕上げ | 通し動作確認（`editor_play` + `capture_game_view`）、物理パラメータ・UI・難易度調整、EditModeテスト全緑・`unity build` 検証 | 調整済みGameScene、全テスト緑、ビルド成功 | 未着手 |

### 各Stepの依存関係

- Step 4 は Step 1〜3 の成果物（迷路生成・シーン・玉）を利用する。Step 5 は Step 4 の状態マシン（Goal遷移での評価呼び出し）に依存する
- Step 4 のアイテム未配置状態でもゲームループ自体は完結するよう、評価はアイテムなし（+0点）で動作する前提でStep 4を実装する

### Step 4 詳細設計（ゲームループ）

前提: Step 1〜3 完了済み（迷路生成ロジック・エディター生成物・玉/傾け環境）。アイテムは Step 5 で追加し、評価は `hasItem=false` で動作する前提。

#### ファイル構成（Step 4 成果物）

```
Assets/Scripts/
├── Logic/GameLoop.cs          … 純粋C#の状態マシン+タイマー（EditModeテスト対象）
├── GameController.cs          … 状態マシンの指揮・UI更新・ゴール判定・再生成
└── MazeBuilder.cs             … ランタイム用の迷路生成（Step2_SceneMazeBuilderの実行時版）
Assets/Editor/
├── Step4_PlayParameters.cs    … Step4の調整パラメータ
├── Step4_GameLoopSetup.cs     … UI Canvas・GameController構築（Undo対応）
└── Step4_GameLoopSetupWindow.cs … EditorWindow
Assets/Tests/EditMode/GameLoopTests.cs
```

Step2_SceneMazeBuilder は `Undo`/`EditorSceneManager` 依存でエディター専用のため、ランタイム再生成は MazeBuilder（`Assets/Scripts/`）で行う。**ジオメトリ（床/壁/天井/マーカーの配置・寸法）は Step2_SceneMazeBuilder と完全一致させる**（出口穴・入り口天井穴・ゴール判定Y座標がジオメトリに依存するため。Step2側のパラメータ変更時は両方へ反映すること）。

#### 純粋ロジック: GameLoop（Logic/、名前空間 BallRolling.Gameplay.Logic）

```csharp
public enum GamePhase { WaitingToStart, Playing, Goal, TimeUp }

public class GameLoop
{
    public GamePhase Phase { get; private set; }
    public float RemainingSeconds { get; private set; }   // Playing中のみ減算
    public float ElapsedSeconds { get; }                  // TimeLimit - RemainingSeconds
    public bool CanRestart { get; }                       // Goal/TimeUp のみ true

    public void Configure(float timeLimitSeconds);        // デフォルト180
    public void StartGame();      // WaitingToStart→Playing。他フェーズでは無視
    public void Tick(float deltaTime);  // Playing中のみ残り時間を減算、0でTimeUpへ（以後のTickは不変）
    public void NotifyGoal();     // Playing→Goal。他フェーズでは無視
    public void Restart();        // Goal/TimeUp→WaitingToStart、RemainingSecondsを全回復
    public static string FormatTime(float seconds);       // 切り下げ "mm:ss"（180→"03:00"、0→"00:00"）
}
```

不正遷移は例外を投げず無視（Updateループでの堅牢性優先）。MonoBehaviourからはこのクラスのみを使用し、GameController自身は状態を持たない。

#### GameController（MonoBehaviour）

シーン直下に空の `GameController` GameObjectとして配置（MazeRootとは分離。MazeRootは再生成で子のみ入れ替えるため Rigidbody/BoardController/カメラは温存される）。

**Serializedフィールド**（Step4_Windowから配線）:

| フィールド | 型 | デフォルト | 備考 |
|---|---|---|---|
| `_mazeRoot` | Transform | — | MazeRoot |
| `_ball` | GameObject | — | MazeRoot/Ball（Step3生成物） |
| `_timerText` / `_messageText` | UnityEngine.UI.Text | — | UI Canvas配下 |
| `_timeLimitSeconds` | float | 180 | 要件#7 |
| `_ballSpawnHeight` | float | 3 | Step3のBallSpawnHeightと同値 |
| `_goalFallLocalY` | float | -1.5 | ゴール判定閾値（後述） |
| `_mazeWidth`/`_mazeHeight` | int | 10/10 | 要件確定値 |
| `_cellSize`/`_wallHeight`/`_wallThickness`/`_floorThickness` | float | 1/1.2/0.2/0.5 | Step2_MazeBuildParametersのデフォルトと同値 |
| `_wallMaterial`/`_floorMaterial`/`_entranceMarkerMaterial`/`_exitMarkerMaterial` | Material | Assets/Art/Materials既存 | 未設定なら`Shader.Find("Universal Render Pipeline/Lit")`で実行時生成 |

**フィールド（private）**: `_loop`（GameLoop）、`_builder`（MazeBuilder）。

**ライフサイクル**:
- `Awake`: `_loop = new GameLoop()`。`_ball.SetActive(false)`（WaitingToStartでは玉を隠す＝要件「開始時に上から落下して登場」に合致）
- `Update`:
  1. Space（`Keyboard.current.spaceKey.wasPressedThisFrame`）押下時:
     - WaitingToStart → `SpawnBall()` + `_loop.StartGame()`（メッセージ消去）
     - Goal / TimeUp（`_loop.CanRestart`）→ `RegenerateMaze()` + `_loop.Restart()`
  2. Playing中: `_loop.Tick(Time.deltaTime)` → `_timerText.text = FormatTime(RemainingSeconds)`、`CheckGoal()`
- UI表示は状態遷移の直後のみ書き換え（毎フレームの文字列生成を回避）

**玉spawn（SpawnBall）**: `_ball.SetActive(true)` → `localPosition = ((Entrance.x+0.5)*cellSize, _ballSpawnHeight, (Entrance.y+0.5)*cellSize)` → `Rigidbody.linearVelocity/angularVelocity = 0` + `WakeUp()`。入り口(0,0)の天井穴（Step2で既に開放済み）から落下する。

**ゴール判定（CheckGoal）**: Playing中のみ、玉の**MazeRootローカル座標**で判定（MazeRootは最大15°傾くためワールド座標は不使用）:
- `localPosition.y < _goalFallLocalY(-1.5)` かつ local X/Z が出口セル(9,9)の範囲内 `[9*cell, 10*cell]`
- 成立時: `_loop.NotifyGoal()` → `ScoreCalculator.Calculate(ElapsedSeconds, hasItem: false)` で★組み立て → `_messageText` に `RenderStars(score) + "\nPRESS SPACE TO RESTART"` → `_ball.SetActive(false)`
- 閾値の根拠: 床下面=-0.5、出口マーカー=-1.2（コライダーなし）。-1.5 は両方を確実に下回り、かつ転がり中の誤検知なし。出口穴が唯一の落下経路（入り口は床あり・天井で封じられている）ため XZ 条件は保険

**迷路再生成（RegenerateMaze）**: `_builder.Clear(_mazeRoot)`（Floor/Walls/Ceiling/Markersの子のみ破棄。カメラ・玉はMazeRoot直下なので影響なし）→ `MazeGenerator.Generate(width, height, UnityEngine.Random.Range(int.MinValue, int.MaxValue))`（要件#3: リスタート毎にランダム）→ `_builder.Build(...)` → BoardControllerの入力再有効化・回転リセット。

**UI更新**（UI仕様セクション準拠）:
- WaitingToStart: タイマー `03:00` / メッセージ `PRESS SPACE`
- Playing: タイマーのみ（メッセージ空）
- Goal: ★文字列 + `PRESS SPACE TO RESTART`（Stars/ItemIndicatorはStep 5で別テキスト追加）
- TimeUp: `TIME UP` + `PRESS SPACE TO RESTART`

#### BoardController への追加（Step 4 で実装）

既存 `BoardController.cs` に2つのpublicメソッドを追加（GameControllerからの指示用）:
- `public void SetInputEnabled(bool isEnabled)` — WaitingToStart/Goal/TimeUp中は矢印キー入力を無視（ボードが動き続けるとメッセージが見難いため）
- `public void ResetRotation()` — リスタート時に `_rigidbody.MoveRotation(_initialRotation)` へ戻す（`FixedUpdate`のSlerp目標も初期姿勢に戻る）

物理設定（`Physics.gravity`・`Time.fixedDeltaTime`）はStep3がプロジェクト設定へ永続化済みのため、ランタイムでの再設定は不要。

#### MazeBuilder（ランタイム、Assets/Scripts/）

- `Clear(Transform root)`: root直下の `Floor`/`Walls`/`Ceiling`/`Markers` を`Destroy`（子オブジェクトは名前で特定）
- `Build(MazeModel maze, 迷路設定, Transform root, Material群)`: Step2_SceneMazeBuilderと同じ配置・寸法でFloor/Walls/Ceiling/Markersを生成。Undo/EditorSceneManager/AssetDatabase 系のコードのみ持たない
- マテリアルはSerializeField参照を優先し、実行時生成はフォールバック（`AssetDatabase`は実行時に使えないため）

#### エディターツール（Step4_、Assets/Editor/）

**Step4_PlayParameters**（Serializable）: `TimeLimitSeconds=180`、`BallSpawnHeight=3`、`GoalFallLocalY=-1.5`、`TimerFontSize=60`、`MessageFontSize=48`、`TimerColor`、`MessageColor`。

**Step4_GameLoopSetup.Apply(p)**（static）:
1. `MazeRoot`・`Ball`（`Step3_PlaySetup.BallName`）の存在チェック → 不足時はエラーLog＋null返し（玉の自動生成はしない）
2. UI Canvas生成: `Screen Space - Overlay` + `CanvasScaler`（Scale With Screen Size, 1920×1080）+ `TimerText`（上部中央アンカー）/`MessageText`（中央アンカー）。フォントは `Resources.GetBuiltinResource&lt;Font&gt;("LegacyRuntime.ttf")`（Unity 6では"Arial.ttf"は使えない）
3. `GameController` GameObject生成+コンポーネント追加し、Serialized参照とパラメータを配線
4. `Undo.RegisterCreatedObjectUndo`（Canvas・GameController）＋配線時 `Undo.RecordObject`＋`EditorUtility.SetDirty`＋`EditorSceneManager.MarkSceneDirty`

**Step4_GameLoopSetupWindow**（EditorWindow、MenuItem "BallRolling/Step4 Game Loop"）: パラメータUI＋「ゲームループを構築」ボタン（Apply実行）＋「プレイモードを開始」ボタン＋手順HelpBox。Step2/3のウィンドウと同形式。

#### 設計判断の確定事項（メイン判断）

- BoardControllerへの `SetInputEnabled` / `ResetRotation` 追加は承認（Step 4 の必要最小限の変更）
- リスタート時のシードは**毎回ランダム**（要件#3）。デバッグ用固定シードオプションは追加しない
- WaitingToStart中の玉は非表示（SetActive(false)）。ゴール判定後も即非表示とする（落下演出なし）
- Step3未実行のシーン（玉不在）でStep4構築した場合はエラーログのみで、玉の自動生成はしない

#### テスト計画（EditMode: GameLoopTests）

- 初期状態 WaitingToStart・`StartGame` で Playing・Remaining=時間上限
- `Tick` 減算・0到達で TimeUp（以後`Tick`してもRemaining=0のまま）
- 不正遷移の無視: Playing中の`StartGame`、WaitingToStart中の`NotifyGoal`
- `NotifyGoal` で Goal・`ElapsedSeconds` が経過時間を保持
- `Restart` で WaitingToStart・Remaining 全回復・`CanRestart` がfalseへ
- `FormatTime`: 180→"03:00"、61.9→"01:01"、0→"00:00"
- ScoreCalculator統合: hasItem=false で時間のみ評点（回帰確認）

#### プレイモードでの動作確認手順

1. Step2で迷路生成 → Step3で玉スポーン → Step4でゲームループ構築 → Ctrl+S でシーン保存
2. プレイモード開始: `03:00` と `PRESS SPACE` が表示され、玉は非表示
3. Space: 玉が入り口(緑マーカー)上空から落下し、タイマーのカウントダウン開始
4. 矢印キーで出口(赤マーカー・9,9)へ誘導 → 穴から落下すると★表示 + `PRESS SPACE TO RESTART`
5. Space: 迷路がランダム再生成（壁配置が変化）し、ボードの傾きが初期化、`PRESS SPACE` に戻る
6. 放置してタイマー0 → `TIME UP` + `PRESS SPACE TO RESTART`、Spaceで再生成

### 進捗の更新ルール

- Step完了時に「状態」列を更新し、設計との差異があれば該当セクションも合わせて修正する
- 完了Stepの問題と解決は `lessons-learned.md` の該当Stepセクションへ記録する（CLAUDE.md規定）
