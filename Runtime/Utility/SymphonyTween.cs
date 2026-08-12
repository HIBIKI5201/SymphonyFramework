using System;
using System.Threading;
using System.Threading.Tasks;
using SymphonyFrameWork.Debugger.Logger;
using SymphonyFrameWork.System;
using UnityEngine;

namespace SymphonyFrameWork.Utility
{
    /// <summary>
    ///     値をフレーム単位で補間するTweenを提供する。
    /// </summary>
    public static class SymphonyTween
    {
        #region 外部向けAPI

        /// <summary>
        ///     指定時間にわたり開始値から終了値まで補間する。
        /// </summary>
        /// <typeparam name="T"> 補間する値の型。 </typeparam>
        /// <param name="s"> 開始値。 </param>
        /// <param name="action"> 補間値を受け取る処理。 </param>
        /// <param name="e"> 終了値。 </param>
        /// <param name="d"> 補間時間。 </param>
        /// <param name="curve"> x軸を正規化して適用するカーブ。 </param>
        /// <param name="token"> Tweenを中断するためのトークン。 </param>
        public static async Awaitable Tweening<T>(T s, Action<T> action, T e, float d,
            AnimationCurve curve = null,
            CancellationToken token = default) where T : struct
        {
            // カーブの最終キー時刻に依存せず、0から1の経過率で評価できる形へ揃える。
            curve = NormalizeCurve(curve);
            float timer = Time.time;

            // 指定時間内は毎フレーム経過率を更新し、カーブ指定の有無に応じた補間値を通知する。
            while (Time.time <= timer + d)
            {
                float elapsed = Time.time - timer;
                float t = Mathf.Clamp01(elapsed / d);
                T? result = curve != null ? CurveValue((s, e), t, curve) : LerpValue((s, e), t);

                // 補間を定義していない型は実行を継続できないため、実行環境でも確認できるログを残す。
                if (result == null)
                {
                    SymphonyDebugLogger.LogDirect($"{typeof(T).Name}型は{nameof(Tweening)}に対応していません");
                    return;
                }

                // action未指定は通知だけを省略し、時間待機自体は継続する既存契約を維持する。
                action?.Invoke(result.Value);
                await Awaitable.NextFrameAsync(token);
            }

            // フレーム間隔による端数にかかわらず、正常完了時は終了値を必ず通知する。
            action?.Invoke(e);
        }

        /// <summary>
        ///     Pause中の経過を除外し、指定時間にわたり開始値から終了値まで補間する。
        /// </summary>
        /// <typeparam name="T"> 補間する値の型。 </typeparam>
        /// <param name="s"> 開始値。 </param>
        /// <param name="action"> 補間値を受け取る処理。 </param>
        /// <param name="e"> 終了値。 </param>
        /// <param name="d"> 補間時間。 </param>
        /// <param name="curve"> x軸を正規化して適用するカーブ。 </param>
        /// <param name="token"> Tweenを中断するためのトークン。 </param>
        public static async Awaitable PausableTweening<T>(T s, Action<T> action, T e, float d,
            AnimationCurve curve = null,
            CancellationToken token = default) where T : struct
        {
            // カーブの最終キー時刻に依存せず、0から1の経過率で評価できる形へ揃える。
            curve = NormalizeCurve(curve);
            float timer = Time.time;

            // 指定時間内は毎フレーム経過率を更新し、Pause中だけ経過時間の基準を後ろへずらす。
            while (Time.time <= timer + d)
            {
                // Pause中は補間値を進めず、キャンセルを受け取れるフレーム待機だけを行う。
                if (PauseManager.Pause)
                {
                    timer += Time.deltaTime;
                    await Awaitable.NextFrameAsync(token);
                    continue;
                }

                float elapsed = Time.time - timer;
                float t = Mathf.Clamp01(elapsed / d);
                T? result = curve != null ? CurveValue((s, e), t, curve) : LerpValue((s, e), t);

                // 補間を定義していない型は実行を継続できないため、実行環境でも確認できるログを残す。
                if (result == null)
                {
                    SymphonyDebugLogger.LogDirect($"{typeof(T).Name}型は{nameof(Tweening)}に対応していません");
                    return;
                }

                // action未指定は通知だけを省略し、時間待機自体は継続する既存契約を維持する。
                action?.Invoke(result.Value);
                await Awaitable.NextFrameAsync(token);
            }

            // フレーム間隔による端数にかかわらず、正常完了時は終了値を必ず通知する。
            action?.Invoke(e);
        }

