# WataOfuton_AvatarAssets

アバター用プロジェクト向けに作ったものをまとめています.

配布場所 Booth ( https://wata-ofuton.booth.pm/ )

何かありましたら Booth のメッセージ機能にてご連絡ください.

---

## 前提環境
動作確認環境
- Unity2022.3.22f1

以下のツールを事前に Project へインポートしてください. なお,常に最新バージョンを使用してください.
- VRChat SDK - Avatars
- [lilToon]( https://github.com/lilxyzw/lilToon "lilToon")
- [Modular Avatar]( https://github.com/bdunderscore/modular-avatar "Modular Avatar")
- [Avatar Optimizer]( https://github.com/anatawa12/AvatarOptimizer "AAO Avatar Optimizer")

---

## Shader関連

- [ClipCostumeShaders]( https://wata-ofuton.booth.pm/items/1330347 "ClipCostumeShaders")
  - アバターの衣服等の指定した部分を自分の視点からのみ見えなくするシェーダー. 
  - 説明書は配布している zip ファイル内のテキストを参照のこと.
  - 各オリジナル Shader の MITLicense 下において配布

- [SleepSphere]( https://wata-ofuton.booth.pm/items/2880029 "SleepSphere")
  - 指定した時間の間アバターの頭部を動かさなかったとき,視界を覆う黒い球体が出現する. HMDを装着したまま寝落ちしたときの目へのダメージを軽減する目的に作成. 
  - 説明書は配布している zip ファイル内のテキストを参照のこと.
  - Released under the MIT license.

## エディタ拡張

- [SymmetryBoneEditor]( https://wata-ofuton.booth.pm/items/6085959 "SymmetryBoneEditor")
  - 左右対称のオブジェクトに Transform の変更を自動で適用する. 
  - 説明書は[こちら]( https://docs.google.com/document/d/1eX_oTiDfT1oAAcRh0ilsAMqWQLj6wiCdB4IbOM6P_Cs/edit?usp=sharing "説明書")
  - Released under the MIT license.

- [MaterialPropartyBatchSetter]( https://wata-ofuton.booth.pm/items/6088555 "MaterialPropartyBatchSetter")
  - lilToon を使用しているマテリアルのうち,主にメッシュの明るさに影響を与えるパラメータを自動で一括設定する. 
  - 説明書は[こちら]( https://docs.google.com/document/d/1tEvEPy0aCbA50ENEGCZ7Ya6HmuupRtC4MCmr142Cd2M/edit?usp=sharing "説明書")
  - Released under the MIT license.

- [Cloth Transform Applier]( https://wata-ofuton.booth.pm/items/6291387 "ClothTransformApplier")
  - Preset からボーンの移動情報を読み込み,ボーンを動かす. 
  - 説明書は[こちら]( https://docs.google.com/document/d/1k-FENC0ggCp_mU0YXnYmKYxPT59GcC3Y7EACWipeir0/edit?usp=sharing "説明書")
  - Released under the MIT license.

- [ますきゃの首の繋ぎ目を整えるツール]( https://wata-ofuton.booth.pm/items/6696521 "MCP_MergeNeck")
  - オリジナル3Dモデル『量産型のらきゃっと　ぷらす』（ますきゃっと ぷらす）（ https://noracat.booth.pm/items/4360118 ）の首周りのメッシュを編集し法線を整えたデータを出力する. 
  - 説明書は[こちら]( https://docs.google.com/document/d/1sE8KZtqwY5ECTBbkkscDGosbT3OlXO8faAX3JE2GOr0/edit?usp=sharing "説明書")
  - Released under the MIT license.

## NDMF系ツール

- [MeshEditPack]( https://wata-ofuton.booth.pm/items/6431920 "MeshEditPack")
  - メッシュ編集系のスクリプトをまとめています. 現在は以下の2つです.
    - ReverseMeshND
    - RemoveMeshByBlendshape
  - 説明書は[こちら]( https://docs.google.com/document/d/1bmrAbSxbiKOJLgbfuXqz27bv4Cj5wEa2deOlP_Pos00/edit?usp=sharing "説明書")
  - Released under the MIT license.

- [MMDSetup]( https://wata-ofuton.booth.pm/items/5527563 "MMDSetup")
  - 半自動でMMDワールドへ対応. 
  - 説明書は[こちら]( https://docs.google.com/document/d/152KRh3asmLLdzXKZOViy10mEc_G9Bkpho1j-09jQ8Xk/edit?usp=sharing "説明書")
  - Released under the CC0 license.
