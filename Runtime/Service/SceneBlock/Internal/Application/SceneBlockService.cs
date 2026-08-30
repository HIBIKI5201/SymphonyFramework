using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using SymphonyFrameWork.System.SceneLoad;

namespace SymphonyFrameWork.System.SceneBlock
{
    /// <summary>
    ///     Scene Blockのロードとアンロードの手順、および保持規則を実行する。
    /// </summary>
    internal sealed class SceneBlockService
    {
        #region 外部向けAPI

        /// <summary>
        ///     追跡状態とシーン操作の実行手段を指定して生成する。
        /// </summary>
        /// <param name="registry"> ブロックとシーン保持の追跡状態。 </param>
        /// <param name="loader"> シーン操作の実行手段。 </param>
        /// <exception cref="ArgumentNullException"> registryまたはloaderがnullの場合。 </exception>
        internal SceneBlockService(SceneBlockRegistry registry, IBlockSceneLoader loader)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        /// <summary> 追跡状態が変化したときに発行する。 </summary>
        internal event Action OnStateChanged;

        /// <summary>
        ///     ブロックの全シーンを依存順にロードする。
        /// </summary>
        /// <param name="blockName"> ブロック名。 </param>
        /// <param name="assetInstanceId"> 由来するアセットのインスタンスID。 </param>
        /// <param name="entries"> ブロックが持つエントリ一覧。 </param>
        /// <param name="progress"> ブロック全体の進捗の通知先。 </param>
        /// <param name="token"> 処理を中断するトークン。 </param>
        /// <returns> 全シーンをロードできた場合はtrue。 </returns>
        /// <exception cref="SceneBlockPlanException"> 依存グラフを解決できない場合。 </exception>
        /// <exception cref="InvalidOperationException"> 同名の別アセットが既にロード済みの場合。 </exception>
        internal async Task<bool> LoadBlock(
            string blockName,
            int assetInstanceId,
            IReadOnlyList<SceneBlockEntry> entries,
            IProgress<float> progress,
            CancellationToken token)
        {
            // 既に追跡中のブロックは、同じアセットであれば冪等に成功として扱う。
            if (_registry.TryGet(blockName, out SceneBlockLoadEntity tracked))
            {
                EnsureSameAsset(tracked, blockName, assetInstanceId);
                progress?.Report(1f);
                return true;
            }

            // 依存グラフの異常はアセットの記述誤りであり、部分的な実行層で走らせない。
            if (!SceneBlockEntryReader.TryCreateLayers(
                    entries,
                    out IReadOnlyList<IReadOnlyList<string>> layers,
                    out IReadOnlyList<string> errorDescriptions))
            {
                throw new SceneBlockPlanException(blockName, errorDescriptions);
            }

            SceneBlockLoadEntity entity = new(
                blockName,
                assetInstanceId,
                layers,
                SceneBlockEntryReader.ReadPriorities(entries),
                SceneBlockEntryReader.ReadPersistentSceneNames(entries));
            _registry.Register(entity);
            entity.BeginLoading();
            NotifyStateChanged();

            int completedSceneCount = 0;
            foreach (IReadOnlyList<string> layer in layers)
            {
                // 保持と外部保持の記録は、実際のロードを開始する前に確定させる。
                List<SceneLoadRequest> requests = new(layer.Count);
                foreach (string sceneName in layer)
                {
                    bool isLoadedOutsideBlocks =
                        !_registry.IsHeldByAnyBlock(sceneName) && _loader.IsSceneLoaded(sceneName);
                    if (isLoadedOutsideBlocks) { _registry.MarkExternallyHeld(sceneName); }

                    _registry.AddHolder(sceneName, blockName);

                    // 既にロード済みのシーンは、他のブロックや利用側の所有物として再ロードしない。
                    if (!_loader.IsSceneLoaded(sceneName))
                    {
                        requests.Add(new SceneLoadRequest(sceneName, entity.GetPriority(sceneName)));
                    }
                }

                if (requests.Count > 0)
                {
                    int layerBaseCount = completedSceneCount;
                    IProgress<float> layerProgress = new BlockProgress(
                        value => ReportBlockProgress(
                            entity,
                            progress,
                            layerBaseCount + (value * requests.Count),
                            entity.SceneCount));

                    // 失敗しても既にロードできたシーンは残し、保持の記録もそのままにする。
                    if (!await _loader.LoadScenesAsync(requests, layerProgress, token))
                    {
                        NotifyStateChanged();
                        return false;
                    }
                }

                completedSceneCount += layer.Count;
                ReportBlockProgress(entity, progress, completedSceneCount, entity.SceneCount);
            }

            entity.CompleteLoading();
            progress?.Report(1f);
            NotifyStateChanged();
            return true;
        }

