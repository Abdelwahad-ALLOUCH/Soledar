using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Short ink fade for scene changes started from UI (pause → main menu).
///
/// One persistent, self-building overlay: it fades to ink, loads the destination,
/// lets it render a frame, then fades back in. It is created on demand and marked
/// DontDestroyOnLoad, so the overlay survives the scene load instead of popping.
///
/// Unscaled time is deliberate: the pause menu runs with Time.timeScale = 0 and
/// the fade must still animate. The overlay swallows pointer input while it is
/// in flight, so a stray click cannot hit the screen behind it.
/// </summary>
[DisallowMultipleComponent]
public class ScreenFader : MonoBehaviour
{
    private const int SortingOrder = 32000; // above the HUD and the pause panel

    private static ScreenFader instance;

    private Image overlay;
    private Coroutine routine;

    public static bool IsFading { get { return instance != null && instance.routine != null; } }

    /// <summary>
    /// Fades out, loads <paramref name="sceneName"/>, then fades in.
    /// Returns false (and changes nothing) when the scene is not in Build Settings.
    /// </summary>
    public static bool FadeToScene(string sceneName, float fadeOut = 0.22f, float fadeIn = 0.28f)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[ScreenFader] Scene '{sceneName}' is not in Build Settings — fade cancelled.");
            return false;
        }

        EnsureInstance().Play(sceneName, fadeOut, fadeIn);
        return true;
    }

    private static ScreenFader EnsureInstance()
    {
        if (instance != null) return instance;

        GameObject go = new GameObject("ScreenFader");
        DontDestroyOnLoad(go);

        instance = go.AddComponent<ScreenFader>();
        instance.Build();
        return instance;
    }

    private void Build()
    {
        GameObject canvasGO = new GameObject("FadeCanvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        GameObject overlayGO = new GameObject("Ink", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlayGO.transform.SetParent(canvasGO.transform, false);

        RectTransform rect = (RectTransform)overlayGO.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        overlay = overlayGO.GetComponent<Image>();
        overlay.color = new Color(SoledarUITheme.Ink.r, SoledarUITheme.Ink.g, SoledarUITheme.Ink.b, 0f);
        overlay.raycastTarget = false;
    }

    private void Play(string sceneName, float fadeOut, float fadeIn)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run(sceneName, fadeOut, fadeIn));
    }

    private IEnumerator Run(string sceneName, float fadeOut, float fadeIn)
    {
        overlay.raycastTarget = true;

        yield return FadeTo(1f, fadeOut);

        SceneManager.LoadScene(sceneName);
        yield return null; // let the destination render one frame under the ink

        yield return FadeTo(0f, fadeIn);

        overlay.raycastTarget = false;
        routine = null;
    }

    private IEnumerator FadeTo(float alpha, float duration)
    {
        float start = overlay.color.a;
        Color color = overlay.color;

        if (duration <= 0f)
        {
            color.a = alpha;
            overlay.color = color;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Unscaled: the pause menu freezes scaled time.
            elapsed += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(start, alpha, Mathf.Clamp01(elapsed / duration));
            overlay.color = color;
            yield return null;
        }

        color.a = alpha;
        overlay.color = color;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        instance = null;
    }
}
