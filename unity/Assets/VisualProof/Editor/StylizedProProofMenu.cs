using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ShiftHappens.VisualProof.Editor
{
    public static class StylizedProProofMenu
    {
        private const string ScenePath = "Assets/VisualProof/StylizedProProof.unity";

        [MenuItem("Tools/Shift Happens/Watch Stylized Pro Proof", true)]
        private static bool CanOpen() => !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem("Tools/Shift Happens/Watch Stylized Pro Proof")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath))
                EditorSceneManager.OpenScene(ScenePath);
            else
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("Stylized Pro visual proof", typeof(StylizedProProof));
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) return;
            }
            EditorApplication.EnterPlaymode();
        }
    }
}
