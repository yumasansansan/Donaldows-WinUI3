using System;
using System.Diagnostics;
using System.IO;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using Windows.Foundation;
using Windows.System;
using Windows.UI;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Boot
{
    // Ports *biosmenu/*bioskeycheck/*biosdialog.
    //
    // Layout follows the original's geometry, which is driven by `by` — a
    // counter that grows 0→40 on entry, animating the panels open:
    //   rows           boxf 0, 40+by*(n+1), ..., +20      -> 80/120/160/200/240
    //   description    boxf 641-by*10, 60, 961-by*8, 360  -> 241,60 .. 641,360
    //   bottom bar     boxf 0, 480-by*2, 640, 480         -> y 400..480
    //   marquee strip  boxf 0, 380, 640, 400
    //
    // Function-key mapping follows the original's raw wparam values, which do
    // NOT line up with its own on-screen hint text:
    //   112 = F1 -> readme          (matches hint "[F1]-Readme")
    //   113 = F2 -> Reset confirm   (matches hint)
    //   114 = F3 -> Default confirm (matches hint)
    //   115 = F4 -> "exit without saving" confirm, and Y here is the ONE key
    //               combination that actually leaves the menu — which is what
    //               the first menu row's own help text tells you to press.
    //   116 = F5 -> "save and exit" confirm, gated on a flag (`sv`) that is
    //               never assigned anywhere in the source, so it is dead code.
    public sealed class BiosMenuScene : IScene
    {
        private readonly record struct MenuItem(string Title, string Description);

        private static readonly MenuItem[] Items =
        {
            new("BIOSメニューへようこそ", "ドナルドウズのBIOSメニューです。\n未完成なので[F4]キー押して[Y]を選んで下さい"),
            new("セーブ設定", "オペレーティングシステムの設定データを編集したり\nこのソフトを入手時の状態にすることができます。"),
            new("システム設定", "BIOSの表示言語を変更したり、オペレーティングシス\nテムの初期化も可能です"),
            new("ハードウェアーテスト", "オペレーティングシステムが正常に動作するか\n総合的にテストします。ベンチマーク機能もついて\nいますので。他のマシンと性能を比べてみることも\n一応できます。"),
            new("終了", "終了します。"),
        };

        private const string MarqueeText =
            "こちらで各種設定を行います。設定方法やどんな事を設定できるかを知るには" +
            "ユーザーズマニュアルを参照して下さい([F1]key)";

        private const float ByMax = 40f;
        private const float ByPerSecond = 60f;

        private SceneContext _context = null!;
        private int _selected;
        private string? _pendingConfirm;
        private bool _confirmExitsOnYes;
        private float _marqueeX = 640f;
        private float _by;

        public void Enter(SceneContext context, object? payload)
        {
            _context = context;
            _selected = 0;
            _pendingConfirm = null;
            _confirmExitsOnYes = false;
            _marqueeX = 640f;
            _by = 0f;
        }

        public SceneTransition? Update(TimeSpan delta)
        {
            _by = Math.Min(ByMax, _by + ByPerSecond * (float)delta.TotalSeconds);

            _marqueeX -= 60f * (float)delta.TotalSeconds;
            if (_marqueeX <= -1000f)
            {
                _marqueeX = 640f;
            }

            return null;
        }

        public void Draw(CanvasDrawingSession session, Size canvasSize)
        {
            session.Clear(Colors.Black);

            using var format = HspFont.Create();

            // Bottom blue bar grows up from the bottom edge as `by` climbs.
            session.FillRectangle(0, 480 - _by * 2, 640, _by * 2, Color.FromArgb(255, 0, 0, 128));

            session.DrawText(
                "DONA BIOS V0.5.5--(c)2008-2009                              BIOS MENU",
                0, 0, Colors.White, format);

            // Description panel slides in from the right.
            var descX = 641 - _by * 10;
            var descRight = 961 - _by * 8;
            session.FillRectangle(descX, 60, descRight - descX, 300, Colors.White);
            session.DrawText(Items[_selected].Description, descX, 60, Colors.Black, format);

            for (var i = 0; i < Items.Length; i++)
            {
                var y = 40 + _by * (i + 1);
                if (i == _selected)
                {
                    var width = i == 0 ? _by + 200 : 240;
                    session.FillRectangle(0, y, width, 20, Color.FromArgb(255, 0, 200, 255));
                    session.DrawText(Items[i].Title, 40, y, Colors.Black, format);
                }
                else
                {
                    session.DrawText(Items[i].Title, 40, y, Colors.White, format);
                }
            }

            session.FillRectangle(0, 380, 640, 20, Colors.Black);
            session.DrawText(MarqueeText, _marqueeX, 380, Colors.White, format);

            session.DrawText("[Esc]-ForceQuit [F1]-Readme [F2]-Reset [F3]-Default [F4]-Save&Reboot", 0, 405, Colors.White, format);
            session.DrawText("操作    [↑]     確定        取り消し", 0, 441, Colors.White, format);
            session.DrawText("    [←][↓][→]     [Enter] 　　　　[BS]or[Del] ", 0, 459, Colors.White, format);

            if (_pendingConfirm is { } message)
            {
                session.FillRectangle(0, 0, 640, 480, Color.FromArgb(200, 0, 0, 0));
                session.DrawText(message, 0, 423, Colors.White, format);
                session.DrawText("[Y]es/[N]o", 550, 460, Colors.White, format);
            }
        }

        public SceneTransition? OnKeyDown(VirtualKey key)
        {
            if (_pendingConfirm is not null)
            {
                if (key == VirtualKey.Y)
                {
                    _pendingConfirm = null;
                    return _confirmExitsOnYes ? ExitToBoot() : null;
                }

                if (key == VirtualKey.N)
                {
                    _pendingConfirm = null;
                }

                return null;
            }

            switch (key)
            {
                case VirtualKey.Up:
                    _selected = _selected == 0 ? Items.Length - 1 : _selected - 1;
                    return null;

                case VirtualKey.Down:
                case VirtualKey.Tab:
                    _selected = (_selected + 1) % Items.Length;
                    return null;

                case VirtualKey.Escape:
                    return ExitToBoot();

                case VirtualKey.F1:
                    LaunchReadme();
                    return null;

                case VirtualKey.F2:
                    _pendingConfirm = "確認：BIOSメニュー起動時の状態にもどしますか？";
                    _confirmExitsOnYes = false;
                    return null;

                case VirtualKey.F3:
                    _pendingConfirm = "確認：BIOSを設定を初期状態に戻しますか？";
                    _confirmExitsOnYes = false;
                    return null;

                case VirtualKey.F4:
                    _pendingConfirm = "確認：BIOS設定を保存しないでBIOSメニューを終了しますか？";
                    _confirmExitsOnYes = true;
                    return null;

                // F5 is the original's `sv=1`-gated "save and exit", and `sv` is
                // never set, so the key does nothing there either.
                default:
                    return null;
            }
        }

        // The original always jumps to *start, which itself re-checks the
        // install flag and forwards to *kidou when already installed.
        private SceneTransition ExitToBoot() =>
            new(_context.Save.IsInstalled ? SceneId.Kidou : SceneId.StartBoot);

        private static void LaunchReadme()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "はじめに読んでね☆ドナルドより.htm");
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
    }
}
