using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Pont C# ↔ JavaScript pour les fonctionnalités WebGL mobile.
/// </summary>
public static class WebGLHelper
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern int JS_IsMobileDevice();
    [DllImport("__Internal")] private static extern void JS_RequestFullscreen();
    [DllImport("__Internal")] private static extern void JS_SetupMobileViewport();
#endif

    /// <summary>Retourne true si l'appareil est mobile/tactile</summary>
    public static bool IsMobileDevice()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return JS_IsMobileDevice() == 1;
#else
        return Application.isMobilePlatform;
#endif
    }

    /// <summary>Demande le plein écran (nécessite d'être appelé depuis un geste utilisateur)</summary>
    public static void RequestFullscreen()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        JS_RequestFullscreen();
#else
        Screen.fullScreen = true;
#endif
    }

    /// <summary>
    /// Configure le viewport mobile et installe le listener plein écran au premier toucher.
    /// À appeler une seule fois au démarrage.
    /// </summary>
    public static void SetupMobileViewport()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        JS_SetupMobileViewport();
#endif
    }
}
