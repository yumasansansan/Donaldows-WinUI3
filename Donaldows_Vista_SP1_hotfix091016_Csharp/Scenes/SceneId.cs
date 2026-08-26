namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes
{
    public enum SceneId
    {
        IdleDesktop,  // *desktop/*bar/*de/*ham
        RooPopup,     // *roo
        StartMenu,    // *roomenu/*roomenuclick
        ModoRoo,      // *modoroo
        ShutdownDialog, // *shutdown
        Endona,       // *endona/*endona0 (shared confirmed-quit fade)
        AboutPopup,   // *click/*clock
        Logoff,       // *logoff
        Screensaver,  // *scr/*scred
        VirusNag,     // *virus/*v_wait/*v_roo/*ed/*cd

        BootIntro,    // buffer.hsp's opening animation
        Setumei,      // *setumei (name entry, hosted as XAML)
        BiosPost,     // *power_sw/*bios
        BiosMenu,     // *biosmenu/*bioskeycheck
        StartBoot,    // *start (loading bar + install confirm)
        InstallWizard, // *install
        Kiss,         // *kiss
        Bsod,         // *blue
        Kidou,        // *kidou

        DodgeGame,    // *game/*g_start/*gameover/*ged

        MessengerIntro,   // *messenger/*messtart
        MessengerChat,    // *messelect/*meskey/*mes1/*mes2
        MessengerOffline, // *offline
        MessengerCloseNag, // *mesclose

        CmdPrompt,  // *cmd/*cmdd/*cmdst/*cmdtype/*chantei
        Notepad,    // *notepad/*type

        RpgIntro,     // *rpg (reveal animation)
        RpgBattle,    // main battle loop: *rpgmenu/*punch/*result
        RpgGameOver,  // *rpg_gameoveroo
    }
}
