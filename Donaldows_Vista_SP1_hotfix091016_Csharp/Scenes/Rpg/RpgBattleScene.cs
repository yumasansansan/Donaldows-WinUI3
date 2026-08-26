using System;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using Windows.Foundation;
using Windows.System;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Rpg
{
    // Ports the main *rpg battle loop: *rpgmenu/*punch/*result/*hono plus the
    // idle sneeze subsystem.
    //
    // The flame background is the original's cloud.gif — a 640x5280 strip
    // scrolled vertically behind the fight and alpha-blended at `hono_fade`,
    // which ramps 0→50 and doubles as the red screen tint (`c_red`). The
    // green/blue tint channels exist in the source but are never assigned, so
    // only red is live here too.
    //
    // Frame-rate-dependent rates in the original (its battle loop is an
    // uncapped `await 0`) are ported as real-time rates.
    //
    // Simplified: *punch's 140-frame charge-up, 25-frame zoom impact and
    // 75-frame knockback are condensed to a shake-and-flash.
    public sealed class RpgBattleScene : IScene
    {
        private enum Phase { Menu, FlavorDialog, Punching, Result }

        // sennou += 0.000127 per frame in the original; ~60fps worth per second.
        private const float PassiveSennouPerSecond = 0.0076f;
        private const float FlameRampPerSecond = 6f;   // hono_fade += 0.1/frame
        private const float FlameMax = 50f;
        private const float FlameScrollPerSecond = 1620f; // scroll += 27/frame
        private const float FlameScrollWrap = 4800f;

        private static readonly TimeSpan PunchDuration = TimeSpan.FromSeconds(1.0);
        private static readonly TimeSpan ResultAutoDismiss = TimeSpan.FromSeconds(6);
        private static readonly TimeSpan SneezeRollInterval = TimeSpan.FromSeconds(4);

        private SceneContext _context = null!;
        private RpgBattleState _state = null!;
        private Phase _phase;

        private string _flavorText = "";
        private TimeSpan _phaseElapsed;
        private TimeSpan _sneezeTimer;
        private float _flameScroll;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _state = payload as RpgBattleState ?? new RpgBattleState();
            _phase = Phase.Menu;
            _phaseElapsed = TimeSpan.Zero;
            _sneezeTimer = TimeSpan.Zero;
            _flameScroll = 0f;
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            session.Clear(Colors.Black);

            // *hono: scrolling cloud strip, alpha-blended at hono_fade.
            if (_state.HonoFade > 0)
            {
                var fire = _context.Buffers.GetBitmap(BufferId.FireScroll);
                session.DrawImage(
                    fire,
                    new Rect(0, 0, 640, 480),
                    new Rect(0, _flameScroll, 640, 480),
                    _state.HonoFade / 255f);
            }

            var flicker = 155 + Random.Shared.Next(0, 100);
            session.DrawImage(
                _context.Buffers.GetBitmap(BufferId.MascotSprite),
                new Rect(229, 30, 182, 270),
                new Rect(0, 0, 182, 270),
                flicker / 255f);

            if (_state.CRed > 0)
            {
                session.FillRectangle(0, 0, 640, 480, Color.FromArgb((byte)Math.Clamp(_state.CRed, 0, 255), 255, 0, 0));
            }

            using var format = HspFont.Create();
            var status = $"{PlayerName()}のデータ 洗脳率:{_state.Sennou:0.0}% 体力:{(int)_state.Tairyoku}";

            switch (_phase)
            {
                case Phase.Menu:
                    session.FillRectangle(0, 350, 640, 110, Color.FromArgb(100, 0, 0, 0));
                    session.DrawText(
                        $"{_state.EnemyName}が突然現れた！！\nどうする？\n1:ぶんなぐル～☆　2:助けを呼んでル～☆　3:にげル～☆　4:ドナルドにこくル～☆\n数字キーを押して選んでね。",
                        10, 350, Colors.White, format);
                    session.DrawText(status, 10, 440, Colors.White, format);
                    break;

                case Phase.FlavorDialog:
                    session.FillRectangle(60, 150, 520, 180, Colors.Black);
                    session.DrawText(_flavorText, new Rect(70, 160, 500, 160), Colors.White, format);
                    session.DrawText("[クリックまたはキーで続行]", new Rect(70, 300, 500, 20), Colors.Gray, format);
                    break;

                case Phase.Punching:
                    var jx = Random.Shared.Next(-6, 6);
                    var jy = Random.Shared.Next(-6, 6);
                    session.FillRectangle(jx, jy, 640, 480, Color.FromArgb(120, 255, 0, 0));
                    break;

                case Phase.Result:
                    session.FillRectangle(0, 350, 640, 110, Color.FromArgb(100, 0, 0, 0));
                    session.DrawText($"{_state.EnemyName}は{_state.Hit}のダメージを喰らった{_state.HitMes}", 10, 350, Colors.White, format);
                    session.DrawText(
                        $"{PlayerName()}はドナルドに近づいたから{_state.SennouGain:0.0}%洗脳されてしまった\n体力は{(int)_state.TairyokuLoss}消耗した",
                        10, 370, Colors.White, format);
                    session.DrawText(status, 10, 440, Colors.White, format);
                    break;
            }
        }

        private string PlayerName() => string.IsNullOrEmpty(_context.Save.PlayerName) ? "君" : _context.Save.PlayerName;

        public SceneTransition? Update(TimeSpan delta)
        {
            _phaseElapsed += delta;
            var seconds = (float)delta.TotalSeconds;

            // The flame effect keeps running through every phase, as in the
            // original's per-iteration *hono call.
            _state.HonoFade = Math.Min(FlameMax, _state.HonoFade + FlameRampPerSecond * seconds);
            _state.CRed = _state.HonoFade;
            _flameScroll += FlameScrollPerSecond * seconds;
            if (_flameScroll > FlameScrollWrap)
            {
                _flameScroll -= FlameScrollWrap;
            }

            if (_phase == Phase.Menu)
            {
                _state.Sennou += PassiveSennouPerSecond * seconds;

                _sneezeTimer += delta;
                if (_sneezeTimer >= SneezeRollInterval)
                {
                    _sneezeTimer = TimeSpan.Zero;
                    if (Random.Shared.Next(0, 5) == 0)
                    {
                        _context.Sound.PlayEffect(SoundId.Kusy);
                        _state.Sennou += 2.0f;
                    }
                }

                if (_state.Tairyoku <= 0 || _state.Sennou >= 100.0f)
                {
                    _context.Sound.StopAll();
                    return new SceneTransition(SceneId.RpgGameOver, _state);
                }

                return null;
            }

            if (_phase == Phase.Punching && _phaseElapsed >= PunchDuration)
            {
                ResolvePunch();
                _phase = Phase.Result;
                _phaseElapsed = TimeSpan.Zero;
                return null;
            }

            if (_phase == Phase.Result && _phaseElapsed >= ResultAutoDismiss)
            {
                _phase = Phase.Menu;
                _phaseElapsed = TimeSpan.Zero;
            }

            return null;
        }

        private void ResolvePunch()
        {
            var hit = Random.Shared.Next(0, 1000);
            _state.EnemyHp -= hit;

            // Faithful quirk: the +1000 critical bump is display-only. The
            // original decrements enehp with the un-boosted roll first, so the
            // bonus never affects real damage — and nothing ever reads enehp
            // anyway, since the battle has no victory condition.
            if (hit > 800)
            {
                _state.HitMes = "！クリティカルヒットだ！！";
                _context.Sound.PlayEffect(SoundId.Iea);
                hit += 1000;
            }
            else
            {
                _state.HitMes = "";
                _context.Sound.PlayEffect(SoundId.Aro);
            }

            _state.Hit = hit;
            _state.TairyokuLoss = Random.Shared.Next(0, 200) + 800;
            _state.Tairyoku -= _state.TairyokuLoss;
            _state.SennouGain = 0.1f * Random.Shared.Next(0, 100);
            _state.Sennou += _state.SennouGain;
        }

        public SceneTransition? OnPointerPressed(float x, float y)
        {
            if (_phase is Phase.FlavorDialog or Phase.Result)
            {
                _phase = Phase.Menu;
                _phaseElapsed = TimeSpan.Zero;
            }

            return null;
        }

        public SceneTransition? OnKeyDown(VirtualKey key)
        {
            if (_phase is Phase.FlavorDialog or Phase.Result)
            {
                _phase = Phase.Menu;
                _phaseElapsed = TimeSpan.Zero;
                return null;
            }

            if (_phase != Phase.Menu)
            {
                return null;
            }

            switch (key)
            {
                case VirtualKey.Number1 or VirtualKey.NumberPad1:
                    _phase = Phase.Punching;
                    _phaseElapsed = TimeSpan.Zero;
                    _context.Sound.PlayEffect(SoundId.Rurou);
                    return null;

                case VirtualKey.Number2 or VirtualKey.NumberPad2:
                    _phase = Phase.FlavorDialog;
                    _phaseElapsed = TimeSpan.Zero;
                    _flavorText = "ドナルド「ここは密室だよぉ驚いた？？」";
                    return null;

                case VirtualKey.Number3 or VirtualKey.NumberPad3:
                    _phase = Phase.FlavorDialog;
                    _phaseElapsed = TimeSpan.Zero;
                    // The original spams a ~900-character wall of repeated
                    // ﾆｶﾞｻﾅｲ followed by three more dialogs; condensed here.
                    _flavorText =
                        "ﾆｶﾞｻﾅｲﾆｶﾞｻﾅｲﾆｶﾞｻﾅｲﾆｶﾞｻﾅｲﾆｶﾞｻﾅｲﾆｶﾞｻﾅｲﾆｶﾞｻﾅｲﾆｶﾞｻﾅｲ……\n" +
                        "らんらんる～☆らんらんる～☆らんらんる～☆\n" +
                        "どんどんきみは洗脳されていくよ、へっはっはっはっは☆";
                    return null;

                case VirtualKey.Number4 or VirtualKey.NumberPad4:
                    _context.Sound.StopAll();
                    return new SceneTransition(SceneId.Kiss);

                default:
                    return null;
            }
        }
    }
}
