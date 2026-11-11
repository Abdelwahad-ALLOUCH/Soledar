using Cinemachine;
using UnityEngine;

/// <summary>
/// Tiny wrapper around a CinemachineImpulseSource so gameplay code can request
/// a camera shake without touching Cinemachine directly.
///
/// Wiring (Phase 2 setup tool):
///   - Lives on the Player GameObject next to a CinemachineImpulseSource.
///   - Main camera's CinemachineCamera needs a CinemachineImpulseListener
///     extension to actually receive the shake.
/// </summary>
[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraShake : MonoBehaviour
{
    private CinemachineImpulseSource source;

    void Awake()
    {
        source = GetComponent<CinemachineImpulseSource>();
        if (source == null)
            Debug.LogError("[CameraShake] CinemachineImpulseSource missing — shakes will be skipped.", this);
    }

    /// <summary>Fires an impulse. `force` scales the default impulse velocity.</summary>
    public void Shake(float force)
    {
        if (source == null || force <= 0f) return;
        source.GenerateImpulse(new Vector3(0f, force * 0.6f, force));
    }
}
