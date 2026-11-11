using UnityEngine;

/// <summary>
/// A single conversation for an NPC (Phase 2: plain lines; Phase 4 adds
/// quest triggers and choices on top of this).
/// Create via: Assets > Create > Soledar > Dialogue > Dialogue Data
/// </summary>
[CreateAssetMenu(fileName = "NewDialogue", menuName = "Soledar/Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Tooltip("Shown in the speaker label (e.g. The Old Cartographer).")]
    public string speakerName = "???";

    [TextArea(2, 4)]
    public string[] lines = new string[0];
}
