using NUnit.Framework;

using SymphonyFrameWork.Orchestrator;

using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace SymphonyFrameWork.Tests
{
    /// <summary> PlayerLoopへの同期フェーズ挿入・除去を検証する。 </summary>
    public sealed class SymphonyFrameworkLifecycleLoopTests
    {
        /// <summary> PlayerLoopはプロセス全体で共有されるため、テストごとに必ず後始末する。 </summary>
        [TearDown]
        public void TearDown()
        {
            SymphonyFrameworkLifecycleLoop.Uninstall();
        }

        /// <summary> 挿入したノードがUpdate.ScriptRunBehaviourUpdateの直前に位置する。 </summary>
        [Test]
        public void Install_InsertsSystemImmediatelyBeforeScriptRunBehaviourUpdate()
        {
            SymphonyFrameworkLifecycleLoop.Install(() => { });

            (int insertedIndex, int scriptRunIndex) = FindIndices();

            Assert.That(insertedIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(scriptRunIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(insertedIndex, Is.EqualTo(scriptRunIndex - 1));
        }

        /// <summary> 二重Installでもノードは1個だけになる。 </summary>
        [Test]
        public void Install_CalledTwice_DoesNotDuplicateSystem()
        {
            SymphonyFrameworkLifecycleLoop.Install(() => { });
            SymphonyFrameworkLifecycleLoop.Install(() => { });

            Assert.That(CountInsertedSystems(), Is.EqualTo(1));
        }

        /// <summary> UninstallでPlayerLoopから除去される。 </summary>
        [Test]
        public void Uninstall_RemovesInsertedSystem()
        {
            SymphonyFrameworkLifecycleLoop.Install(() => { });

            SymphonyFrameworkLifecycleLoop.Uninstall();

            Assert.That(CountInsertedSystems(), Is.Zero);
            Assert.That(SymphonyFrameworkLifecycleLoop.IsInstalled, Is.False);
        }

        /// <summary> 未挿入状態でのUninstallは例外を投げない。 </summary>
        [Test]
        public void Uninstall_WithoutInstall_DoesNothing()
        {
            Assert.DoesNotThrow(() => SymphonyFrameworkLifecycleLoop.Uninstall());
        }

        /// <summary> 挿入したノードのdelegateを直接呼ぶとtickが実行される。 </summary>
        [Test]
        public void Install_InvokesTickEveryManualUpdateCall()
        {
            int callCount = 0;
            SymphonyFrameworkLifecycleLoop.Install(() => callCount++);

            InvokeInsertedSystem();
            InvokeInsertedSystem();

            Assert.That(callCount, Is.EqualTo(2));
        }

        /// <summary> Update配下で挿入済みマーカー型と、Update.ScriptRunBehaviourUpdateのindexを探す。 </summary>
        private static (int insertedIndex, int scriptRunIndex) FindIndices()
        {
            PlayerLoopSystem[] children = GetUpdateChildren();

            int insertedIndex = -1;
            int scriptRunIndex = -1;
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].type == typeof(Update.ScriptRunBehaviourUpdate)) { scriptRunIndex = i; }
                else if (children[i].type != null
                    && children[i].type.Name == "SymphonyFrameworkLifecycleSystem") { insertedIndex = i; }
            }

            return (insertedIndex, scriptRunIndex);
        }

        private static int CountInsertedSystems()
        {
            PlayerLoopSystem[] children = GetUpdateChildren();

            int count = 0;
            foreach (PlayerLoopSystem child in children)
            {
                if (child.type != null && child.type.Name == "SymphonyFrameworkLifecycleSystem") { count++; }
            }

            return count;
        }

        private static void InvokeInsertedSystem()
        {
            PlayerLoopSystem[] children = GetUpdateChildren();

            foreach (PlayerLoopSystem child in children)
            {
                if (child.type == null || child.type.Name != "SymphonyFrameworkLifecycleSystem") { continue; }

                child.updateDelegate.Invoke();
                return;
            }

            Assert.Fail("挿入済みの同期フェーズが見つかりませんでした。");
        }

        private static PlayerLoopSystem[] GetUpdateChildren()
        {
            PlayerLoopSystem rootLoop = PlayerLoop.GetCurrentPlayerLoop();
            foreach (PlayerLoopSystem topSystem in rootLoop.subSystemList)
            {
                if (topSystem.type == typeof(Update)) { return topSystem.subSystemList; }
            }

            Assert.Fail("Updateノードが見つかりませんでした。");
            return null;
        }
    }
}
