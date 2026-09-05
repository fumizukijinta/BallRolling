# CLAUDE.md

このファイルは Claude Code にこのプロジェクトでの作業方法を指示します。

## プロジェクト概要

- Unity 6（6000.6.0f1）+ Universal 3D (URP) テンプレートで作成した玉転がしゲーム
- ビルドターゲット: StandaloneWindows64
- バージョン管理: 未導入（git 導入時にルートの `.gitignore` を使用）

## よく使うコマンド

Unity CLI（`unity` コマンド）を使用します。

```powershell
unity open .                # プロジェクトを Unity エディターで開く
unity status                # 接続中のエディター状態を確認
unity test .                # EditMode / PlayMode テストを実行
unity projects verify .     # ビルドを壊す整合性問題をチェック
unity build .               # プロジェクトをビルド
```

## コーディング規約

- ゲームplay用スクリプトは `Assets/Scripts/` に配置する
- テストコードは `Assets/Tests/EditMode/`・`Assets/Tests/PlayMode/` に配置する
- エディター拡張は `Assets/Editor/` に配置する

## バージョン管理の運用

- 画像・動画・音声（とその .meta）は容量対策で**コミットしない**（.gitignore で除外済み）
- 3Dモデル（fbx等）・フォント・ライブラリ（dll等）は Git LFS で管理
- メディアファイルはgit外で別途バックアップが必要（他マシンでクローンした場合は手動配置）
