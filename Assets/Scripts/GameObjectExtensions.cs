using System;
using UnityEngine;

public static class GameObjectExtensions
{
    public static bool HasComponent<T>(this GameObject gameObject) where T : Component
    {
        return gameObject.GetComponent<T>() != null;
    }
}