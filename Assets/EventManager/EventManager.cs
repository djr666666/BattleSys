using System;
using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────
// 这是一套独立的通用事件系统(不属于战斗框架),来自作者自己的开源库:
//   简单好用的事件系统 → https://github.com/djr666666/EventListeningSystem
// 战斗框架只是"使用者":逻辑发 SendMessage、UI 监听 AddEvent。
// 单独放一个体(Assets/EventManager),和战斗解耦,别的项目也能直接拿去用。
// ─────────────────────────────────────────────────────────────

/// <summary>
/// 全局事件总线:用"事件类型"当 key,谁关心谁订阅,发的时候按类型找到所有订阅者依次回调。
/// 作用:把"发事件的人"和"处理的人"解耦——战斗逻辑只管 SendMessage,UI/表现自己 AddEvent 监听。
/// 用法:AddEvent<T>(处理函数) 订阅;SendMessage(new T{...}) 发送;Remove<T>(处理函数) 取消。
/// </summary>
public class EventManager
{
    // key = 事件类型的哈希;value = 该类型的所有订阅者(装成 object,用时转回 Action<T>)
    public static readonly Dictionary<int, List<object>> _listeners = new Dictionary<int, List<object>>();

    /// <summary>订阅某类事件。</summary>
    public static void AddEvent<T>(Action<T> handle)
    {
        int eventId = typeof(T).GetHashCode();
        if (!_listeners.ContainsKey(eventId))
            _listeners.Add(eventId, new List<object>());

        if (!_listeners[eventId].Contains(handle))
            _listeners[eventId].Add(handle);
        else
            Debug.LogWarning("该事件已经注册过:" + handle.GetType().FullName);
    }

    /// <summary>发送事件(带数据)。</summary>
    public static void SendMessage<T>(T obj)
    {
        int eventId = typeof(T).GetHashCode();
        if (_listeners.ContainsKey(eventId))
        {
            for (int i = 0; i < _listeners[eventId].Count; i++)
            {
                Action<T> action = _listeners[eventId][i] as Action<T>;
                if (action != null) action(obj);
            }
        }
    }

    /// <summary>发送事件(无数据,用默认值)。</summary>
    public static void SendMessage<T>()
    {
        int eventId = typeof(T).GetHashCode();
        if (_listeners.ContainsKey(eventId))
        {
            for (int i = 0; i < _listeners[eventId].Count; i++)
            {
                Action<T> action = _listeners[eventId][i] as Action<T>;
                if (action != null) action(default(T));
            }
        }
    }

    /// <summary>取消订阅。</summary>
    public static void Remove<T>(Action<T> handle)
    {
        int eventId = typeof(T).GetHashCode();
        if (_listeners.ContainsKey(eventId))
            _listeners[eventId].Remove(handle);
    }

    /// <summary>清空所有订阅(战斗结束/切场景时用,防止残留订阅)。</summary>
    public static void RemoveAll() => _listeners.Clear();
}