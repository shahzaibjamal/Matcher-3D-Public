using System;
using System.Reflection;
using UnityEngine;

public static class EventDebugger
{
    /// <summary>
    /// Prints all subscribers, their class types, and the specific methods listening to a delegate.
    /// </summary>
    /// <param name="targetDelegate">The event/delegate to inspect.</param>
    /// <param name="eventName">A label for the console log (e.g., "OnGoldUpdated").</param>
    public static void LogSubscribers(Delegate targetDelegate, string eventName = "Unknown Event")
    {
        if (targetDelegate == null)
        {
            Debug.Log($"<color=white>[{eventName}]</color> <color=grey>No active subscribers.</color>");
            return;
        }

        Delegate[] invocationList = targetDelegate.GetInvocationList();

        Debug.Log($"<color=cyan><b>[{eventName}]</b></color> Found <b>{invocationList.Length}</b> subscribers:");

        for (int i = 0; i < invocationList.Length; i++)
        {
            Delegate del = invocationList[i];

            // The object instance that owns the method
            string targetName = del.Target != null ? del.Target.ToString() : "<i>Static Class</i>";

            // The name of the function
            string methodName = del.Method.Name;

            // Check if the target is a Unity Object that has been destroyed (The 'Ghost' Subscriber)
            string status = "";
            if (del.Target is UnityEngine.Object obj && obj == null)
            {
                status = " <color=red>[ALIVE REFERENCE TO DESTROYED OBJECT!]</color>";
            }

            Debug.Log($"   {i + 1}. <b>{targetName}</b> → <color=yellow>{methodName}()</color>{status}");
        }
    }
}