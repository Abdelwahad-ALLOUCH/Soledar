using System.Collections.Generic;

/// <summary>
/// The player-facing names and one-line descriptions of every ability the game
/// knows about, plus which ones are part of the starting kit.
///
/// The pause menu's Abilities page lists only entries the player actually owns
/// (base kit + anything saved via SaveManager.HasAbility), so this catalog is
/// the single place to add a new ability's wording. Unknown saved ids are
/// ignored rather than shown raw, which keeps debug saves readable.
/// </summary>
public static class AbilityCatalog
{
    public const string WindOfAtlas = "windOfAtlas";
    public const string ShadowPassage = "shadowPassage";
    public const string DescentOfAsh = "descentOfAsh";

    public class Entry
    {
        public string Id;
        public string Name;
        public string Description;
        public bool AlwaysOwned;
    }

    private static readonly Entry[] entries =
    {
        new Entry
        {
            Name = "Dash",
            Description = "A quick burst forward that clears gaps and slips past strikes.",
            AlwaysOwned = true,
        },
        new Entry
        {
            Name = "Wall Jump",
            Description = "Kick off a wall to climb narrow shafts.",
            AlwaysOwned = true,
        },
        new Entry
        {
            Name = "Lantern",
            Description = "Hold Focus to spend Echo Energy and restore one mask.",
            AlwaysOwned = true,
        },
        new Entry
        {
            Id = WindOfAtlas,
            Name = "Wind of the Atlas",
            Description = "A second Jump in mid-air carries you higher.",
        },
        new Entry
        {
            Id = ShadowPassage,
            Name = "Shadow Passage",
            Description = "Walk through the Palace's sealed shadow veils.",
        },
        new Entry
        {
            Id = DescentOfAsh,
            Name = "Descent of Ash",
            Description = "Down-strike in the air to crash into the ground.",
        },
    };

    /// <summary>Entries the player currently owns, in catalog (reading) order.</summary>
    public static List<Entry> GetOwned(SaveManager save)
    {
        List<Entry> owned = new List<Entry>(entries.Length);

        for (int i = 0; i < entries.Length; i++)
        {
            Entry entry = entries[i];
            if (entry.AlwaysOwned || (save != null && save.HasAbility(entry.Id)))
                owned.Add(entry);
        }

        return owned;
    }

    /// <summary>Every catalogued entry, owned or not (used by editor tooling).</summary>
    public static IReadOnlyList<Entry> All { get { return entries; } }
}
