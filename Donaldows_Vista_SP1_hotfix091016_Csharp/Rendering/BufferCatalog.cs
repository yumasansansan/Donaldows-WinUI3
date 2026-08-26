using System.Collections.Generic;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering
{
    public static class BufferCatalog
    {
        public static readonly IReadOnlyDictionary<BufferId, string> FileNames = new Dictionary<BufferId, string>
        {
            [BufferId.DesktopBackground] = "logo.bmp",
            [BufferId.TaskbarIcon] = "hamico.gif",
            [BufferId.DonaFace] = "dona2.gif",
            [BufferId.MascotSprite] = "ranran.bmp",
            [BufferId.BiosLogo] = "b-logo.bmp",
            [BufferId.BsodImage] = "ban.bmp",
            [BufferId.DonaldNormal] = "1.jpg",
            [BufferId.DonaldEyesShine] = "2.jpg",
            [BufferId.DonaFrontFace] = "dona1.gif",
            [BufferId.HddStatus] = "hdd.gif",
            [BufferId.IeaGag] = "iea.bmp",
            [BufferId.AnikiGag] = "aniki.bmp",
            [BufferId.InstallBackdrop] = "install.bmp",
            [BufferId.InstallHeader] = "hapset.bmp",
            [BufferId.MascotSmall] = "ranran2.bmp",
            [BufferId.FireScroll] = "cloud.gif",
            [BufferId.LoveZoom] = "love.bmp",
            [BufferId.ItDonald] = "it.jpg",
            [BufferId.Wait1] = "anime/WAIT1.BMP",
            [BufferId.Wait2] = "anime/WAIT2.BMP",
            [BufferId.Wait3] = "anime/WAIT3.BMP",
            [BufferId.Sp1] = "anime/SP1.BMP",
            [BufferId.Sp2] = "anime/SP2.BMP",
            [BufferId.Sp3] = "anime/SP3.BMP",
            [BufferId.Sp4] = "anime/SP4.BMP",
            [BufferId.Sp5] = "anime/SP5.BMP",
            [BufferId.Sp6] = "anime/SP6.BMP",
            [BufferId.Sp7] = "anime/SP7.BMP",
            [BufferId.Sp8] = "anime/SP8.BMP",
            [BufferId.SneezeStamp] = "kusyami.gif",
            [BufferId.EnemySprite] = "nmini.bmp",
            [BufferId.PlayerCursorSprite] = "mg.bmp",
        };
    }
}
