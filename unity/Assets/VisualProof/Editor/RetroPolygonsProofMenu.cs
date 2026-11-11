using UnityEditor;
using UnityEditor.SceneManagement;

namespace ShiftHappens.VisualProof.Editor
{
    public static class RetroPolygonsProofMenu
    {
        [MenuItem("Tools/Shift Happens/Watch Retro Polygons Crowded Handoff", true)]
        private static bool CanOpen() => !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem("Tools/Shift Happens/Watch Retro Polygons Crowded Handoff")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/VisualProof/RetroPolygonsProof.unity");
            EditorApplication.EnterPlaymode();
        }
    }
}
