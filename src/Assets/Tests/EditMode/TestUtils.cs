using System.Reflection;
using UnityEngine;

/// <summary>
/// EditMode tests don't enter Play Mode, so Unity never calls Awake/Start on
/// plain MonoBehaviours. These helpers invoke those lifecycle methods
/// directly via reflection so singleton setup (e.g. GameManager.Instance)
/// behaves the same way it would at runtime.
/// </summary>
public static class TestUtils
{
    public static void InvokeAwake(MonoBehaviour behaviour)
    {
        InvokeLifecycleMethod(behaviour, "Awake");
    }

    public static void InvokeStart(MonoBehaviour behaviour)
    {
        InvokeLifecycleMethod(behaviour, "Start");
    }

    private static void InvokeLifecycleMethod(MonoBehaviour behaviour, string methodName)
    {
        MethodInfo method = behaviour.GetType().GetMethod(
            methodName, BindingFlags.Instance | BindingFlags.NonPublic);

        method?.Invoke(behaviour, null);
    }
}
