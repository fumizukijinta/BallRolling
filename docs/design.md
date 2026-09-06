# 詳細設計 / プログラム設計 — BallRolling

要件: [requirements.md](requirements.md) 参照。実装変更時は本書も更新する。

## アーキテクチャ

純粋C#ロジック（EditModeテスト対象）とUnity固有（MonoBehaviour）を分離し、`Assets/Scripts/`配下に配置（名前空間 `BallRolling.Gameplay` / `BallRolling.Gameplay.Logic`）。

```
Assets/Scripts/
├── Logic/          … 純粋C#（テスト可能）
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
| 4 | ゲームループの実装 | GameController状態マシン（WaitingToStart/Playing/Goal/TimeUp）: Space開始（玉spawn・タイマー180s開始）、ゴール判定（出口からの落下）、タイムアップ、Spaceで迷路再生成。UI（Timer / Message）更新 | `Assets/Scripts/GameController.cs` + UI Canvas | 未着手 |
| 5 | アイテムと評価画面 | 行き止まりから入り口・出口を除き乱数で1箇所へアイテム配置、ItemPickup（トリガー取得）、ScoreCalculator評価の★表示（最大★★★）+ ItemIndicator | `Assets/Scripts/ItemPickup.cs`、Stars表示、ScoreCalculator接続 | 未着手 |
| 6 | 調整と仕上げ | 通し動作確認（`editor_play` + `capture_game_view`）、物理パラメータ・UI・難易度調整、EditModeテスト全緑・`unity build` 検証 | 調整済みGameScene、全テスト緑、ビルド成功 | 未着手 |

### 各Stepの依存関係

- Step 4 は Step 1〜3 の成果物（迷路生成・シーン・玉）を利用する。Step 5 は Step 4 の状態マシン（Goal遷移での評価呼び出し）に依存する
- Step 4 のアイテム未配置状態でもゲームループ自体は完結するよう、評価はアイテムなし（+0点）で動作する前提でStep 4を実装する

### 進捗の更新ルール

- Step完了時に「状態」列を更新し、設計との差異があれば該当セクションも合わせて修正する
- 完了Stepの問題と解決は `lessons-learned.md` の該当Stepセクションへ記録する（CLAUDE.md規定）
