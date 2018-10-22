using System;
using System.Collections.Generic;

public class Missive
{
    // Add named and typeless listener with callback
    public static void AddListener(string missiveName, Action callback)
    {
        RegisterListener(missiveName, callback);
    }

    // Add typed listener with callback
    public static void AddListener<T>(Action<T> callback) where T : Missive
    {
        RegisterListener(typeof(T).ToString(), callback);
    }

    // Add named and typed listener with callback
    public static void AddListener<T>(string missiveName, Action<T> callback) where T : Missive
    {
        RegisterListener(typeof(T).ToString() + missiveName, callback);
    }

    // Remove named and typeless listener with callback
    public static void RemoveListener(string missiveName, Action callback)
    {
        UnregisterListener(missiveName, callback);
    }

    // Remove typed listener with callback
    public static void RemoveListener<T>(Action<T> callback) where T : Missive
    {
        UnregisterListener(typeof(T).ToString(), callback);
    }

    // Remove named and typed listener with callback
    public static void RemoveListener<T>(string missiveName, Action<T> callback) where T : Missive
    {
        UnregisterListener(typeof(T).ToString() + missiveName, callback);
    }

    // Send named and typeless missive
    public static void Send(string missiveName)
    {
        SendMissive<Missive>(missiveName, null);
    }

    // Send typed missive
    public static void Send<T>(T missive) where T : Missive
    {
        SendMissive<T>(typeof(T).ToString(), missive);
    }

    // Send named and typed missive
    public static void Send<T>(string missiveName, T missive) where T : Missive
    {
        SendMissive<T>(typeof(T).ToString() + missiveName, missive);
    }

    private static Dictionary<string, List<Delegate>> handlers = new Dictionary<string, List<Delegate>>();

    private static void RegisterListener(string messageName, Delegate callback)
    {
        if (string.IsNullOrEmpty(messageName)) return;
        if (callback == null) return;

        if (!handlers.ContainsKey(messageName))
        {
            handlers.Add(messageName, new List<Delegate>());
        }
        List<Delegate> messageHandlers = handlers[messageName];
        messageHandlers.Add(callback);
    }

    private static void UnregisterListener(string messageName, Delegate callback)
    {
        if (string.IsNullOrEmpty(messageName)) return;
        if (callback == null) return;
        if (!handlers.ContainsKey(messageName)) return;

        List<Delegate> missiveHandlers = handlers[messageName];
        Delegate messageHandler = missiveHandlers.Find(x => x.Method == callback.Method && x.Target == callback.Target);
        if (messageHandler == null) return;

        missiveHandlers.Remove(messageHandler);
    }

    private static void SendMissive<T>(string missiveName, T missive) where T : Missive
    {
        if (!handlers.ContainsKey(missiveName)) return;

        List<Delegate> missiveHandlers = handlers[missiveName];
        for (int i = 0; i < missiveHandlers.Count; i++)
        {
            if (missiveHandlers[i].GetType() != typeof(Action<T>) && missiveHandlers[i].GetType() != typeof(Action)) continue;

            if (typeof(T) == typeof(Missive))
            {
                Action action = (Action)missiveHandlers[i];
                action();
            }
            else
            {
                Action<T> action = (Action<T>)missiveHandlers[i];
                action(missive);
            }
        }
    }

    protected Missive() { }
}