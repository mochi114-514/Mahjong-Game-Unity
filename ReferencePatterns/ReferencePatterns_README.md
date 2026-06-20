# ReferencePatterns README

## このフォルダの目的

このフォルダには、過去プロジェクトで実際に使っていた参考コードを置く。

目的は、コードをそのままコピーすることではない。

目的は、Codexに対して以下のような「作り方の癖」や「責務分離の考え方」を伝えることである。

- 通知役、指示役、機能役の分け方
- Notifierが通知に徹する構造
- Managerが全体の流れや状態を統括する構造
- 下位ManagerやServiceに具体処理を任せる構造
- Audio系のように、通知を受けて別システムへ依頼する中継構造
- 1つのスクリプトに何でも詰め込まない設計方針

---

## 参考コードの置き方

過去プロジェクトの `.cs` ファイルは、そのままUnityの `Assets/` 配下に置かない。

理由は、UnityがC#スクリプトとしてコンパイルし、現在のプロジェクトに存在しない型や参照によってエラーになる可能性があるため。

参考コードは、以下のように `.cs.txt` 形式で置く。

```text
Docs/
  ReferencePatterns/
    README.md
    GameManager.cs.txt
    WaveManager.cs.txt
    WaveAudioNotifier.cs.txt
    GameAudioManager.cs.txt
    GameFlowBgmNotifier.cs.txt
```

このフォルダは参考資料置き場であり、実装コード置き場ではない。

---

## Codexへの扱わせ方

Codexには、このフォルダのコードを「参考資料」として扱わせる。

Codexに指示する時は、以下の方針を必ず伝える。

```text
Docs/ReferencePatterns/ にあるコードは参考資料です。
そのままコピーしないでください。
参考にするのは、責務分離、通知の流れ、ManagerとNotifierの関係、命名感だけです。
今回の特殊麻雀プロトタイプに必要な最小構成へ置き換えてください。
```

---

## 基本の責務分離

このプロジェクトでは、重要なゲーム進行処理において、以下の責務分離を基本方針とする。

```text
通知役 → 指示役 → 機能役
```

ただし、すべての小さなクラスに無理に適用しない。

主に以下のような重要処理で意識する。

- ターン進行
- ツモ
- 打牌
- 必殺技発動
- 必殺技効果解決
- 局開始
- 局終了
- UI通知
- Audio通知
- ログ通知

---

## 通知役

通知役は、すでに起きた事実を知らせるだけの役割を持つ。

通知役は、次に何をするかを決めない。

### 例

- TileDrawnNotifier
- TileDiscardedNotifier
- SkillActivatedNotifier
- TurnStartedNotifier
- RoundEndedNotifier
- Audio系Notifier

### やってよいこと

- イベントを購読する
- イベントを発火する
- 必要最小限の情報をイベント引数として渡す
- 状態やイベントを別の形式へ変換して通知する
- AudioKeyや表示用データなどに変換して、指示役へ渡す

### やってはいけないこと

- ターンを進める
- 局終了を決める
- 勝敗を決める
- ツモ結果を決める
- 必殺技効果を直接解決する
- ゲーム進行を勝手に決定する

通知役は「決定済みの出来事を知らせる係」として扱う。

---

## 指示役

指示役は、現在の状態や通知を受けて、次に何をするかを決める役割を持つ。

### 例

- MahjongGameFlow
- SkillSystem
- TurnController
- AudioManager
- UIController
- 特定機能の最上位Manager

### やってよいこと

- 現在の状態を見る
- 次に何をするか判断する
- 機能役へ処理を依頼する
- 状態遷移を管理する
- 必要な通知を発火する
- 複数の機能役をまとめて呼ぶ

### 避けるべきこと

- 低レベル処理をすべて自分で抱える
- UI、Audio、ルール、データ処理を1つの巨大スクリプトに詰め込む
- Notifierの責務まで持つ
- Serviceや機能役の処理を複製する

指示役は「何をするか決める係」として扱う。

---

## 機能役

機能役は、指示役から依頼された具体的な処理を実行する役割を持つ。

### 例

- DrawService
- DiscardService
- TurnOrderService
- WinChecker
- ActionResolver
- DevLog
- MahjongLogFormatter
- BGM再生担当
- SE再生担当
- View更新担当

### やってよいこと

- 山から牌を引く
- 手牌から牌を捨てる
- 次のSeatIdを返す
- 和了判定を行う
- ログを書く
- BGMやSEを再生する
- UI表示を更新する

### 避けるべきこと

- ゲーム全体の進行を勝手に決める
- 自分の責務外の状態を持つ
- 他の無関係な機能役を大量に直接呼ぶ
- 指示役を飛び越えて全体を動かす

機能役は「実際の処理をする係」として扱う。

---

## 参考例：Audio系の流れ

音を鳴らしたい場合、ゲーム進行側のManagerが直接AudioClipやAudioSourceを持たない。

悪い例：

```text
WaveManagerがAudioClipを持つ
WaveManagerが直接AudioSourceを鳴らす
GameManagerがBGMやSEの詳細を知っている
```

