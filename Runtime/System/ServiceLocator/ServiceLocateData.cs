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
        public Dictionary<Type, object> LocateObjects => _locateObjects;

        public bool Add(Type type, object obj)
        {
            return _locateObjects.TryAdd(type, obj);
        }

        public bool Remove(Type type)
        {
            if (_locateObjects.TryGetValue(type, out object obj))
            {
                if (
                    obj is Component component
                    && component != null && !component.Equals(null) //nullチェックを行う
                    && component.transform.parent == _gameObject.transform) //親がロケーターのインスタンスか
                {
                    component.transform.SetParent(null);
                }

                _locateObjects.Remove(type);
                return true;
            }

            return false;
        }

        public T Get<T>() 
        {
            if (_locateObjects.TryGetValue(typeof(T), out object value))
            {
                return (T)value;
            }

            return default;
        }

        public object Get(Type type)
        {
            if (_locateObjects.TryGetValue(type, out object value))
            {
                return value;
            }

            return default;
        }

        public bool IsLocate(Type type)
        {
            return _locateObjects.ContainsKey(type);
        }

        public void RegisterAction<T>(Action action)
        {
            Type type = typeof(T);

            if (!_waitingActions.TryAdd(type, action))
            {
                _waitingActions[type] += action;
            }
        }

        public void RegisterAction<T>(Action<T> action)
        {
            Type type = typeof(T);

            // まだ登録されていなければ、待機リストに追加します。
            if (_waitingActionsWithInstance.TryGetValue(type, out Delegate existing))
            {
                _waitingActionsWithInstance[type] = Delegate.Combine(existing, action);
            }
            else
            {
                _waitingActionsWithInstance[type] = action;
            }
        }

        public void InvokeWaitingAction<T>(T instance)
        {
            // この型のインスタンスが登録されるのを待っていたアクションがあれば、ここで実行します。
            if (_waitingActions.TryGetValue(typeof(T), out Action waitingAction))
            {
                waitingAction?.Invoke();
                _waitingActions.Remove(typeof(T)); //実行したら解放
            }

            // 同様に、インスタンスを引数に取る待機アクションも実行します。
            if (_waitingActionsWithInstance
                .TryGetValue(typeof(T), out var del))
            {
                if (del is Action<T> action)
                {
                    action.Invoke(instance);
                }
                _waitingActionsWithInstance.Remove(typeof(T)); //実行したら解放
            }
        }

        [Tooltip("登録されているインスタンスを型をキーにして保持する辞書")]
        private readonly Dictionary<Type, object> _locateObjects = new();

        [Tooltip("インスタンス登録まで待機してから実行されるコールバックアクションを保持する辞書")]
        private readonly Dictionary<Type, Action> _waitingActions = new();
        [Tooltip("インスタンス登録まで待機し、登録されたインスタンスを引数として受け取るコールバックアクションを保持する辞書")]
        private readonly Dictionary<Type, Delegate> _waitingActionsWithInstance = new();

        private readonly GameObject _gameObject;

        public bool IsValid() =>
            Instance != null && Instance.activeInHierarchy && Instance.scene.isLoaded;
    }
}
