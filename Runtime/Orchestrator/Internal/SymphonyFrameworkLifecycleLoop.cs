using System;
using System.Collections.Generic;

using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace SymphonyFrameWork.Orchestrator
{
    /// <summary>
    ///     UnityのPlayerLoopへ、OnEnableとStartの間で実行されるフレームワークの同期フェーズを挿入する。
    /// </summary>
    /// <remarks>
    ///     <see cref="SymphonyOrchestrator"/>だけが呼び出す。挿入位置は<see cref="Update.ScriptRunBehaviourUpdate"/>の
    ///     直前で、Unityが保留中の<c>Start</c>をまとめて実行してから<c>Update</c>へ進む直前にあたる。
    /// </remarks>
    internal static class SymphonyFrameworkLifecycleLoop
    {
        #region 外部向けAPI

        /// <summary> フレームワークの同期フェーズがPlayerLoopへ挿入済みかどうか。 </summary>
        internal static bool IsInstalled { get; private set; }

        /// <summary>
        ///     <paramref name="tick"/>を<see cref="Update.ScriptRunBehaviourUpdate"/>の直前へ挿入する。
        /// </summary>
        /// <param name="tick"> 毎フレームStart呼び出しの直前に実行するcallback。 </param>
        /// <exception cref="ArgumentNullException"> <paramref name="tick"/>がnullの場合。 </exception>
        /// <exception cref="InvalidOperationException">
        ///     <see cref="Update.ScriptRunBehaviourUpdate"/>がPlayerLoopから見つからない場合。
        /// </exception>
        /// <remarks> Domain Reload無効での再初期化に備え、多重挿入を避けるため必ず一度<see cref="Uninstall"/>してから積み直す。 </remarks>
        internal static void Install(PlayerLoopSystem.UpdateFunction tick)
        {
            if (tick == null) { throw new ArgumentNullException(nameof(tick)); }

            // 前回挿入分が残っていると同じtickが二重に走るため、必ず取り除いてから積み直す。
            Uninstall();

            PlayerLoopSystem rootLoop = PlayerLoop.GetCurrentPlayerLoop();
            if (!TryInsertBeforeScriptRunBehaviourUpdate(ref rootLoop, tick))
            {
                throw new InvalidOperationException(
                    $"{nameof(Update)}.{nameof(Update.ScriptRunBehaviourUpdate)}が見つからず、"
                    + $"フレームワークの同期フェーズを挿入できませんでした。");
            }

            PlayerLoop.SetPlayerLoop(rootLoop);
            IsInstalled = true;
        }

        /// <summary>
        ///     挿入済みの同期フェーズをPlayerLoopから取り除く。
        /// </summary>
        /// <remarks> 未挿入の場合は何もしない。 </remarks>
        internal static void Uninstall()
        {
            if (!IsInstalled) { return; }

            PlayerLoopSystem rootLoop = PlayerLoop.GetCurrentPlayerLoop();
            RemoveInsertedSystem(ref rootLoop);
            PlayerLoop.SetPlayerLoop(rootLoop);
            IsInstalled = false;
        }

        #endregion

        #region 内部処理

        /// <summary> 挿入したノードを識別するためだけのマーカー型。 </summary>
        private struct SymphonyFrameworkLifecycleSystem
        {
        }

        /// <summary>
        ///     PlayerLoopの<see cref="Update"/>ノード配下を探し、<see cref="Update.ScriptRunBehaviourUpdate"/>の
        ///     直前へ<paramref name="tick"/>を持つノードを挿入する。
        /// </summary>
        /// <returns> 挿入できた場合はtrue。 </returns>
        private static bool TryInsertBeforeScriptRunBehaviourUpdate(
            ref PlayerLoopSystem rootLoop,
            PlayerLoopSystem.UpdateFunction tick)
        {
            PlayerLoopSystem[] topSystems = rootLoop.subSystemList;
            if (topSystems == null) { return false; }

            for (int i = 0; i < topSystems.Length; i++)
            {
                if (topSystems[i].type != typeof(Update)) { continue; }

                PlayerLoopSystem updatePhase = topSystems[i];
                PlayerLoopSystem[] children = updatePhase.subSystemList;
                if (children == null) { return false; }

                int insertIndex = -1;
                for (int j = 0; j < children.Length; j++)
                {
                    if (children[j].type != typeof(Update.ScriptRunBehaviourUpdate)) { continue; }

                    insertIndex = j;
                    break;
                }
                if (insertIndex < 0) { return false; }

                PlayerLoopSystem[] newChildren = new PlayerLoopSystem[children.Length + 1];
                Array.Copy(children, 0, newChildren, 0, insertIndex);
                newChildren[insertIndex] = new PlayerLoopSystem
                {
                    type = typeof(SymphonyFrameworkLifecycleSystem),
                    updateDelegate = tick,
                };
                Array.Copy(children, insertIndex, newChildren, insertIndex + 1, children.Length - insertIndex);

                updatePhase.subSystemList = newChildren;
                topSystems[i] = updatePhase;
                rootLoop.subSystemList = topSystems;
                return true;
            }

            return false;
        }

        /// <summary>
        ///     PlayerLoopの<see cref="Update"/>ノード配下から、挿入済みのマーカー型を持つノードを取り除く。
        /// </summary>
        private static void RemoveInsertedSystem(ref PlayerLoopSystem rootLoop)
        {
            PlayerLoopSystem[] topSystems = rootLoop.subSystemList;
            if (topSystems == null) { return; }

            for (int i = 0; i < topSystems.Length; i++)
            {
                if (topSystems[i].type != typeof(Update)) { continue; }

                PlayerLoopSystem updatePhase = topSystems[i];
                PlayerLoopSystem[] children = updatePhase.subSystemList;
                if (children == null) { return; }

                List<PlayerLoopSystem> filtered = new(children.Length);
                foreach (PlayerLoopSystem child in children)
                {
                    if (child.type != typeof(SymphonyFrameworkLifecycleSystem)) { filtered.Add(child); }
                }

                updatePhase.subSystemList = filtered.ToArray();
                topSystems[i] = updatePhase;
                rootLoop.subSystemList = topSystems;
                return;
            }
        }

        #endregion
    }
}
