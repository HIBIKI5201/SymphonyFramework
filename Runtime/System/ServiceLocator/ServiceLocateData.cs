using System;
using System.Collections.Generic;
using UnityEngine;

namespace SymphonyFrameWork.System.ServiceLocate
{
    public class ServiceLocateData
    {
        public ServiceLocateData()
        {
            _gameObject = new GameObject("ServiceLocateData");
            SymphonyCoreSystem.MoveObjectToSymphonySystem(_gameObject);
        }

        public GameObject Instance => _gameObject;
        public Dictionary<Type, object> SingletonObjects => _singletonObjects;
        public Dictionary<Type, Action> WaitingActions => _waitingActions;
        public Dictionary<Type, Delegate> WaitingActionsWithInstance => _waitingActionsWithInstance;

        [Tooltip("登録されているインスタンスを型をキーにして保持する辞書")]
        private readonly Dictionary<Type, object> _singletonObjects = new();

        [Tooltip("インスタンス登録まで待機してから実行されるコールバックアクションを保持する辞書")]
        private readonly Dictionary<Type, Action> _waitingActions = new();
        [Tooltip("インスタンス登録まで待機し、登録されたインスタンスを引数として受け取るコールバックアクションを保持する辞書")]
        private readonly Dictionary<Type, Delegate> _waitingActionsWithInstance = new();

        private readonly GameObject _gameObject;

        public bool IsValid() =>
            Instance != null && Instance.activeInHierarchy && Instance.scene.isLoaded;
    }
}
