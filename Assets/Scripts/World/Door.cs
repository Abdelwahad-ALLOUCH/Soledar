using System.Collections;
using UnityEngine;

/// <summary>
/// A gate that blocks passage until unlocked. Placed on the "Ground" layer so
/// the player, enemies and wall probes all treat it as level geometry.
///
/// Unlocking happens through a CollectibleKey reference (Phase 3 groundwork;
/// real key inventory + multiple locks in Phase 4). A gate with no key stays
/// sealed — used for the region exit teaser.
///
/// Lives on the door GameObject.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class Door : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string doorName = "Medina Gate";
    [SerializeField] private bool startLocked = true;

    [Header("Open Feedback")]
    [SerializeField] private float fadeDuration = 0.5f;

    public bool IsOpen { get; private set; }
    public string DoorName { get { return doorName; } }

    private Collider2D col;
    private SpriteRenderer sprite;
    private bool fading;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();

        if (!startLocked && !IsOpen)
            Open();
    }

    /// <summary>Called by a CollectibleKey when it is picked up.</summary>
    public void Unlock(string keyName)
    {
        if (IsOpen)
        {
            Debug.Log($"[Door] '{doorName}' was already open (key '{keyName}' used again?).", this);
            return;
        }

        Debug.Log($"[Door] '{doorName}' unlocked with '{keyName}'.", this);
        Open();
    }

    private void Open()
    {
        IsOpen = true;

        if (col != null)
            col.enabled = false;

        if (sprite != null && fadeDuration > 0f)
            StartCoroutine(FadeOut());
        else if (sprite != null)
            sprite.enabled = false;
    }

    private IEnumerator FadeOut()
    {
        if (fading) yield break;
        fading = true;

        Color start = sprite.color;
        Color end = new Color(start.r, start.g, start.b, 0f);
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            sprite.color = Color.Lerp(start, end, Mathf.Clamp01(t / fadeDuration));
            yield return null;
        }

        sprite.enabled = false;
    }
}
