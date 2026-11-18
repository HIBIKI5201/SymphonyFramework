# Audio Manager

AudioMixerのグループごとにAudioSourceを用意し、グループ単位で音量を変えます。

## 目的と範囲

BGM・SEなどの出力先ごとに、再生用のAudioSourceと音量の操作口を1つずつ持てるようにします。

- **すること**: グループごとのAudioSourceの生成と保持（シーン遷移後も残る）、0〜1の比率による音量の変更
- **しないこと**: クリップの選択と再生（取得したAudioSourceで利用側が行う）、音量設定の保存、AudioMixerやグループの作成

## 入口

| 項目 | 内容 |
| --- | --- |
| namespace | `SymphonyFramework.System` |
| 主な公開型 | `AudioManager` |
| メニューパス | なし |
| 設定の保存先 | `Assets/Resources/SymphonyFramework/AudioConfig.asset` |

## 仕組みの要点

1. **グループは`AudioConfig.asset`に書いた名前で引きます。** 名前はAudioMixer内のグループ名と一致させます。
2. **AudioSourceは、最初に`GetAudioSource`か`VolumeSliderChanged`を呼んだ時点でまとめて作られます。** AudioMixerが未設定ならAudioSourceも、それを持つGameObjectも作られません。
3. **音量はAudioMixerの公開パラメーターへdBで書き込みます。** 利用側は0〜1の比率を渡し、dBへの変換はこのモジュールが行います。

## 機能一覧

| ID | 機能 | 使うAPI | 結果 |
| --- | --- | --- | --- |
| AUD-01 | AudioSourceの取得 | `AudioManager.GetAudioSource(name)` | 指定グループへ出力するAudioSourceを返す。未登録なら`null` |
| AUD-02 | 音量の変更 | `AudioManager.VolumeSliderChanged(name, value)` | グループの公開パラメーターへ、比率から変換したdBを設定する |

`AudioConfig.asset`の項目は次のとおりです。

| 項目 | 内容 |
| --- | --- |
| Audio Mixer | 再生と音量制御に使うAudioMixer |
| Audio Group Setting List > Audio Group Name | AudioMixer内のグループ名 |
| Audio Group Setting List > Exposed Parameter Name | 音量変更に使う公開パラメーター名 |
| Audio Group Setting List > Is Loop | このグループのAudioSourceをループ再生にするか |

## クイックスタート

```csharp
using SymphonyFramework.System;
using UnityEngine;

AudioSource bgm = AudioManager.GetAudioSource("BGM");
bgm.clip = bgmClip;
bgm.Play();

AudioManager.VolumeSliderChanged("BGM", 0.5f);
```

## うまくいかないとき

| 起きること | この機能の動き | 対処 |
| --- | --- | --- |
| AudioMixerが未設定 | 警告「オーディオミキサーがアサインされていません」を1回だけ出す。`GetAudioSource`は`null` | `AudioConfig.asset`のAudio Mixerを設定する |
| グループ名がAudioMixerに無い | そのグループのAudioSourceを作らない | Audio Group Nameを、AudioMixer内のグループ名と一致させる |
| 未登録のグループ名を渡した | `GetAudioSource`は`null`、`VolumeSliderChanged`は何もしない | `AudioConfig.asset`へグループを足す |
| 公開パラメーターが見つからない | `VolumeSliderChanged`が警告「<グループ名>のボリュームがありません」を出し、音量を変えない | AudioMixerで音量を公開し、その名前をExposed Parameter Nameへ書く |
| 名前が空、または比率が0〜1の外 | `ArgumentException`、`ArgumentOutOfRangeException`を投げる | 呼び出し側の値を直す |
| 初期化前に呼んだ | `SymphonyNotInitializedException`を投げる | 呼び出しを初期化後へ移す |

## 守ること

- `VolumeSliderChanged`へ渡す値はdBではなく0〜1の比率にする。
- グループ名の綴りの誤りを防ぐには、[AutoEnumGenerator](./AutoEnumGenerator.md)が生成する`AudioGroupTypeEnum`の`ToString()`を渡す。

## 確かめ方

- Play Modeで`GetAudioSource`を呼び、戻り値が`null`でないことと、`outputAudioMixerGroup`が目的のグループであることを確かめる。
- Console（または`Library/SymphonyFramework/Cache/Log.txt`）に、グループごとの「正常に追加されました」が出ていることを見る。

## 関連

- [AutoEnumGenerator](./AutoEnumGenerator.md)
- [Architecture.md](../Architecture.md)
- [Deprecations.md](../Deprecations.md)
- [CHANGELOG.md](../../CHANGELOG.md)
