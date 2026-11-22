using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-off art pipeline tool: turns the presentation sheet
/// (Ziri_Animation_Sheet.png) into individual, foot-anchored animation frames.
///
/// The sheet is a rendered mockup, not an export: every frame sits on an opaque
/// gradient backdrop with a painted contact shadow and a frame number below it.
/// This tool keys the backdrop out by chroma/luma, keeps the character's largest
/// connected blob, fills enclosed holes, trims the contact shadow, and composes
/// each frame onto a fixed canvas with the feet at a shared anchor. That anchor
/// is what stops the walk cycle from bobbing sideways when the cape width changes.
///
/// Re-running is safe: the generated PNGs are overwritten and Unity reimports them.
/// Sprite import settings, pivots and clips are applied by ZiriAnimationSetup.
/// </summary>
public static class ZiriFrameExtractor
{
    const string SourcePath = "Assets/Art/Characters/Ziri/Ziri_Animation_Sheet.png";
    const string OutputFolder = "Assets/Art/Characters/Ziri/Animations";

    // Fixed frame canvas: the largest pose (idle 2, cape trailing) is ~200 px wide.
    public const int CanvasWidth = 240;
    public const int CanvasHeight = 200;
    public const int FootAnchorX = 120;
    public const int FootAnchorY = 12;

    class Blob
    {
        public readonly List<int> Pixels = new List<int>();
        public int MinX = int.MaxValue, MaxX, MinY = int.MaxValue, MaxY;

        public int Width { get { return MaxX - MinX + 1; } }
        public int Height { get { return MaxY - MinY + 1; } }
        public int CenterX { get { return (MinX + MaxX) / 2; } }
        public int CenterY { get { return (MinY + MaxY) / 2; } }
    }

    [MenuItem("Tools/Soledar/Extract Ziri Animation Frames")]
    static void ExtractFromMenu()
    {
        int count = ExtractAll();
        Debug.Log("[ZiriFrames] Extracted " + count + " frames into " + OutputFolder + ".");
    }

    /// <summary>Extracts all 22 frames. Returns the number written (0 on failure).</summary>
    public static int ExtractAll()
    {
        Texture2D sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!sheet.LoadImage(File.ReadAllBytes(SourcePath)))
        {
            Debug.LogError("[ZiriFrames] Could not read " + SourcePath);
            return 0;
        }

        int w = sheet.width, h = sheet.height;
        Color32[] px = sheet.GetPixels32();
        float[] alpha;
        bool[] mask = BuildMask(px, w, h, out alpha);

        List<Blob> blobs = FindBlobs(mask, w, h);

        // Frame numbers and labels are small; the sidebar portrait sits past x=1780.
        List<Blob> characterBlobs = new List<Blob>();
        foreach (Blob b in blobs)
        {
            if (b.Pixels.Count < 900 || b.Height < 90 || b.CenterX < 400 || b.CenterX > 1780) continue;
            characterBlobs.Add(b);
        }

        List<Blob> idle = new List<Blob>(), walk = new List<Blob>(), run = new List<Blob>();
        foreach (Blob b in characterBlobs)
        {
            if (b.CenterY > 440) idle.Add(b);
            else if (b.CenterY > 240) walk.Add(b);
            else run.Add(b);
        }
        idle.Sort((a, b) => a.MinX.CompareTo(b.MinX));
        walk.Sort((a, b) => a.MinX.CompareTo(b.MinX));
        run.Sort((a, b) => a.MinX.CompareTo(b.MinX));

        if (idle.Count != 6 || walk.Count != 8 || run.Count != 8)
        {
            Debug.LogError("[ZiriFrames] Unexpected frame count — idle " + idle.Count +
                           ", walk " + walk.Count + ", run " + run.Count +
                           ". Expected 6/8/8. Check the sheet layout or the blob filters.");
            return 0;
        }

        Directory.CreateDirectory(OutputFolder);
        List<Color32[]> canvases = new List<Color32[]>();

