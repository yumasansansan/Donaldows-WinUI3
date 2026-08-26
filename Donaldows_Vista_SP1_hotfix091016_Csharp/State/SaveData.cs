namespace Donaldows_Vista_SP1_hotfix091016_Csharp.State
{
    // Replaces the original's three note-buffer text files (save/name.txt,
    // save/install.txt, save/game.txt) with one typed, JSON-persisted model.
    public sealed class SaveData
    {
        public string PlayerName { get; set; } = "";
        public bool IsInstalled { get; set; }

        // Deliberate behavior change from the original's noteadd-prepend quirk
        // (which displayed the most recently added score, not the max) — this
        // keeps the numeric maximum across plays, per the approved plan.
        public int HighScore { get; set; }
    }
}
