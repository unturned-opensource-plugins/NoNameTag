using System;
using System.Collections.Concurrent;
using UnityEngine;
using Logger = Emqo.NoNameTag.Utilities.PluginLogger;

namespace Emqo.NoNameTag.Utilities
{
    /// <summary>
    /// Unity 主线程调度器
    /// 用于在后台线程（如 Timer）中安全地执行主线程操作
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher _instance;
        private static readonly ConcurrentQueue<Action> _actionQueue = new ConcurrentQueue<Action>();

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static UnityMainThreadDispatcher Instance()
        {
            if (_instance == null)
            {
                // 查找现有的实例
                _instance = FindObjectOfType<UnityMainThreadDispatcher>();

                if (_instance == null)
                {
                    // 创建新的游戏对象
                    var dispatcherObject = new GameObject("NoNameTag_MainThreadDispatcher");
                    _instance = dispatcherObject.AddComponent<UnityMainThreadDispatcher>();
                    DontDestroyOnLoad(dispatcherObject);
                }
            }

            return _instance;
        }

        /// <summary>
        /// 将动作加入队列，在主线程执行
        /// </summary>
        public void Enqueue(Action action)
        {
            if (action == null) return;
            _actionQueue.Enqueue(action);
        }

        /// <summary>
        /// Unity Update 方法，在主线程每帧执行
        /// </summary>
        private void Update()
        {
            // 执行队列中的所有动作
            while (_actionQueue.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex, "Error executing action in main thread dispatcher", LogCategory.Plugin);
                }
            }
        }

        /// <summary>
        /// 清理队列
        /// </summary>
        public void ClearQueue()
        {
            while (_actionQueue.TryDequeue(out _))
            {
                // 清空队列
            }
        }

        /// <summary>
        /// 销毁时清理
        /// </summary>
        private void OnDestroy()
        {
            ClearQueue();
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