良い例：

```text
1. Wave開始などのイベントが発生する
2. Audio系Notifierがその通知を受け取る
3. NotifierがAudioKeyなどに変換する
4. GameAudioManagerのような指示役へ依頼する
5. BGM再生担当やSE再生担当が実際に音を鳴らす
```

この考え方を、特殊麻雀プロトタイプの以下にも応用する。

- ツモ
- 打牌
- 必殺技
- ログ
- UI更新
- Audio演出

---

## 特殊麻雀プロトタイプでの対応例

### ツモ

```text
指示役：MahjongGameFlow
機能役：DrawService
通知役：TileDrawnNotifier / DevLog
```

ルール：

- 牌を引く処理は必ずDrawService経由にする
- Wallから各クラスが直接Drawしない
- DrawServiceはActiveSkillEffectを参照して、通常ツモかスキル補正ツモかを解決する
- ツモ結果はログに残す

---

### 打牌

```text
指示役：MahjongGameFlow
機能役：DiscardService / Hand / DiscardPile
通知役：TileDiscardedNotifier / DevLog
```

ルール：

- 打牌したSeatIdをDiscardRecordに必ず記録する
- 自分の捨て牌に自分が反応する候補は出さない
- 打牌後の進行判断はMahjongGameFlowが行う
- Notifierは打牌済みの事実を通知するだけにする

---

### ターン進行

```text
指示役：MahjongGameFlow
機能役：TurnOrderService
通知役：TurnStartedNotifier / DevLog
```

ルール：

- TurnOrderServiceはactiveSeatsを順番に回すだけにする
- TurnOrderServiceは勝敗、鳴き、必殺技、UIを判断しない
- activeSeatsがEastのみなら、次のターンもEastにする

---

### 必殺技

```text
指示役：SkillSystem
機能役：ActiveSkillEffect / DrawService / ActionResolver
通知役：SkillActivatedNotifier / SkillEffectResolvedNotifier / DevLog
```

ルール：

- 必殺技は自分のターン中だけ発動できる
- 必殺技の発動と効果解決を分ける
- 発動済み効果はActiveSkillEffectとして残す
- 他家の打牌時に新しく必殺技を発動するUIは作らない
- 効果解決はDrawServiceやActionResolverなど、適切な機能役が行う

---

### ログ

```text
指示役：MahjongGameFlow / SkillSystem
機能役：DevLog / MahjongLogFormatter
通知役：各イベント通知
```

ルール：

- ログは観測専用とする
- ログ処理がゲーム進行を決定してはいけない
- Unity ConsoleのError / Warning / Exceptionを保存する
- ツモ、打牌、必殺技発動、効果解決、ターン開始はログに残す

---

## Managerの扱い

### GameManager

GameManagerは、ゲーム全体の最上位指示役として扱う。

ただし、今回の特殊麻雀プロトタイプでは、最初から巨大なGameManagerを作らない。

最初は、必要最小限の `MahjongGameFlow` をゲーム進行の中心にする。

### 下位Manager

下位Managerは、GameManagerまたは機能の最上位指示役から状態や指示を受けて動く。

下位Managerが勝手にゲーム全体の状態を変更しない。

### Notifier

NotifierはManagerではない。

Notifierは、決定済みの事実を通知するだけのスクリプトとして扱う。

Notifierがゲーム進行を決めてはいけない。

---

## 適用しなくてよいもの

以下には、無理に「通知役 → 指示役 → 機能役」を適用しなくてよい。

- Tile
- SeatId
- DiscardRecord
- 単純なenum
- 単純なデータクラス
- 小さなView補助
- 一時的なプロトタイプ用UI

ただし、プロトタイプ用の一時実装には `// PROTOTYPE` コメントを付ける。

---

## Codexに守ってほしいこと

Codexは、このフォルダのコードをそのままコピーしない。

参考にするのは以下のみ。

- 責務の分け方
- 通知役、指示役、機能役の関係
- Notifierが通知に徹する考え方
- Managerが全処理を抱え込まない考え方
- 機能役に具体処理を任せる考え方
- Inspectorで確認しやすい[SerializeField]の使い方
- 必要な箇所だけイベントで疎結合にする考え方

短期間プロトタイプなので、過剰な抽象化は避ける。

ただし、後で壊れやすい以下の境界は守る。

- ツモはDrawService経由
- 順番はTurnOrderService経由
- 必殺技発動はSkillSystem
- 発動済み効果はActiveSkillEffect
- Notifierは通知のみ
- ログは観測のみ

---

## Codexへの依頼例

```text
Docs/ReferencePatterns/README.md を読んでください。

このフォルダの参考コードはコピーしないでください。
責務分離、通知役・指示役・機能役の関係、Notifierが通知に徹する構造だけを参考にしてください。

今回の特殊麻雀プロトタイプでは、MahjongGameFlowを最小の指示役として、DrawService、TurnOrderService、SkillSystem、DevLogなどに処理を分けてください。

まずは最小構成で、ツモ、打牌、ターン進行、必殺技発動、スキル効果解決、ログ出力の流れを作ってください。
```