        int written = 0;
        written += WriteRow(idle, "Idle", px, w, alpha, canvases);
        written += WriteRow(walk, "Walk", px, w, alpha, canvases);
        written += WriteRow(run, "Run", px, w, alpha, canvases);

        AssetDatabase.Refresh();
        WriteContactSheet(canvases);
        return written;
    }

    static float[] Score(Color32[] px, int w, int h)
    {
        float[] score = new float[w * h];
        for (int i = 0; i < px.Length; i++)
        {
            Color32 c = px[i];
            float r = c.r / 255f, g = c.g / 255f, b = c.b / 255f;
            float mx = Mathf.Max(r, Mathf.Max(g, b));
            float mn = Mathf.Min(r, Mathf.Min(g, b));
            float sat = mx > 0.001f ? (mx - mn) / mx : 0f;
            float luma = 0.299f * r + 0.587f * g + 0.114f * b;
            // The backdrop is nearly neutral and dark. Purple cloth, gold trim and
            // the bone mask all clear one of these two ramps; the slate does not.
            float chroma = (sat - 0.24f) / 0.16f;
            float bright = (luma - 0.155f) / 0.12f;
            score[i] = Mathf.Clamp01(Mathf.Max(chroma, bright));
        }
        return score;
    }

    static bool[] BuildMask(Color32[] px, int w, int h, out float[] alpha)
    {
        float[] score = Score(px, w, h);
        bool[] mask = new bool[w * h];
        alpha = new float[w * h];
        for (int i = 0; i < mask.Length; i++)
        {
            mask[i] = score[i] > 0.35f;
            alpha[i] = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.1f, 0.85f, score[i]));
        }
        return mask;
    }

    static List<Blob> FindBlobs(bool[] mask, int w, int h)
    {
        List<Blob> blobs = new List<Blob>();
        bool[] seen = new bool[w * h];
        int[] stack = new int[w * h];

        for (int i = 0; i < mask.Length; i++)
        {
            if (!mask[i] || seen[i]) continue;
            Blob blob = new Blob();
            int sp = 0;
            stack[sp++] = i;
            seen[i] = true;
            while (sp > 0)
            {
                int p = stack[--sp];
                int x = p % w, y = p / w;
                blob.Pixels.Add(p);
                if (x < blob.MinX) blob.MinX = x;
                if (x > blob.MaxX) blob.MaxX = x;
                if (y < blob.MinY) blob.MinY = y;
                if (y > blob.MaxY) blob.MaxY = y;

                for (int oy = -1; oy <= 1; oy++)
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        int nx = x + ox, ny = y + oy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        int q = ny * w + nx;
                        if (mask[q] && !seen[q]) { seen[q] = true; stack[sp++] = q; }
                    }
            }
            blobs.Add(blob);
        }
        return blobs;
    }

    static int WriteRow(List<Blob> blobs, string label, Color32[] px, int sheetW,
                        float[] alpha, List<Color32[]> canvases)
    {
        int written = 0;
        for (int i = 0; i < blobs.Count; i++)
        {
            Color32[] canvas = BuildFrame(blobs[i], px, sheetW, alpha);
            if (canvas == null) continue;
            string number = (i + 1).ToString("00");
            string path = OutputFolder + "/Ziri_" + label + "_" + number + ".png";
            WritePng(path, canvas, CanvasWidth, CanvasHeight);
            canvases.Add(canvas);
            written++;
        }
        return written;
    }

    static Color32[] BuildFrame(Blob blob, Color32[] px, int sheetW, float[] alpha)
    {
        const int pad = 8;
        int x0 = Mathf.Max(0, blob.MinX - pad);
        int y0 = Mathf.Max(0, blob.MinY - pad);
        int x1 = blob.MaxX + pad;
        int y1 = blob.MaxY + pad;
        int cw = x1 - x0 + 1;
        int ch = y1 - y0 + 1;

        bool[] body = new bool[cw * ch];
        float[] a = new float[cw * ch];
        foreach (int p in blob.Pixels)
        {
            int x = p % sheetW - x0;
            int y = p / sheetW - y0;
            body[y * cw + x] = true;
            a[y * cw + x] = alpha[p];
        }

        // Dilate to close thin gaps in the dark cloak, fill what that encloses,
        // then erode back so the silhouette does not gain a dark background rim.
        bool[] solid = Dilate(body, cw, ch, 2);
        solid = FillEnclosed(solid, cw, ch);
        solid = Erode(solid, cw, ch, 2);
        TrimContactShadow(solid, a, cw, ch);

        // Local crop rows increase upward, so the lowest occupied row is the feet.
        int footRow = -1, footPixels = 0;
        float footSum = 0f;
        for (int y = 0; y < ch && footRow < 0; y++)
            for (int x = 0; x < cw; x++)
                if (solid[y * cw + x] || a[y * cw + x] > 0.5f)
                {
                    footRow = y;
                    for (int yy = y; yy <= Mathf.Min(ch - 1, y + 5); yy++)
                        for (int xx = 0; xx < cw; xx++)
                            if (solid[yy * cw + xx]) { footPixels++; footSum += xx; }
                    break;
                }

        if (footRow < 0) return null;
        float footX = footPixels > 0 ? footSum / footPixels : cw * 0.5f;
        int dx = Mathf.RoundToInt(FootAnchorX - footX);
        int dy = FootAnchorY - footRow;

        Color32[] canvas = new Color32[CanvasWidth * CanvasHeight];
        for (int y = 0; y < ch; y++)
            for (int x = 0; x < cw; x++)
            {
                int i = y * cw + x;
                float outAlpha = Mathf.Max(a[i], solid[i] ? 0.97f : 0f);
                if (outAlpha <= 0.004f) continue;
                int cx = x + dx, cy = y + dy;
                if (cx < 0 || cy < 0 || cx >= CanvasWidth || cy >= CanvasHeight) continue;
                Color32 src = px[(y0 + y) * sheetW + x0 + x];
                canvas[cy * CanvasWidth + cx] = new Color32(src.r, src.g, src.b, (byte)Mathf.RoundToInt(outAlpha * 255f));
            }
        return canvas;
    }

    /// <summary>
    /// The sheet paints a wide contact shadow under the feet. Keep only what
    /// falls inside the ankle span measured a few rows above, so the shadow's
    /// sideways smear disappears without clipping the boots.
    /// </summary>
    static void TrimContactShadow(bool[] solid, float[] alpha, int w, int h)
    {
        int bottom = -1;
        for (int y = 0; y < h && bottom < 0; y++)
            for (int x = 0; x < w; x++)
                if (solid[y * w + x]) { bottom = y; break; }
        if (bottom < 0) return;

        int refY = Mathf.Min(h - 1, bottom + 12);
        int left = int.MaxValue, right = int.MinValue;
        for (int x = 0; x < w; x++)
            if (solid[refY * w + x]) { if (x < left) left = x; if (x > right) right = x; }
        if (right < left) return;
        left -= 4;
        right += 4;

        for (int y = bottom; y <= Mathf.Min(h - 1, bottom + 8); y++)
            for (int x = 0; x < w; x++)
                if (solid[y * w + x] && (x < left || x > right))
                { solid[y * w + x] = false; alpha[y * w + x] = 0f; }
    }

    static bool[] Erode(bool[] src, int w, int h, int radius)
    {
        bool[] tmp = new bool[src.Length], dst = new bool[src.Length];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool all = true;
                for (int dx = -radius; dx <= radius && all; dx++)
                {
                    int nx = x + dx;
                    if (nx < 0 || nx >= w || !src[y * w + nx]) all = false;
                }
                tmp[y * w + x] = all;
            }
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool all = true;
                for (int dy = -radius; dy <= radius && all; dy++)
                {
                    int ny = y + dy;
                    if (ny < 0 || ny >= h || !tmp[ny * w + x]) all = false;
                }
                dst[y * w + x] = all;
            }
        return dst;
    }

    static bool[] Dilate(bool[] src, int w, int h, int radius)
    {
        bool[] tmp = new bool[src.Length];
        bool[] dst = new bool[src.Length];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (src[y * w + x])
                    for (int dx = -radius; dx <= radius; dx++)
                        tmp[y * w + Mathf.Clamp(x + dx, 0, w - 1)] = true;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (tmp[y * w + x])
                    for (int dy = -radius; dy <= radius; dy++)
                        dst[Mathf.Clamp(y + dy, 0, h - 1) * w + x] = true;
        return dst;
    }

    /// <summary>Everything not reachable from the crop border is inside the character.</summary>
    static bool[] FillEnclosed(bool[] mask, int w, int h)
    {
        bool[] outside = new bool[mask.Length];
        int[] stack = new int[mask.Length];
        int sp = 0;
        for (int x = 0; x < w; x++)
        {
            if (!mask[x]) { outside[x] = true; stack[sp++] = x; }
            int b = (h - 1) * w + x;
            if (!mask[b]) { outside[b] = true; stack[sp++] = b; }
        }
        for (int y = 0; y < h; y++)
        {
            int l = y * w;
            if (!mask[l]) { outside[l] = true; stack[sp++] = l; }
            int r = y * w + w - 1;
            if (!mask[r]) { outside[r] = true; stack[sp++] = r; }
        }
        while (sp > 0)
        {
            int p = stack[--sp];
            int x = p % w, y = p / w;
            for (int oy = -1; oy <= 1; oy++)
                for (int ox = -1; ox <= 1; ox++)
                {
                    int nx = x + ox, ny = y + oy;
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                    int q = ny * w + nx;
                    if (!mask[q] && !outside[q]) { outside[q] = true; stack[sp++] = q; }
                }
        }
        bool[] filled = new bool[mask.Length];
        for (int i = 0; i < mask.Length; i++) filled[i] = mask[i] || !outside[i];
        return filled;
    }

    static void WritePng(string path, Color32[] pixels, int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    /// <summary>Contact sheet in Temp/ (not an asset) for eyeballing the extraction.</summary>
    static void WriteContactSheet(List<Color32[]> frames)
    {
        int perRow = 8;
        int rows = 3;
        int w = CanvasWidth * perRow, h = CanvasHeight * rows;
        Texture2D sheet = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32[] outp = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                outp[y * w + x] = ((x / 16 + y / 16) % 2 == 0)
                    ? new Color32(96, 104, 80, 255)
                    : new Color32(70, 78, 60, 255);
        for (int i = 0; i < frames.Count; i++)
        {
            int col = i % perRow, row = i / perRow;
            for (int y = 0; y < CanvasHeight; y++)
                for (int x = 0; x < CanvasWidth; x++)
                {
                    Color32 c = frames[i][y * CanvasWidth + x];
                    if (c.a == 0) continue;
                    int dst = (row * CanvasHeight + y) * w + col * CanvasWidth + x;
                    Color bg = outp[dst];
                    float a = c.a / 255f;
                    outp[dst] = new Color32(
                        (byte)Mathf.RoundToInt(c.r * a + bg.r * (1 - a)),
                        (byte)Mathf.RoundToInt(c.g * a + bg.g * (1 - a)),
                        (byte)Mathf.RoundToInt(c.b * a + bg.b * (1 - a)), 255);
                }
        }
        sheet.SetPixels32(outp);
        sheet.Apply();
        Directory.CreateDirectory("Temp");
        File.WriteAllBytes("Temp/ziri_frames_contact.png", sheet.EncodeToPNG());
        Object.DestroyImmediate(sheet);
        Debug.Log("[ZiriFrames] Contact sheet written to Temp/ziri_frames_contact.png");
    }
}
