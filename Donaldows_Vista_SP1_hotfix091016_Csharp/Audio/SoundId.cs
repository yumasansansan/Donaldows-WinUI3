namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Audio
{
    // Consolidated from Donaldows_old/buffer.hsp's mmload table. HSP registered
    // separate one-shot/loop ids for the same wav (mci channel constraint); here
    // one SoundId covers both, selected via PlayEffect vs PlayBgm. Trailing
    // comment lists the original HSP id(s) for cross-referencing when porting
    // the mmplay call sites in donaldows.hsp/mes.hsp/rpg.hsp.
    public enum SoundId
    {
        Heha,           // hsp id 0, 2
        Aro,            // hsp id 1
        Jyan,           // hsp id 3
        Aree,           // hsp id 4
        Tara,           // hsp id 5
        Cd,             // hsp id 6
        Uresii,         // hsp id 7
        Urcd,           // hsp id 8
        Fu,             // hsp id 9
        Kusy,           // hsp id 10
        Welcome,        // hsp id 11
        Ran,            // hsp id 12, 121
        U,              // hsp id 13
        Ohana,          // hsp id 14
        Mosi,           // hsp id 15
        Kore,           // hsp id 16
        Dori,           // hsp id 17
        Izen,           // hsp id 18
        Kotti,          // hsp id 19
        N,              // hsp id 20
        Koremo,         // hsp id 21
        Korekaa,        // hsp id 22
        Itte,           // hsp id 24
        Magic,          // hsp id 25
        Kiken,          // hsp id 27
        Odo,            // hsp id 28
        Ur,             // hsp id 29
        Donadayo,       // hsp id 30
        Start,          // hsp id 31
        Iea,            // hsp id 32
        Login,          // hsp id 33
        Logoff,         // hsp id 34
        Gameover,       // hsp id 35
        Rurou,          // hsp id 36, 361
        GameBgm1,       // hsp id 37
        Uresiina,       // hsp id 38
        Donarudodes,    // hsp id 39
        Shutdown,       // hsp id 40
        Echoroo,        // hsp id 41
        Fii,            // hsp id 42
        AnikiA,         // hsp id 43
        AnikiDarasinee, // hsp id 44
        AnikiSumasen,   // hsp id 45
        AnikiU,         // hsp id 46
        Online,         // hsp id 47, 471
        Type,           // hsp id 48
        Motikon,        // hsp id 49
        Uac,            // hsp id 50
        GameBgm2,       // hsp id 51
        Buchu,          // hsp id 52
        Yattyau,        // hsp id 53
        Fart,           // hsp id 54
    }
}
