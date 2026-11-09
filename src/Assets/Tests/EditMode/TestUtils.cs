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
        MethodInfo awake = behaviour.GetType().GetMethod(
            "Awake", BindingFlags.Instance | BindingFlags.NonPublic);

        awake?.Invoke(behaviour, null);
    }
}
