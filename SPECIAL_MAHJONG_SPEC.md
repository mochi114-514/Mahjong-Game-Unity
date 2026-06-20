# SPECIAL_MAHJONG_SPEC.md

## ゲーム概要

このゲームは、通常の麻雀ではなく、必殺技で麻雀の挙動を一時的に変える特殊麻雀プロトタイプです。

正式なリーチ麻雀を完全再現するゲームではありません。

最初の目的は、以下の核を短期間で確認することです。

- 牌を引く
- 牌を捨てる
- 自分のターンが回る
- 必殺技を発動する
- 必殺技によって次のツモが変わる
- 特殊麻雀として面白そうか判断できる

---

## 開発目的

今回の目的は、完成版を作ることではありません。

目的は、Codexを使って短期間で動くプロトタイプを作り、今後の開発速度を上げることです。

そのため、最初は完成度よりも以下を優先します。

1. 動くこと
2. 触れること
3. 特殊ルールの面白さが分かること
4. バグ発生時にログで説明できること
5. 後から作り直せる程度に境界が残っていること

---

## 初期プロトタイプの完成イメージ

最初のプロトタイプでは、以下の流れができればよいです。

1. MahjongPrototypeシーンを開始する
2. 山が生成される
3. Eastに配牌される
4. Eastのターンが始まる
5. Eastがツモする
6. 手牌から牌を選んで捨てる
7. 捨て牌が表示される
8. 次のターンもEastに戻る
9. 必殺技ボタンを押す
10. ActiveSkillEffectが登録される
11. 次のツモでDrawServiceがActiveSkillEffectを参照する
12. 指定牌が山にあれば、その牌を引く
13. 効果が消費される
14. ツモ、打牌、必殺技発動、効果解決がログに残る
15. リトライできる

---

## 最初に存在するもの

最初のプロトタイプには、最低限以下を含めます。

- SeatId
- Tile
- Wall
- Hand
- DiscardRecord
- PlayerSeat
- MahjongGameFlow
- TurnOrderService
- DrawService
- SkillSystem
- ActiveSkillEffect
- DevLog
- MahjongLogFormatter
- 手牌表示
- 捨て牌表示
- ツモボタン
- 打牌操作
- 必殺技ボタン
- リトライ

---

## 最初に存在しないもの

以下は最初のプロトタイプでは存在しません。

- 通常のチー
- ポン
- カン
- リーチ
- フリテン
- ドラ
- 裏ドラ
- 符計算
- 正式点数計算
- 本場
- 供託
- 流局時テンパイ料
- チョンボ
- ダブルロン
- 高度なCPU思考
- オンライン対戦
- 正式な4人対局
- 美しいUI
- 本格演出
- 本格サウンド

---

## 牌

最初の牌表示はテキストでよいです。

例：

```text
1m
2m
3m
5p
East
Red
```

牌画像は後回しです。

最初は、見た目よりも処理が正しく動くことを優先します。

---

## 山

Wallは山を保持します。

山で守ること：

- 初期化時に牌を生成する
- シャッフルできる
- 残り枚数を取得できる
- 指定牌が残っているか確認できる
- 指定牌を山から取り除いて返せる
- 通常ツモ用に次の牌を返せる

ただし、Wallを外部から直接操作しすぎないようにします。
ツモの入口はDrawServiceに統一します。

---

## 手牌

Handはプレイヤーの手牌を保持します。

手牌で守ること：

- 牌を追加できる
- 牌を削除できる
- 現在の牌一覧を取得できる
- UI表示用に参照できる

正式な和了判定やシャンテン計算は最初は不要です。

---

## 捨て牌

捨て牌には、誰が捨てたかを必ず記録します。

DiscardRecord の最低項目：

- actorSeat
- tile
- turnIndex

これにより、将来4人制にした時も、自分の捨て牌か他家の捨て牌かを判定できます。

---

## ターン

最初は1人用です。

```text
activeSeats = [East]
```