        /// <summary>
        ///     指定時間にわたり開始値から終了値まで線形補間する。
        /// </summary>
        /// <typeparam name="T"> 線形補間する値の型。 </typeparam>
        /// <param name="s"> 開始値。 </param>
        /// <param name="action"> 補間値を受け取る処理。 </param>
        /// <param name="e"> 終了値。 </param>
        /// <param name="d"> 補間時間。 </param>
        /// <param name="token"> Tweenを中断するためのトークン。 </param>
        [Obsolete("旧型式です。" + nameof(Tweening) + "を使用する事を推奨します")]
        public static async Task TweeningLerp<T>(T s, Action<T> action, T e, float d,
            CancellationToken token = default) where T : struct
        {
            float timer = Time.time;

            // 旧APIの既存契約どおり、指定時間内は毎フレーム線形補間値を通知する。
            while (Time.time <= timer + d)
            {
                float elapsed = Time.time - timer;
                float t = Mathf.Clamp01(elapsed / d);
                T? result = LerpValue((s, e), t);

                // 補間を定義していない型は実行を継続できないため、実行環境でも確認できるログを残す。
                if (result == null)
                {
                    SymphonyDebugLogger.LogDirect($"{typeof(T).Name}型は{nameof(TweeningLerp)}に対応していません");
                    return;
                }
                // action未指定は通知だけを省略し、時間待機自体は継続する既存契約を維持する。
                action?.Invoke(result.Value);
                await Awaitable.NextFrameAsync();
            }

            // フレーム間隔による端数にかかわらず、正常完了時は終了値を必ず通知する。
            action?.Invoke(e);
        }

        /// <summary>
        ///     指定時間にわたりカーブを適用して開始値から終了値まで補間する。
        /// </summary>
        /// <typeparam name="T"> カーブ補間する値の型。 </typeparam>
        /// <param name="s"> 開始値。 </param>
        /// <param name="action"> 補間値を受け取る処理。 </param>
        /// <param name="e"> 終了値。 </param>
        /// <param name="d"> 補間時間。 </param>
        /// <param name="curve"> 補間割合へ適用するAnimationCurve。 </param>
        /// <param name="token"> Tweenを中断するためのトークン。 </param>
        [Obsolete("旧型式です。" + nameof(Tweening) + "を使用する事を推奨します")]
        public static async Task TweeningCurve<T>(T s, Action<T> action, T e, float d, AnimationCurve curve,
            CancellationToken token = default) where T : struct
        {
            // 旧APIでもカーブの最終キー時刻に依存せず、0から1の経過率で評価できる形へ揃える。
            curve = NormalizeCurve(curve);
            float timer = Time.time;

            // 旧APIの既存契約どおり、指定時間内は毎フレームカーブ補間値を通知する。
            while (Time.time <= timer + d)
            {
                float elapsed = Time.time - timer;
                float t = Mathf.Clamp01(elapsed / d);
                T? result = CurveValue((s, e), t, curve);

                // 補間を定義していない型は実行を継続できないため、実行環境でも確認できるログを残す。
                if (result == null)
                {
                    SymphonyDebugLogger.LogDirect($"{typeof(T).Name}型は{nameof(TweeningCurve)}に対応していません");
                    return;
                }
                // action未指定は通知だけを省略し、時間待機自体は継続する既存契約を維持する。
                action?.Invoke(result.Value);
                await Awaitable.NextFrameAsync();
            }

            // フレーム間隔による端数にかかわらず、正常完了時は終了値を必ず通知する。
            action?.Invoke(e);
        }

