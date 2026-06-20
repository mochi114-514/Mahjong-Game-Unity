# AGENTS_GAME_RULES.md

## 目的

このファイルは、Codexに常に守らせたい固定ルールをまとめたものです。

このプロジェクトは、Unityで作る特殊麻雀プロトタイプです。
目的は、正式な麻雀ゲームを完成させることではなく、短期間で「牌を引く・捨てる・必殺技でツモや行動が変わる」ゲームの核を動かすことです。

設計方針の詳細は `Docs/ReferencePatterns/README.md` を参照します。
このファイルでは、ゲーム制作時に必ず守るルールだけを定義します。

---

## Unity環境

- Unity Version: Unity 6.3 LTS / 6000.3.x
- Template: Universal 2D
- Project Type: 2D
- Render Pipeline: URP / Universal Render Pipeline
- Target Platform: Windows first
- WebGL may be considered later
- Initial Scene: MahjongPrototype
- UI: Unity Canvas + TextMeshPro
- Tile visuals may be text-based at first
- Input should start with UI Buttons and click events
- Do not assume Built-in Render Pipeline
- Do not add unnecessary packages unless required

---

## 開発優先順位

優先順位は以下の通りです。

1. Unity上で動くこと
2. 牌を引けること
3. 牌を捨てられること
4. 自分のターンが回ること
5. 必殺技を自分のターン中に発動できること
6. 必殺技によって次のツモが変わること
7. ログで挙動を確認できること
8. 後から拡張できる最低限の境界を残すこと

美しいUI、正式な麻雀ルール、演出、効果音、バランス調整は後回しです。

---

## 最初は作らないもの

以下は、このプロトタイプでは存在しないルールとして扱います。

- 通常のチー
- ポン
- カン
- リーチ
- フリテン
- ドラ
- 裏ドラ
- 符計算
- 正式な点数計算
- 本場
- 供託
- 流局時テンパイ料
- チョンボ
- ダブルロン
- 高度なCPU思考
- オンライン対戦
- 正式な4人対局

これらを勝手に実装しないでください。

---

## 最初のプレイ人数

最初は1人用です。

- 操作プレイヤーは East
- activeSeats は `[East]`
- activeSeats が1人だけの場合、次のターンも East に戻る

ただし、将来4人制へ拡張できるように、SeatId と activeSeats の考え方は最初から持たせます。

---

## SeatIdルール

SeatIdは以下を定義します。

- East
- South
- West
- North

Self / Opponent のような固定タグで管理しないでください。

自家・他家の判定は、viewerSeat と actorSeat の比較で行います。

---

## ターンルール

ターン順は activeSeats を基準にします。

最初は activeSeats が `[East]` なので、East のターンが繰り返されます。

ターン進行で守ること：

- TurnOrderService は activeSeats から次の SeatId を返すだけにする
- TurnOrderService は勝敗、スキル、UI、鳴き、点数を判断しない
- MahjongGameFlow が現在の進行を管理する
- Notifier がターン進行を決めない

---

## ツモルール

牌を引く処理は必ず DrawService 経由にします。

禁止例：

```csharp
wall.Draw();
```

推奨例：

```csharp
drawService.DrawTile(currentSeat, gameState);
```

ツモ処理で守ること：

- Wallから各クラスが直接牌を引かない
- DrawService が通常ツモとスキル補正ツモを解決する
- 必殺技によってツモが変わる場合も、必ず山に残っている牌から引く
- 存在しない牌を生成しない
- ツモ結果はログに残す

---

## 打牌ルール

捨て牌には必ず誰が捨てたかを記録します。

DiscardRecord には最低限以下を含めます。

- actorSeat
- tile
- turnIndex

打牌で守ること：

- 自分の捨て牌に自分が反応する候補を出さない
- 打牌後の進行判断は MahjongGameFlow が行う
- 打牌結果はログに残す
- 将来の4人制に備え、actorSeat を必ず残す

---

## 必殺技ルール

必殺技は、自分のターン中だけ発動できます。

必殺技は割り込み型ではありません。

以下のタイミングで新しく必殺技を発動するUIや処理は作らないでください。

- 他家の打牌時
- 鳴き候補発生時
- ロン候補発生時
- 相手行動中

必殺技で守ること：

- 発動と効果解決を分ける
- 発動済み効果は ActiveSkillEffect として保持する
- SkillSystem が発動可否と登録を担当する
- DrawService や ActionResolver などが、必要なタイミングで ActiveSkillEffect を参照して効果を解決する
- 効果解決後、不要になった ActiveSkillEffect は消費・削除する
- 必殺技発動と効果解決はログに残す

---

## 最初に実装する必殺技

最初の必殺技は「指定牌ツモ」です。

効果：

- 自分のターン中に発動する
- targetTile を指定する
- 次のツモ時、山に targetTile が残っていればその牌を引く
- 山に targetTile が残っていなければ通常ツモする
- ツモ後、この ActiveSkillEffect は消える

注意：

- 山に存在しない牌を生成しない
- 必ず山に残っている牌から引く
- 成功・失敗をログに残す

---

## ログルール

最初から薄いログ基盤を作ります。

目的は、CodexやChatGPTにバグ報告を渡しやすくすることです。

ログは観測専用です。
ログ処理がゲーム進行を決定してはいけません。

最低限、以下をログに残します。

- RunStarted
- RoundStarted
- TurnStarted
- TileDrawn
- TileDiscarded
- SkillActivated
- SkillEffectRegistered
- SkillEffectResolved
- SkillEffectExpired
- WinChecked
- RoundEnded
- SlowFrame
- Unity Console Warning
- Unity Console Error
- Unity Console Exception

推奨形式：

- jsonl
- 1行1イベント
- 保存先は `Application.persistentDataPath/DevLogs/`
- Unity Editor または Development Build で有効
- Release Build では無効化または軽量化できるようにする

---

## ログに含めたい情報

可能な範囲で、以下を含めます。

- time
- frame
- scene
- category
- eventName
- message
- seat
- tile
- hand
- wallCount
- turnIndex
- activeSkill
- stackTrace

すべてのイベントに全項目を入れる必要はありません。
関係ある情報だけを入れてください。

---

## 仮実装ルール

短期間プロトタイプなので、仮実装は許可します。

ただし、仮実装には必ず以下のコメントを付けます。

```csharp
// PROTOTYPE
```

仮実装にしてよい例：

- テキスト牌表示
- 仮のtargetTile
- 簡易和了判定
- 簡易UI
- 一時的なボタン操作

仮実装にしてはいけない例：

- DrawServiceを通さず直接Wallから引く
- Notifierが進行を決める
- SkillSystemを通さずスキル状態を直接いじる
- actorSeatを記録しない打牌

---

## 参考設計資料

設計方針や責務分離の詳細は以下を参照します。

```text
Docs/ReferencePatterns/README.md
```

ただし、参考コードをそのままコピーしてはいけません。

参考にするのは以下だけです。

- 責務分離
- 通知役、指示役、機能役の関係
- Notifierが通知に徹する考え方
- Managerが全処理を抱え込まない考え方
- 機能役に具体処理を任せる考え方

---

## Codex作業後の報告

Codexは作業後に以下を報告してください。

- 追加ファイル
- 変更ファイル
- 各スクリプトの役割
- Unity上での確認手順
- Inspector設定
- `// PROTOTYPE` として残した箇所
- 既知のリスク