したがって、Eastのターンが繰り返されます。

ただし、TurnOrderServiceは最初からactiveSeatsを使って次のSeatIdを返す作りにします。

---

## 必殺技

必殺技は、自分のターン中だけ発動できます。

必殺技は、発動した瞬間にすべての効果を実行するのではなく、ActiveSkillEffectとして残ります。

その後、DrawServiceなどの処理が必要なタイミングでActiveSkillEffectを参照して効果を解決します。

---

## 最初の必殺技：指定牌ツモ

### 概要

指定した牌を、次のツモで引こうとする必殺技です。

### 発動条件

- 自分のターン中である
- まだツモ前、またはプロトタイプ上発動可能と決めたタイミングである
- targetTileが設定されている

### 効果

- ActiveSkillEffectとして登録される
- 次のツモ時、DrawServiceがこの効果を参照する
- 山にtargetTileが残っていれば、その牌を引く
- 山にtargetTileが残っていなければ、通常ツモする
- 解決後、この効果は消える

### ログ

以下をログに残します。

- SkillActivated
- SkillEffectRegistered
- SkillEffectResolved
- DrawModifiedBySkill
- SkillEffectExpired

---

## 将来追加したい必殺技案

最初は実装しません。

ただし、後で追加しやすいように、ActiveSkillEffect方式を維持します。

案：

- 指定牌を引きやすくする
- 特定の種類の牌を引きやすくする
- シャンテン数が進む牌を引きやすくする
- 数巡だけ有効牌を引きやすくする
- 遠隔チー効果を自分のターンで仕込み、後の他家打牌時に候補を出す
- 和了時に点数を増やす

---

## 簡易和了判定

最初の和了判定は仮でよいです。

例：

- 和了ボタンを仮で置く
- 特定条件だけでWin扱いにする
- 通常形のごく簡易判定だけにする

正式な役判定、符計算、点数計算は不要です。

仮実装には `// PROTOTYPE` を付けます。

---

## UI

最初のUIは最低限でよいです。

必要なUI：

- 手牌表示
- 捨て牌表示
- ツモボタン
- 必殺技ボタン
- リトライボタン
- ログ確認用の簡易表示があれば便利

最初はTextMeshProでテキスト牌を表示します。

牌画像、演出、アニメーションは後回しです。

---

## ログ

ログは、CodexやChatGPTへバグ報告を渡すために使います。

例：

```text
[GameFlow] TurnStarted seat=East turnIndex=1
[Mahjong] TileDrawn seat=East tile=5m wallCount=69 source=Normal
[Mahjong] TileDiscarded seat=East tile=9p turnIndex=1
[Skill] SkillActivated seat=East skill=ForceDraw targetTile=5m
[Skill] SkillEffectRegistered effect=ForceDraw targetTile=5m duration=NextDraw
[Skill] DrawModifiedBySkill skill=ForceDraw targetTile=5m result=Applied
[Mahjong] TileDrawn seat=East tile=5m wallCount=68 source=SkillModified
[Skill] SkillEffectExpired effect=ForceDraw reason=ConsumedByDraw
```

---

## バグ報告で確認したいこと

ログによって、最低限以下を確認できるようにします。

- 誰のターンか
- 何をツモしたか
- 何を捨てたか
- 山の残り枚数
- 必殺技が発動したか
- ActiveSkillEffectが登録されたか
- 効果が解決されたか
- 効果が成功したか失敗したか
- 効果が消えたか
- エラーや例外が出たか

---

## 最初の成功条件

最初の成功条件は以下です。

- UnityでMahjongPrototypeシーンを再生できる
- Eastに配牌される
- Eastがツモできる
- Eastが捨てられる
- 次のターンもEastになる
- 必殺技を発動できる
- 必殺技によって次ツモが変わる
- ログにツモ、打牌、必殺技発動、効果解決が残る
- リトライできる

これができれば、最初のベース構造として成功です。