        #endregion

        #region 内部処理

        /// <summary>
        ///     対応する型の線形補間値を返す。
        /// </summary>
        /// <typeparam name="T"> 線形補間する値の型。 </typeparam>
        /// <param name="value"> 補間の開始値と終了値。 </param>
        /// <param name="t"> 0から1までの補間割合。 </param>
        /// <returns> 対応型の補間値。未対応型の場合はnull。 </returns>
        private static T? LerpValue<T>((T s, T e) value, float t) where T : struct
        {
            // 型ごとのUnity標準補間へ振り分け、未対応型は呼び出し元で診断できるnullにする。
            T? result = value switch
            {
                (int s, int e) => (T)Convert.ChangeType(Mathf.Lerp(s, e, t), typeof(T)),
                (float s, float e) => (T)Convert.ChangeType(Mathf.Lerp(s, e, t), typeof(T)),
                (Vector2 s, Vector2 e) => (T)Convert.ChangeType(Vector2.Lerp(s, e, t), typeof(T)),
                (Vector3 s, Vector3 e) => (T)Convert.ChangeType(Vector3.Lerp(s, e, t), typeof(T)),
                (Quaternion s, Quaternion e) => (T)Convert.ChangeType(Quaternion.Lerp(s, e, t), typeof(T)),
                (Color s, Color e) => (T)Convert.ChangeType(Color.Lerp(s, e, t), typeof(T)),
                _ => null
            };

            return result;
        }

        /// <summary>
        ///     対応する型のカーブ補間値を返す。
        /// </summary>
        /// <typeparam name="T"> カーブ補間する値の型。 </typeparam>
        /// <param name="value"> 補間の開始値と終了値。 </param>
        /// <param name="t"> 0から1までの正規化時間。 </param>
        /// <param name="curve"> 補間割合へ適用するAnimationCurve。 </param>
        /// <returns> 対応型の補間値。未対応型の場合はnull。 </returns>
        private static T? CurveValue<T>((T s, T e) value, float t, AnimationCurve curve) where T : struct
        {
            // 型ごとの差分へカーブ値を掛け、未対応型は呼び出し元で診断できるnullにする。
            T? result = value switch
            {
                (int s, int e) => (T)Convert.ChangeType((e - s) * curve.Evaluate(t), typeof(T)),
                (float s, float e) => (T)Convert.ChangeType((e - s) * curve.Evaluate(t), typeof(T)),
                (Vector2 s, Vector2 e) => (T)Convert.ChangeType((e - s) * curve.Evaluate(t), typeof(T)),
                (Vector3 s, Vector3 e) => (T)Convert.ChangeType((e - s) * curve.Evaluate(t), typeof(T)),
                (Color s, Color e) => (T)Convert.ChangeType((e - s) * curve.Evaluate(t), typeof(T)),
                _ => null
            };

            return result;
        }

        /// <summary>
        ///     カーブのキー時刻を0から1へ正規化する。
        /// </summary>
        /// <param name="curve"> 正規化するAnimationCurve。 </param>
        /// <returns> 最後のキー時刻が1になるよう正規化した新しいカーブ。 </returns>
        private static AnimationCurve NormalizeCurve(AnimationCurve curve)
        {
            // カーブ未指定またはキー無しの場合は、呼び出し元で線形補間へ切り替えられるnullを返す。
            if (curve == null || curve.length == 0) { return null; }

            // 最後のキー時刻を基準にし、元カーブを変更せず正規化した複製を作る。
            float maxTime = curve.keys[curve.length - 1].time;
            AnimationCurve normalizedCurve = new();

            // キーの値と接線を維持し、時刻だけを0から1の範囲へ変換する。
            foreach (Keyframe key in curve.keys)
            {
                float normalizedTime = key.time / maxTime;
                Keyframe normalizedKey = new(normalizedTime, key.value, key.inTangent, key.outTangent);
                normalizedCurve.AddKey(normalizedKey);
            }

            return normalizedCurve;
        }

        #endregion
    }
}
