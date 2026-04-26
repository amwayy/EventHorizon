using System;
using System.Collections.Generic;

public static class ReferencePool
{
    private static readonly Dictionary<Type, Stack<IReference>> Pool = new ();

    public static T Acquire<T>() where T : class, IReference, new()
    {
        var type = typeof(T);

        if (Pool.TryGetValue(type, out var stack) && stack.Count > 0)
        {
            return (T)stack.Pop();
        }

        return new T();
    }

    public static void Release(IReference reference)
    {
        if (reference == null)
            return;

        reference.Clear();

        var type = reference.GetType();
        if (!Pool.TryGetValue(type, out var stack))
        {
            stack = new Stack<IReference>();
            Pool.Add(type, stack);
        }

        stack.Push(reference);
    }

    public static void ClearAll()
    {
        Pool.Clear();
    }
}