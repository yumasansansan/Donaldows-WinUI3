using System;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using Windows.Foundation;
using Windows.System;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Boot
{
    // Ports *power_sw and *bios, including the deldona=1 "Operating System
    // Notfound" branch that routes into the RPG punishment battle.
    //
    // *power_sw blanks the screen first (cls, wait 1, cls 4, wait 50, beep,
    // wait 100) so there is about a second and a half of black before the BIOS
    // logo appears; the logo then sits alone for `wait 20` before the version
    // header is printed at the top of the screen.
    //
    // The original's `beep` calls (via kernel32.as) aren't ported — they're
    // PC-speaker tones with no wav equivalent in the sound catalog.
    public sealed class BiosPostScene : IScene
    {
        private static readonly TimeSpan PowerOnBlank = TimeSpan.FromMilliseconds(1510);
        private static readonly TimeSpan LogoAt = PowerOnBlank;
        private static readonly TimeSpan HeaderAt = LogoAt + TimeSpan.FromMilliseconds(200);
        private static readonly TimeSpan NormalPostDuration = HeaderAt + TimeSpan.FromMilliseconds(2000);

        private static readonly TimeSpan SearchingAt = HeaderAt;
        private static readonly TimeSpan NotFoundListAt = SearchingAt + TimeSpan.FromMilliseconds(1000);
        private static readonly TimeSpan ErrorLineAt = NotFoundListAt + TimeSpan.FromMilliseconds(2000);
        private static readonly TimeSpan DialogsAt = ErrorLineAt + TimeSpan.FromMilliseconds(2000);

        private static readonly string[] BiosDonaldDialogs =
        {
            "BIOSドナルド「てめえ！よくもドナルドのOSを消したな！！」",
            "BIOSドナルド「洗脳が足りないようだな」",
            "BIOSドナルド「これでも喰らえ！！！！！」",
        };

        private SceneContext _context = null!;
        private TimeSpan _elapsed;
        private bool _punishment;
        private int _dialogIndex = -1;
        private bool _logoSoundPlayed;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _elapsed = TimeSpan.Zero;
            _punishment = _context.AppState.Deldona;
            _context.CloseIntercept = null; // *bios does `onexit 0`
            _dialogIndex = -1;
            _logoSoundPlayed = false;
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            session.Clear(Colors.Black);

            if (_elapsed < LogoAt)
            {
                return;
            }

            session.DrawImage(_context.Buffers.GetBitmap(BufferId.BiosLogo), 0, 0);

            if (_elapsed < HeaderAt)
            {
                return;
            }

            using var format = HspFont.Create();
            session.DrawText("DONA BIOS V0.6.5--(c)2008-2009", 0, 0, Colors.White, format);

            if (!_punishment)
            {
                return;
            }

            // The original prints the POST log from pos 0,60 downward.
            var log = "";
            if (_elapsed >= SearchingAt)
            {
                log += "Searching Operating System\n";
            }

            if (_elapsed >= NotFoundListAt)
            {
                log += "cdrom0>\tnotfound\nfd0>\t\tnotfound\nhdd0>\t\tnotfound\n" +
                       "hdd1>\t\tnotfound\nsda1>\t\tnotfound\nnetboot>\tnotfound\n";
            }

            if (_elapsed >= ErrorLineAt)
            {
                log += "Err:0x45546185>Operating System Notfound !";
            }

            session.DrawText(log, 0, 60, Colors.White, format);

            if (_dialogIndex >= 0 && _dialogIndex < BiosDonaldDialogs.Length)
            {
                session.FillRectangle(60, 200, 520, 84, Color.FromArgb(255, 60, 60, 60));
                session.DrawText(BiosDonaldDialogs[_dialogIndex], 70, 212, Colors.White, format);
                session.DrawText("[クリックまたはキーで続行]", 70, 254, Colors.Silver, format);
            }
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            _elapsed += delta;

            if (!_logoSoundPlayed && _elapsed >= LogoAt)
            {
                _logoSoundPlayed = true;
                _context.Sound.PlayEffect(SoundId.Heha);
            }

            if (!_punishment)
            {
                return _elapsed >= NormalPostDuration
                    ? new SceneTransition(_context.Save.IsInstalled ? SceneId.Kidou : SceneId.StartBoot)
                    : null;
            }

            // The original's three `dialog` calls are modal message boxes; here
            // they advance on click/keypress instead.
            if (_dialogIndex < 0 && _elapsed >= DialogsAt)
            {
                _dialogIndex = 0;
            }

            return null;
        }

        public SceneTransition? OnKeyDown(VirtualKey key)
        {
            if (_punishment)
            {
                return Advance();
            }

            // wparam 46 = VK_DELETE, 123 = VK_F12 in the original's *menuwait_key.
            if (key is VirtualKey.Delete or VirtualKey.F12)
            {
                return new SceneTransition(SceneId.BiosMenu);
            }

            return null;
        }

        public SceneTransition? OnPointerPressed(float x, float y) => _punishment ? Advance() : null;

        private SceneTransition? Advance()
        {
            if (_dialogIndex < 0)
            {
                return null;
            }

            _dialogIndex++;
            return _dialogIndex >= BiosDonaldDialogs.Length ? new SceneTransition(SceneId.RpgIntro) : null;
        }
    }
}
