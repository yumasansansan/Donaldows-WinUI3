namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering
{
    // Mirrors the bitmap assets buffer.hsp preloaded into numbered `buffer` slots,
    // named by meaning rather than by HSP's raw slot number. Only entries needed
    // by scenes ported so far are listed here; extend as later phases need more.
    public enum BufferId
    {
        DesktopBackground, // buffer 20, img/logo.bmp
        TaskbarIcon,       // drawn ad hoc in *bar, img/hamico.gif
        DonaFace,          // drawn ad hoc in *clock/*shutdown, img/dona2.gif
        MascotSprite,      // buffer 11, img/ranran.bmp (start-menu character art)
        BiosLogo,          // img/b-logo.bmp, drawn ad hoc in *bios
        BsodImage,         // img/ban.bmp, drawn ad hoc in *blue
        DonaldNormal,      // buffer 14, img/1.jpg
        DonaldEyesShine,   // buffer 15, img/2.jpg
        DonaFrontFace,     // img/dona1.gif, drawn ad hoc in *start
        HddStatus,         // img/hdd.gif, drawn ad hoc in *install
        IeaGag,            // img/iea.bmp, *roo iea=3 gag
        AnikiGag,          // img/aniki.bmp, *roo iea=10/20 "big bro" gag
        InstallBackdrop,   // img/install.bmp, *start install-confirm background
        InstallHeader,     // img/hapset.bmp, *install header art
        MascotSmall,       // img/ranran2.bmp, *install completion scatter
        FireScroll,        // buffer 21, img/cloud.gif — cloud texture reused as the RPG fire background
        LoveZoom,          // buffer 10, img/love.bmp, *install red zoom transition
        ItDonald,          // buffer 16, img/it.jpg, *cd prank fullscreen zoom
        SneezeStamp,       // buffer 22, img/kusyami.gif
        EnemySprite,       // buffer 17, img/nmini.bmp (dodge-game "mini Donald" enemy)
        PlayerCursorSprite, // buffer 18, img/mg.bmp (dodge-game player cursor)

        // Solid-fill buffers (buffer.hsp lines 86-94) — resolved via SolidColorCatalog, not BufferCatalog.
        TaskbarBackdrop,     // buffer 12, color 0,20,20
        MenuRowBackdrop,     // buffer 8, color 0,32,64 (DARK_BLUE)
        MenuHoverHighlight,  // buffer 1, color 0,255,200 (BIOS)
        Black,               // buffer 5
        Orange,              // buffer 7, color 255,50,0
    }
}
