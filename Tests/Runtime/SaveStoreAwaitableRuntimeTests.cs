using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using SymphonyFrameWork.System.SaveSystem;
using SymphonyFrameWork.Utility;

using UnityEngine;
using UnityEngine.TestTools;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     SaveStoreの非同期APIがAwaitableへ移行しても、往復と重複排除の契約を
    ///     保つことを検証する。SaveStoreはPlay Mode開始時に初期化される。
    /// </summary>
    public sealed class SaveStoreAwaitableRuntimeTests
    {
        /// <summary> 検証用のセーブデータ型。 </summary>
        [Serializable]
        public sealed class AwaitableTestSaveData : SaveDataContent
        {
            public int Value;
        }

        /// <summary>
        ///     未読み込み状態の検証専用型。
        ///     Storeのキャッシュはstaticで他テストへ持ち越されるため、
        ///     この型はどのテストからもロードしない。
        /// </summary>
        [Serializable]
        public sealed class NeverLoadedTestSaveData : SaveDataContent
        {
            public int Value;
        }

        /// <summary> 検証で書き込んだ保存データを消し、次のテストへ持ち越さない。 </summary>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Task task = SymphonyAwaitable.AsTask(
                SaveStore.DeleteAsync<AwaitableTestSaveData>());

            yield return new WaitUntil(() => task.IsCompleted);

            // 削除自体の失敗はテスト対象ではないため、例外は観測して捨てる。
            _ = task.Exception;
        }

        /// <summary> 保存した値が読み込みで戻る。 </summary>
        [UnityTest]
        public IEnumerator SaveAsync_ThenLoadAsync_RoundTripsValue()
        {
            // 3.0.0からGetは暗黙のロードを行わないため、先にロードして正本を確定させる。
            Task<AwaitableTestSaveData> initialTask = SymphonyAwaitable.AsTask(
                SaveStore.LoadAsync<AwaitableTestSaveData>());
            yield return new WaitUntil(() => initialTask.IsCompleted);
            initialTask.GetAwaiter().GetResult().Value = 42;

            Task saveTask = SymphonyAwaitable.AsTask(
                SaveStore.SaveAsync<AwaitableTestSaveData>());
            yield return new WaitUntil(() => saveTask.IsCompleted);
            saveTask.GetAwaiter().GetResult();

            SaveStore.Get<AwaitableTestSaveData>().Value = 0;

            Task<AwaitableTestSaveData> loadTask = SymphonyAwaitable.AsTask(
                SaveStore.LoadAsync<AwaitableTestSaveData>());
            yield return new WaitUntil(() => loadTask.IsCompleted);

            AwaitableTestSaveData loaded = loadTask.GetAwaiter().GetResult();
            Assert.That(loaded.Value, Is.EqualTo(42));
        }

        /// <summary> 削除で既定値へ戻る。 </summary>
        [UnityTest]
        public IEnumerator DeleteAsync_ResetsToDefault()
        {
            Task<AwaitableTestSaveData> initialTask = SymphonyAwaitable.AsTask(
                SaveStore.LoadAsync<AwaitableTestSaveData>());
            yield return new WaitUntil(() => initialTask.IsCompleted);
            initialTask.GetAwaiter().GetResult().Value = 7;

            Task saveTask = SymphonyAwaitable.AsTask(
                SaveStore.SaveAsync<AwaitableTestSaveData>());
            yield return new WaitUntil(() => saveTask.IsCompleted);
            saveTask.GetAwaiter().GetResult();

            Task deleteTask = SymphonyAwaitable.AsTask(
                SaveStore.DeleteAsync<AwaitableTestSaveData>());
            yield return new WaitUntil(() => deleteTask.IsCompleted);
            deleteTask.GetAwaiter().GetResult();

            Assert.That(SaveStore.Get<AwaitableTestSaveData>().Value, Is.Zero);
        }

        /// <summary>
        ///     **未読み込みの型に対するGetは暗黙のロードを行わず例外にする。**
        ///     暗黙の同期ロードを残すと、非同期I/Oを行うLoaderで待機を同期ブロックし、
        ///     PlayerLoopで進む処理が完了不能になる。3.0.0で廃止した契約の回帰テスト。
        /// </summary>
        [Test]
        public void Get_NotLoaded_ThrowsInvalidOperation()
        {
            Assert.That(SaveStore.IsLoaded<NeverLoadedTestSaveData>(), Is.False);
            Assert.Throws<InvalidOperationException>(
                () => SaveStore.Get<NeverLoadedTestSaveData>());
        }

        /// <summary> ロード後はIsLoadedがtrueになり、Getが同じインスタンスを返す。 </summary>
        [UnityTest]
        public IEnumerator Get_AfterLoad_ReturnsLoadedInstance()
        {
            Task<AwaitableTestSaveData> loadTask = SymphonyAwaitable.AsTask(
                SaveStore.LoadAsync<AwaitableTestSaveData>());
            yield return new WaitUntil(() => loadTask.IsCompleted);

            AwaitableTestSaveData loaded = loadTask.GetAwaiter().GetResult();

            Assert.That(SaveStore.IsLoaded<AwaitableTestSaveData>(), Is.True);
            Assert.That(SaveStore.Get<AwaitableTestSaveData>(), Is.SameAs(loaded));
        }

        /// <summary>
        ///     **同じ型へ続けてLoadAsyncを呼んでも両方が完了する。**
        ///     内部は重複排除で1つのTaskを共有するが、Awaitableは共有できないため
        ///     呼び出しごとに別のAwaitableを作っている。その両立の回帰テスト。
        /// </summary>
        [UnityTest]
        public IEnumerator LoadAsync_CalledTwice_BothComplete()
        {
            Task first = SymphonyAwaitable.AsTask(
                SaveStore.LoadAsync(typeof(AwaitableTestSaveData)));
            Task second = SymphonyAwaitable.AsTask(
                SaveStore.LoadAsync(typeof(AwaitableTestSaveData)));

            yield return new WaitUntil(() => first.IsCompleted && second.IsCompleted);

            first.GetAwaiter().GetResult();
            second.GetAwaiter().GetResult();
        }

        /// <summary>
        ///     キャンセル済みトークンではOperationCanceledExceptionになる。
        ///     Awaitableを直接awaitして、利用側が受け取る型を観測する。
        /// </summary>
        [UnityTest]
        public IEnumerator SaveAsync_Canceled_ThrowsOperationCanceled()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            Exception captured = null;

            async Task AwaitDirectlyAsync()
            {
                try
                {
                    await SaveStore.SaveAsync<AwaitableTestSaveData>(cancellation.Token);
                }
                catch (Exception exception)
                {
                    captured = exception;
                }
            }

            Task task = AwaitDirectlyAsync();

            yield return new WaitUntil(() => task.IsCompleted);

            Assert.That(captured, Is.InstanceOf<OperationCanceledException>());
        }

        /// <summary> 不正な型は同期的に例外になる。 </summary>
        [Test]
        public void LoadAsync_NullType_ThrowsSynchronously()
        {
            Assert.Throws<ArgumentNullException>(() => SaveStore.LoadAsync(null));
        }
    }
}
