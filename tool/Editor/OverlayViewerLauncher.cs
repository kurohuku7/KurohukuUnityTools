using UnityEngine;
using UnityEditor;
using System.Diagnostics;

/// <summary>
/// Restart overlay viewer when Play mode button is pushed.
/// SteamVR overlay viewer causes an error when launch. It needs to restart process so auto restart when play mode is toggled.
/// </summary>
[InitializeOnLoadAttribute]
public class OverlayViewerLauncher : MonoBehaviour
{
    private static Process viewer = null;

    static OverlayViewerLauncher()
    {
        EditorApplication.playModeStateChanged += (state) =>
        {
            // Set from kurohuku tool
            if (!EditorPrefs.GetBool("launchOverlayViewer")) return;

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                viewer = Process.Start(EditorPrefs.GetString("overlayViewerPath"));
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                viewer?.CloseMainWindow();
            }
        };
    }
}
