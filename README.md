# BallRolling

Unity 6 で開発する玉転がしゲーム。

## 開発環境

| 項目 | 値 |
|---|---|
| エンジン | Unity 6000.6.0f1 |
| レンダーパイプライン | URP (Universal Render Pipeline) |
| ビルドターゲット | StandaloneWindows64 |
| テスト | Unity Test Framework 1.8.0 |

## セットアップ

```powershell
# Unity CLI でプロジェクトを開く
unity open .

# エディターが未インストールの場合
unity install 6000.6.0f1
```

## テスト

Unity Test Framework を使用します（EditMode / PlayMode 両対応）。

```powershell
# すべてのテストを実行
unity test .

# EditMode のみ実行
unity test . --test-mode EditMode
```

Claude Code を使用している場合は `/test` カスタムコマンド（`.claude/commands/test.md`）も利用できます。

## ディレクトリ構造

```
BallRolling/
├── Assets/            # ゲームアセット・スクリプト
├── Packages/          # パッケージマニフェスト
├── ProjectSettings/   # プロジェクト設定
├── .claude/           # Claude Code 設定（テスト自動化コマンドなど）
│   ├── commands/      # カスタムスラッシュコマンド
│   └── settings.json  # 権限などの設定
├── CLAUDE.md          # Claude Code 向けプロジェクトガイド
├── .gitignore         # バージョン管理用の除外設定
└── README.md          # このファイル
```
