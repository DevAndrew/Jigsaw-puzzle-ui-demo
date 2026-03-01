using UnityEngine;

namespace JigsawPrototype.App
{
    public static class AutoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Object.FindFirstObjectByType<GameBootstrapper>() != null)
            {
                return;
            }

            var go = new GameObject(nameof(GameBootstrapper));
            go.AddComponent<GameBootstrapper>();
            Object.DontDestroyOnLoad(go);
        }
    }
}

