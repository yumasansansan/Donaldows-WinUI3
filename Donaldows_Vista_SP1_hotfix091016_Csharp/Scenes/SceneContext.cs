using System;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Save;
using Donaldows_Vista_SP1_hotfix091016_Csharp.State;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes
{
    // Shared services handed to every scene on Enter, replacing HSP's flat global scope.
    public sealed class SceneContext
    {
        public required SoundManager Sound { get; init; }
        public required BufferManager Buffers { get; init; }
        public required Action RequestAppExit { get; init; }

        // Ports `gsel 0,-1`, which hides the window while the app keeps
        // running — the state *virus idles in between nag popups.
        public required Action MinimizeWindow { get; init; }
        public required SaveData Save { get; init; }
        public required SaveManager SaveManager { get; init; }
        public required AppState AppState { get; init; }

        // Mutable: scenes toggle this (e.g. the dodge game while actually
        // playing) to suppress the OS cursor in favor of their own sprite.
        public bool HideCursor { get; set; }

        // Ports HSP's `onexit` hook, which the original re-points as it moves
        // between screens: cleared at *bios so the boot chain closes normally,
        // set to *virus at *de so the desktop's close button is hijacked, to
        // *rpg during the punishment battle, and to *scred in the screensaver.
        // Sticky — only scenes that correspond to an `onexit` statement in the
        // source assign it. Null means the window really closes.
        public SceneId? CloseIntercept { get; set; }
    }
}
