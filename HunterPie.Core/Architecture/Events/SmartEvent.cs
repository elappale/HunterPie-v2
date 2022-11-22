﻿using HunterPie.Core.Architecture.Collections;
using HunterPie.Core.Logger;
using System;
using System.Reflection;

namespace HunterPie.Core.Architecture.Events;

#nullable enable
/// <summary>
/// SmartEvent is an wrapper for Events that works in a better way, detecting subscriber leaks and handling errors on invoke
/// without crashing the whole application.
/// </summary>
/// <typeparam name="TSource">Type of class that's going to dispatch the event</typeparam>
/// <typeparam name="TEventArgs">Type of event arg</typeparam>
public class SmartEvent<TSource, TEventArgs> : ISmartEvent
    where TSource : class
    // TODO: Make TEventArgs : EventArgs 
{
    private readonly object _sync = new();
    private EventHandler<TEventArgs>? _event;

    public ThreadSafeObservableCollection<MethodInfo> References { get; } = new();

    public SmartEvent()
    {
        SmartEventsTracker.Track(this);
    }

    public void Hook(EventHandler<TEventArgs> handler)
    {
        lock (_sync)
        {
            _event += handler;
            References.Add(handler.Method);
        }
    }

    public void Unhook(EventHandler<TEventArgs> handler)
    {
        lock (_sync)
        {
            _event -= handler;
            References.Remove(handler.Method);
        }
    }

    public void Invoke(object? sender, TEventArgs e)
    {
        lock (_sync)
        {
            if (_event is null)
                return;

            foreach (EventHandler<TEventArgs> sub in _event.GetInvocationList())
                try
                {
                    sub(sender, e);
                }
                catch (Exception err)
                {
                    Log.Error("Exception in {0}: {1}", sub.Method.Name, err);
                }
        }
    }

    public void Dispose()
    {
        SmartEventsTracker.Untrack(this);
        GC.SuppressFinalize(this);
    }
}

public class SmartEvent<TEventArgs> : SmartEvent<object, TEventArgs> { }