using UnityEngine;

/// <summary>
/// SOLEDAR's UI design tokens, shared by the main menu, HUD and editor tooling.
///
/// Palette: ink backgrounds, ivory content, faded gold for accents and actions.
/// The gold is reserved for things the player can act on or must notice, which
/// is what keeps the hierarchy readable inside a dark game.
///
/// Type scale (Legacy Text has no custom fonts yet, so hierarchy comes from
/// size + weight + colour, not from a serif/sans pairing):
///   display 84 · title 40 · heading 22 · body 21 · label 20 · small 15
/// </summary>
public static class SoledarUITheme
{
    /* ---------------- colour ---------------- */

    public static readonly Color Ink = Hex(0x0B, 0x09, 0x10);
    public static readonly Color Panel = Hex(0x14, 0x10, 0x19);
    public static readonly Color PanelRaised = Hex(0x1D, 0x18, 0x24);
    public static readonly Color Ivory = Hex(0xEF, 0xE4, 0xCE);
    public static readonly Color Gold = Hex(0xE4, 0xC2, 0x7A);
    public static readonly Color Muted = Hex(0xA2, 0x96, 0x80);
    public static readonly Color Dim = Hex(0x6E, 0x63, 0x53);
    public static readonly Color Danger = Hex(0xC4, 0x55, 0x3D);
    public static readonly Color Rule = Hex(0x3A, 0x32, 0x46);

    /* ---------------- type ---------------- */

    public const int DisplaySize = 84;
    public const int TitleSize = 40;
    public const int HeadingSize = 22;
    public const int BodySize = 21;
    public const int LabelSize = 20;
    public const int SmallSize = 15;

    /// <summary>Gold key-cap / accent used inside prompts and hints.</summary>
    public static string GoldHex { get { return "#E4C27A"; } }

    public static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static Color Hex(int r, int g, int b)
    {
        return new Color32((byte)r, (byte)g, (byte)b, 255);
    }
}
