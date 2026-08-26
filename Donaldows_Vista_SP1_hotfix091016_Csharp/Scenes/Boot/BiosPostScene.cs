using System;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Windows.Foundation;
using Windows.System;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Boot
{
    // Ports *power_sw/*bios, including the deldona=1 "Operating System
    // Notfound" branch that routes into the RPG punishment battle after the
    // *cmd FORMAT easter egg.
    //
    // The original's `beep` calls (via kernel32.as) aren't ported — they're
    // PC-speaker tones with no wav equivalent in the sound catalog.
    public sealed class BiosPostScene : IScene
    {
        private static readonly TimeSpan LogoHold = TimeSpan.FromMilliseconds(200);
        private static readonly TimeSpan NormalPostDuration = LogoHold + TimeSpan.FromMilliseconds(2000);

        // deldona branch beats, as offsets from scene entry.
        private static readonly TimeSpan SearchingAt = LogoHold;
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

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _elapsed = TimeSpan.Zero;
            _punishment = _context.AppState.Deldona;
            _context.CloseIntercept = null; // *bios does `onexit 0`
            _dialogIndex = -1;
            _context.Sound.PlayEffect(SoundId.Heha);
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            session.Clear(Colors.Black);
            session.DrawImage(_context.Buffers.GetBitmap(BufferId.BiosLogo), 0, 0);

            using var format = new CanvasTextFormat { FontSize = 14 };
            session.DrawText("DONA BIOS V0.6.5--(c)2008-2009", 0, 40, Colors.White, format);

            if (!_punishment)
            {
                return;
            }

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
                log += "Err:0x45546185>Operating System Notfound !\n";
            }

            session.DrawText(log, new Rect(0, 60, 640, 300), Colors.White, format);

            if (_dialogIndex >= 0 && _dialogIndex < BiosDonaldDialogs.Length)
            {
                session.FillRectangle(60, 200, 520, 80, Color.FromArgb(255, 40, 40, 40));
                session.FillRectangle(60, 200, 520, 80, Color.FromArgb(40, 255, 255, 255));
                using var dialogFormat = new CanvasTextFormat { FontSize = 14 };
                session.DrawText(BiosDonaldDialogs[_dialogIndex], new Rect(70, 210, 500, 40), Colors.White, dialogFormat);
                session.DrawText("[クリックまたはキーで続行]", new Rect(70, 252, 500, 20), Colors.Gray, dialogFormat);
            }
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            _elapsed += delta;

            if (!_punishment)
            {
                return _elapsed >= NormalPostDuration
                    ? new SceneTransition(_context.Save.IsInstalled ? SceneId.Kidou : SceneId.StartBoot)
                    : null;
            }

            // The original's three `dialog` calls are modal message boxes; here
            // they advance on click/keypress instead (see Advance below).
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
