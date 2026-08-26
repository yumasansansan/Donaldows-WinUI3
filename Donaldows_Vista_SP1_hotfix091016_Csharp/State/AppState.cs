namespace Donaldows_Vista_SP1_hotfix091016_Csharp.State
{
    // Session-lifetime state shared across scenes but never persisted to disk
    // (the original's equivalents are bare HSP globals that reset each run).
    public sealed class AppState
    {
        // Ports `deldona`: set when the *cmd FORMAT easter egg completes,
        // read by BiosPostScene to route into the RPG punishment battle
        // instead of the normal boot chain. Reset to false by RpgIntroScene,
        // matching the original's `deldona=0` at the top of *rpg.
        public bool Deldona { get; set; }

        // Ports `iea`: counts how many times the start button has been
        // clicked this session, driving *roo's escalating gags at 3/10/20.
        public int PopupCount { get; set; }

        // Ports `kis`: the *kiss escalation counter, shared across every
        // path that leads there (install complete, messenger accept, RPG
        // "give in").
        public int KissCount { get; set; }
    }
}
