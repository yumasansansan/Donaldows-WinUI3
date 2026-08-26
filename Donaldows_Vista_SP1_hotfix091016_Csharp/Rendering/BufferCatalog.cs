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
            [BufferId.SneezeStamp] = "kusyami.gif",
            [BufferId.EnemySprite] = "nmini.bmp",
            [BufferId.PlayerCursorSprite] = "mg.bmp",
        };
    }
}
