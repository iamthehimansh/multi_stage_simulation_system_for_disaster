using UnityEngine;
using UnityEngine.SceneManagement;

namespace FightForLife.Core
{
    /// <summary>
    /// Debug helper: pass -autoloadmap=SceneName on the command line to skip
    /// the main menu and load straight into a gameplay scene. Used to repro
    /// player crashes without manual UI clicks. Does nothing without the flag.
    /// </summary>
    public static class AutoLoadMap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnLoaded()
        {
            string target = null;
            foreach (var arg in System.Environment.GetCommandLineArgs())
            {
                if (arg != null && arg.StartsWith("-autoloadmap="))
                {
                    target = arg.Substring("-autoloadmap=".Length);
                    break;
                }
            }
            if (string.IsNullOrEmpty(target)) return;
            // Only fire from the first loaded scene (MainMenu). Otherwise we'd
            // re-enter ourselves after the target scene loads.
            if (SceneManager.GetActiveScene().name == target) return;
            Debug.Log("[AutoLoadMap] Loading scene: " + target);
            var runner = new GameObject("[AutoLoadMap]");
            Object.DontDestroyOnLoad(runner);
            runner.AddComponent<Runner>().sceneName = target;
        }

        private class Runner : MonoBehaviour
        {
            public string sceneName;
            private System.Collections.IEnumerator Start()
            {
                // Give the main menu one frame to finish its own Start().
                yield return null;
                yield return null;
                var op = SceneManager.LoadSceneAsync(sceneName);
                while (op != null && !op.isDone) yield return null;
                Destroy(gameObject);
            }
        }
    }
}
