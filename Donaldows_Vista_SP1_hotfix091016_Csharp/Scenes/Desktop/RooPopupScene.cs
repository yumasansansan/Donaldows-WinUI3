using System;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Windows.Foundation;
using Microsoft.UI;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Desktop
{
    // Ports *roo: the taskbar-corner pop-up the mascot does when the start
    // button is clicked, including the escalating gags at the 3rd, 10th, and
    // 20th click this session (`iea`, tracked in AppState.PopupCount so it
    // persists across repeated menu opens).
    //
    // Beat timings follow HSP's 10ms `wait` unit — the aniki gag's voice-line
    // pauses are 2s/3s/2s, not the 200/300/200ms an earlier version used.
    // The zoom/fade flourish that follows the plain rise is not reproduced, so
    // its trailing 1.4s hold is compressed rather than left as a dead pause.
    public sealed class RooPopupScene : IScene
    {
        private enum GagKind { None, Iea, AnikiUp, AnikiJumpscare }

        private static readonly TimeSpan RiseDuration = TimeSpan.FromMilliseconds(150);
        private static readonly TimeSpan RevealDuration = TimeSpan.FromMilliseconds(600);
        private static readonly TimeSpan GagSlideDuration = TimeSpan.FromMilliseconds(250);

        private SceneContext _context = null!;
        private GagKind _gag;
        private int _beat;
        private TimeSpan _elapsed;
        private bool _revealing;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _context.AppState.PopupCount++;
            var iea = _context.AppState.PopupCount;

            _gag = iea switch
            {
                3 => GagKind.Iea,
                10 => GagKind.AnikiUp,
                20 => GagKind.AnikiJumpscare,
                _ => GagKind.None,
            };

            _beat = 0;
            _elapsed = TimeSpan.Zero;
            _revealing = _gag == GagKind.None;

            if (_revealing)
            {
                _context.Sound.PlayEffect(SoundId.Echoroo);
            }
            else if (_gag == GagKind.Iea)
            {
                _context.Sound.PlayEffect(SoundId.Iea);
            }
            else
            {
                _context.Sound.PlayEffect(SoundId.AnikiA);
            }
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            session.Clear(Colors.Black);
            session.DrawImage(_context.Buffers.GetBitmap(BufferId.DesktopBackground), new Rect(0, 0, 640, 480));

            if (_revealing)
            {
                var progress = Math.Clamp((float)(_elapsed / RiseDuration), 0f, 1f);
                var y = 480f - progress * 260f; // slides up to the menu's resting position (a=220)
                var height = 480f - y;
                session.DrawImage(_context.Buffers.GetBitmap(BufferId.MascotSprite), new Rect(0, y, 182, height), new Rect(0, 0, 182, height));
                return;
            }

            var image = _gag == GagKind.Iea ? BufferId.IeaGag : BufferId.AnikiGag;
            session.DrawImage(_context.Buffers.GetBitmap(image), 0, GagImageY());

            if (_gag == GagKind.AnikiJumpscare && _beat >= 2)
            {
                var fadeProgress = Math.Clamp((float)(_elapsed / TimeSpan.FromSeconds(1)), 0f, 1f);
                session.FillRectangle(0, 0, 640, 480, Color.FromArgb((byte)(fadeProgress * 255), 255, 50, 0));
            }
        }

        private float GagImageY()
        {
            var slideUp = Math.Clamp((float)(_elapsed / GagSlideDuration), 0f, 1f);

            if (_gag == GagKind.AnikiUp && _beat == 4)
            {
                // Second slide continues from the held mid-screen position up and off the top.
                return 100f - slideUp * 860f;
            }

            if (_beat == 0)
            {
                return 480f - slideUp * 380f; // 480 -> ~100
            }

            return 100f; // held between beats 1-3
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            _elapsed += delta;

            if (_revealing)
            {
                return _elapsed >= RevealDuration ? new SceneTransition(SceneId.StartMenu) : null;
            }

            return _gag switch
            {
                GagKind.Iea => UpdateIea(),
                GagKind.AnikiUp => UpdateAnikiUp(),
                GagKind.AnikiJumpscare => UpdateAnikiJumpscare(),
                _ => null,
            };
        }

        private SceneTransition? UpdateIea()
        {
            if (_elapsed < GagSlideDuration)
            {
                return null;
            }

            BeginReveal();
            return null;
        }

        private static readonly TimeSpan[] AnikiUpBeatDurations =
        {
            GagSlideDuration,                // 0: slide up
            TimeSpan.FromMilliseconds(2000), // 1: hold, then aniki_darasinee
            TimeSpan.FromMilliseconds(3000), // 2: hold, then aniki_sumasen
            TimeSpan.FromMilliseconds(2000), // 3: hold, then aniki_u
            GagSlideDuration,                // 4: slide up and off
        };

        private SceneTransition? UpdateAnikiUp()
        {
            if (_elapsed < AnikiUpBeatDurations[_beat])
            {
                return null;
            }

            _elapsed = TimeSpan.Zero;
            _beat++;

            switch (_beat)
            {
                case 1:
                    _context.Sound.PlayEffect(SoundId.AnikiDarasinee);
                    break;
                case 2:
                    _context.Sound.PlayEffect(SoundId.AnikiSumasen);
                    break;
                case 3:
                    _context.Sound.PlayEffect(SoundId.AnikiU);
                    break;
                case 5:
                    BeginReveal();
                    break;
            }

            return null;
        }

        private SceneTransition? UpdateAnikiJumpscare()
        {
            switch (_beat)
            {
                case 0 when _elapsed >= GagSlideDuration:
                    _beat = 1;
                    _elapsed = TimeSpan.Zero;
                    return null;

                case 1 when _elapsed >= TimeSpan.FromMilliseconds(2000):
                    _beat = 2;
                    _elapsed = TimeSpan.Zero;
                    _context.Sound.PlayEffect(SoundId.Fart);
                    return null;

                case 2 when _elapsed >= TimeSpan.FromMilliseconds(2000):
                    _context.Sound.StopAll();
                    return new SceneTransition(SceneId.Bsod);

                default:
                    return null;
            }
        }

        private void BeginReveal()
        {
            _revealing = true;
            _elapsed = TimeSpan.Zero;
            _context.Sound.PlayEffect(SoundId.Echoroo);
        }
    }
}