        /// <summary>
        ///     ブロックの保持を解き、保持が残らないシーンだけをアンロードする。
        /// </summary>
        /// <param name="blockName"> ブロック名。 </param>
        /// <param name="assetInstanceId"> 由来するアセットのインスタンスID。 </param>
        /// <param name="progress"> ブロック全体の進捗の通知先。 </param>
        /// <param name="token"> 処理を中断するトークン。 </param>
        /// <returns> アンロードが必要なシーンをすべて処理できた場合はtrue。 </returns>
        /// <exception cref="InvalidOperationException"> 同名の別アセットが追跡中の場合。 </exception>
        internal async Task<bool> UnloadBlock(
            string blockName,
            int assetInstanceId,
            IProgress<float> progress,
            CancellationToken token)
        {
            // 追跡していないブロックは、既に片付いているものとして成功で返す。
            if (!_registry.TryGet(blockName, out SceneBlockLoadEntity entity))
            {
                progress?.Report(1f);
                return true;
            }

            EnsureSameAsset(entity, blockName, assetInstanceId);

            entity.BeginUnloading();
            NotifyStateChanged();

            bool isAllUnloaded = true;
            int processedSceneCount = 0;

            // 依存の後ろから解いていき、先行シーンが後に残るようにする。
            for (int layerIndex = entity.Layers.Count - 1; layerIndex >= 0; layerIndex--)
            {
                IReadOnlyList<string> layer = entity.Layers[layerIndex];
                List<string> unloadTargets = new(layer.Count);

                foreach (string sceneName in layer)
                {
                    bool hasNoHolder = _registry.ReleaseHolder(sceneName, blockName);

                    // 他のブロックの保持、ブロック外のロード、永続指定のどれかがあれば残す。
                    if (!hasNoHolder) { continue; }
                    if (_registry.IsExternallyHeld(sceneName)) { continue; }
                    if (entity.IsPersistent(sceneName)) { continue; }

                    // ロードに失敗して実在しないシーンは、保持だけ解いてアンロードを要求しない。
                    if (!_loader.IsSceneLoaded(sceneName)) { continue; }

                    unloadTargets.Add(sceneName);
                }

                if (unloadTargets.Count > 0)
                {
                    int layerBaseCount = processedSceneCount;
                    IProgress<float> layerProgress = new BlockProgress(
                        value => ReportBlockProgress(
                            entity,
                            progress,
                            layerBaseCount + (value * unloadTargets.Count),
                            entity.SceneCount));

                    if (!await _loader.UnloadScenesAsync(unloadTargets, layerProgress, token))
                    {
                        isAllUnloaded = false;
                    }
                }

                processedSceneCount += layer.Count;
                ReportBlockProgress(entity, progress, processedSceneCount, entity.SceneCount);
            }

            // 保持を解いた後で追跡から外し、Queryが中途半端な状態を読まないようにする。
            _registry.Remove(blockName);
            progress?.Report(1f);
            NotifyStateChanged();
            return isAllUnloaded;
        }

        #endregion

        #region 内部処理

        private readonly SceneBlockRegistry _registry;
        private readonly IBlockSceneLoader _loader;

        /// <summary>
        ///     追跡中のブロックが同じアセット由来か検証する。
        /// </summary>
        /// <param name="entity"> 追跡中のブロック。 </param>
        /// <param name="blockName"> 要求されたブロック名。 </param>
        /// <param name="assetInstanceId"> 要求されたアセットのインスタンスID。 </param>
        /// <exception cref="InvalidOperationException"> 同名の別アセットの場合。 </exception>
        private static void EnsureSameAsset(
            SceneBlockLoadEntity entity,
            string blockName,
            int assetInstanceId)
        {
            // 名前が衝突したまま進めると、片方のアセットの保持がもう片方の操作で解かれる。
            if (entity.AssetInstanceId == assetInstanceId) { return; }

            throw new InvalidOperationException(
                $"Scene Block {blockName} は別のアセットで既にロードされています。"
                + " ブロック名が重複しないようにしてください。");
        }

        /// <summary>
        ///     ブロック全体の進捗をEntityと呼び出し側へ反映する。
        /// </summary>
        /// <param name="entity"> 対象のブロック。 </param>
        /// <param name="progress"> 呼び出し側の通知先。 </param>
        /// <param name="processedSceneCount"> 処理済みとみなすシーン数。 </param>
        /// <param name="sceneCount"> ブロックのシーン総数。 </param>
        private void ReportBlockProgress(
            SceneBlockLoadEntity entity,
            IProgress<float> progress,
            float processedSceneCount,
            int sceneCount)
        {
            // シーンを持たないブロックは進捗を割れないため、完了として扱う。
            float value = sceneCount > 0 ? processedSceneCount / sceneCount : 1f;

            if (entity.ReportProgress(value)) { NotifyStateChanged(); }

            progress?.Report(entity.Progress);
        }

        /// <summary>
        ///     追跡状態の変化を購読者へ通知する。
        /// </summary>
        private void NotifyStateChanged() => OnStateChanged?.Invoke();

        /// <summary>
        ///     層の進捗をブロック全体の進捗へ変換して中継する。
        /// </summary>
        private sealed class BlockProgress : IProgress<float>
        {
            #region 外部向けAPI

            /// <summary>
            ///     中継先の処理を指定して生成する。
            /// </summary>
            /// <param name="report"> 層の進捗を受け取る処理。 </param>
            internal BlockProgress(Action<float> report)
            {
                _report = report;
            }

            /// <summary>
            ///     層の進捗を中継する。
            /// </summary>
            /// <param name="value"> 0から1の範囲に正規化された層の進捗。 </param>
            public void Report(float value) => _report?.Invoke(value);

            #endregion

            #region 内部処理

            private readonly Action<float> _report;

            #endregion
        }

        #endregion
    }
}
