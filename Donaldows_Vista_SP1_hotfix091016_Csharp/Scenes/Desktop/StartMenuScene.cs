using System;
using System.Diagnostics;
using System.IO;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Windows.Foundation;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Desktop
{
    // Ports *roomenu/*roomenuclick. The taskbar-corner pop-up/slide-down
    // animations from *roo/*modoroo now live in RooPopupScene/ModoRooScene;
    // this scene itself opens/closes instantly at its resting position (a=220).
    public sealed class StartMenuScene : IScene
    {
        private const float MenuAnchorY = 220f;
        private const float MenuX = 1f;
        private const float MenuWidth = 180f;
        private const float RowHeight = 24f;
        private const float TaskbarTop = 460f;

        private readonly record struct Row(float Y, string Label, SceneId? Target, Action? SideEffect = null);

        private static readonly Row[] Rows =
        {
            new(MenuAnchorY + 3,   "ドナルドウズ　ゲーム",              SceneId.DodgeGame),
            new(MenuAnchorY + 33,  "ドナルドプロンプト",                 SceneId.CmdPrompt),
            new(MenuAnchorY + 63,  "ﾄﾞﾅﾙﾄﾞｳｽﾞ　ﾒｯｾﾝｼﾞｬｰ",                SceneId.MessengerIntro),
            new(MenuAnchorY + 93,  "Donarnet Explorer",                 null), // dead in the original too
            new(MenuAnchorY + 153, "ヘルプ",                             null, LaunchHelp), // *roohelp
            new(MenuAnchorY + 183, "ログオフ",                           SceneId.Logoff),
            new(MenuAnchorY + 213, "シャットダウン",                     SceneId.ShutdownDialog),
        };

        private static void LaunchHelp()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "はじめに読んでね☆ドナルドより.htm");
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        // Ports the menu loop's idle taunts, fired at frame counts 400/1000/2000
        // of an `await 1` loop.
        private static readonly (TimeSpan At, SoundId Sound)[] IdleTaunts =
        {
            (TimeSpan.FromSeconds(7), SoundId.Izen),
            (TimeSpan.FromSeconds(17), SoundId.Mosi),
            (TimeSpan.FromSeconds(33), SoundId.Aree),
        };

        private SceneContext _context = null!;
        private int _hoveredRow = -1;
        private TimeSpan _elapsed;
        private int _nextTaunt;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _hoveredRow = -1;
            _elapsed = TimeSpan.Zero;
            _nextTaunt = 0;
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            _elapsed += delta;

            if (_nextTaunt < IdleTaunts.Length && _elapsed >= IdleTaunts[_nextTaunt].At)
            {
                _context.Sound.PlayEffect(IdleTaunts[_nextTaunt].Sound);
                _nextTaunt++;
            }

            return null;
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            DesktopBackdrop.Draw(session, _context);

            session.DrawImage(_context.Buffers.GetBitmap(BufferId.MascotSprite), new Rect(0, MenuAnchorY, 182, 240));

            using var rowFormat = new CanvasTextFormat { FontSize = 14, VerticalAlignment = CanvasVerticalAlignment.Center };
            for (var i = 0; i < Rows.Length; i++)
            {
                var row = Rows[i];
                session.FillRectangle(MenuX, row.Y, MenuWidth, RowHeight, _context.Buffers.GetColor(BufferId.MenuRowBackdrop));
                session.DrawText(row.Label, new Rect(MenuX + 4, row.Y, MenuWidth - 4, RowHeight), Colors.White, rowFormat);

                if (i == _hoveredRow)
                {
                    var highlight = _context.Buffers.GetColor(BufferId.MenuHoverHighlight);
                    session.FillRectangle(MenuX, row.Y, MenuWidth, RowHeight, Color.FromArgb(100, highlight.R, highlight.G, highlight.B));
                }
            }
        }

        public SceneTransition? OnPointerMoved(float x, float y)
        {
            _hoveredRow = -1;
            if (x < 0 || x >= MenuWidth)
            {
                return null;
            }

            for (var i = 0; i < Rows.Length; i++)
            {
                if (y >= Rows[i].Y && y < Rows[i].Y + RowHeight)
                {
                    _hoveredRow = i;
                    break;
                }
            }

            return null;
        }

        public SceneTransition? OnPointerPressed(float x, float y)
        {
            if (x < 20 && y > TaskbarTop)
            {
                return new SceneTransition(SceneId.ModoRoo);
            }

            if (_hoveredRow >= 0)
            {
                var row = Rows[_hoveredRow];
                if (row.Target is { } sceneId)
                {
                    _context.Sound.PlayEffect(SoundId.Kore);
                    return new SceneTransition(sceneId);
                }

                if (row.SideEffect is { } sideEffect)
                {
                    _context.Sound.PlayEffect(SoundId.Kore);
                    sideEffect();
                    return null;
                }
            }

            _context.Sound.PlayEffect(SoundId.Kusy);
            return null;
        }
    }
}
