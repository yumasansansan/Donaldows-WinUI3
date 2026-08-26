using System;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Windows.Foundation;
using Microsoft.UI;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Desktop
{
    // Ports *roo: the mascot rising out of the taskbar corner, including the
    // escalating gags on the 3rd, 10th and 20th open of a session (`iea`, kept
    // in AppState.PopupCount).
    //
    // The reveal is two stages. First `repeat 13` raises the sprite 20px per
    // frame while revealing it from the top down (`gmode 0,182,cnt*20`). Then
    // `repeat 26` zooms a copy outward — 182+cnt*20 by 240+cnt*20, drifting up
    // 20px per frame — while fading it out (alpha 255-cnt*10), with the OS
    // window jittering VERTICALLY only: `width ,,,p+rnd(26-cnt)-rnd(26-cnt)`
    // sets p4, the Y coordinate. A `wait 140` hold follows before the menu.
    public sealed class RooPopupScene : IScene
    {
        private enum GagKind { None, Iea, AnikiUp, AnikiJumpscare }

        // 13- and 26-frame loops paced by `wait 1`, which is 10ms per unit.
        private static readonly TimeSpan RiseDuration = TimeSpan.FromMilliseconds(130);
        private static readonly TimeSpan ZoomDuration = TimeSpan.FromMilliseconds(260);
        private static readonly TimeSpan HoldDuration = TimeSpan.FromMilliseconds(1400);
        private static readonly TimeSpan RevealDuration = RiseDuration + ZoomDuration + HoldDuration;
        private static readonly TimeSpan GagSlideDuration = TimeSpan.FromMilliseconds(250);

        private const float RestY = 220f;

        private SceneContext _context = null!;
        private GagKind _gag;
        private int _beat;
        private TimeSpan _elapsed;
        private bool _revealing;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _context.AppState.PopupCount++;

            _gag = _context.AppState.PopupCount switch
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
            DesktopBackdrop.Draw(session, _context);

            if (!_revealing)
            {
                var image = _gag == GagKind.Iea ? BufferId.IeaGag : BufferId.AnikiGag;
                session.DrawImage(_context.Buffers.GetBitmap(image), 0, GagImageY());

                if (_gag == GagKind.AnikiJumpscare && _beat >= 2)
                {
                    var fadeProgress = Math.Clamp((float)(_elapsed / TimeSpan.FromSeconds(1)), 0f, 1f);
                    session.FillRectangle(0, 0, 640, 480, Color.FromArgb((byte)(fadeProgress * 255), 255, 50, 0));
                }

                return;
            }

            var mascot = _context.Buffers.GetBitmap(BufferId.MascotSprite);

            // Stage 1: rise from the taskbar, revealed top-down.
            if (_elapsed < RiseDuration)
            {
                var t = Math.Clamp((float)(_elapsed / RiseDuration), 0f, 1f);
                var y = 480f - t * (480f - RestY);
                var revealed = t * 240f;
                session.DrawImage(mascot, new Rect(0, y, 182, revealed), new Rect(0, 0, 182, revealed));
                return;
            }

            // The risen sprite then stays put for the rest of the scene.
            session.DrawImage(mascot, new Rect(0, RestY, 182, 240), new Rect(0, 0, 182, 240));

            // Stage 2: an expanding, fading copy drifts upward over it.
            if (_elapsed < RiseDuration + ZoomDuration)
            {
                var t = Math.Clamp((float)((_elapsed - RiseDuration) / ZoomDuration), 0f, 1f);
                var step = t * 26f;
                var width = 182f + step * 20f;
                var height = 240f + step * 20f;
                var alpha = Math.Clamp((255f - step * 10f) / 255f, 0f, 1f);
                session.DrawImage(mascot, new Rect(0, RestY - step * 20f, width, height), mascot.Bounds, alpha);
            }
        }

        private float GagImageY()
        {
            var slideUp = Math.Clamp((float)(_elapsed / GagSlideDuration), 0f, 1f);

            if (_gag == GagKind.AnikiUp && _beat == 4)
            {
                return 100f - slideUp * 860f;
            }

            return _beat == 0 ? 480f - slideUp * 380f : 100f;
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            _elapsed += delta;

            if (_revealing)
            {
                if (_elapsed >= RiseDuration && _elapsed < RiseDuration + ZoomDuration)
                {
                    var remaining = 1f - (float)((_elapsed - RiseDuration) / ZoomDuration);
                    _context.ShakeWindow(0, (int)Math.Clamp(remaining * 26f, 0f, 26f));
                }
                else
                {
                    _context.ShakeWindow(0, 0);
                }

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
            if (_elapsed >= GagSlideDuration)
            {
                BeginReveal();
            }

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

        // Faithful quirk: the original never disarms the desktop's onclick
        // while the pop-up plays, so clicking the start corner during it runs
        // *click, falls through to *ham, and immediately re-enters *roo —
        // restarting the animation and bumping `iea` again.
        public SceneTransition? OnPointerPressed(float x, float y) =>
            x < 20 && y > DesktopBackdrop.TaskbarTop ? new SceneTransition(SceneId.RooPopup) : null;

        private void BeginReveal()
        {
            _revealing = true;
            _elapsed = TimeSpan.Zero;
            _context.Sound.PlayEffect(SoundId.Echoroo);
        }
    }
}
