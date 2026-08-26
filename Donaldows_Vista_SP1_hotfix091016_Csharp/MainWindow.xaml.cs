using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Audio;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Controls;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Save;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Boot;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Cmd;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Desktop;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Dodge;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Kiss;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Messenger;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Rpg;
using Donaldows_Vista_SP1_hotfix091016_Csharp.Scenes.Shutdown;
using Donaldows_Vista_SP1_hotfix091016_Csharp.State;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Donaldows_Vista_SP1_hotfix091016_Csharp
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        // Win2D/CanvasAnimatedControl draws via a SwapChainPanel that doesn't
        // reliably take keyboard focus, so keyboard input is instead handled
        // on RootGrid (which focuses normally) and routed key events still
        // bubble up to it from whatever descendant has focus.
        private readonly SaveManager _saveManager = new();
        private readonly SaveData _save;
        private readonly AppState _appState = new();

        private SceneManager? _sceneManager;
        private SoundManager? _sound;
        private SceneContext? _context;
        private Dictionary<SceneId, Func<IScene>>? _factories;
        private bool _bootRequested;
        private bool _allowClose;
        private bool _cursorHidden;
        private readonly CursorHostGrid _cursorHost;

        public MainWindow()
        {
            InitializeComponent();

            // Wrap the XAML content so the cursor can be hidden during the
            // dodge game; see CursorHostGrid for why this isn't done in markup.
            var content = (FrameworkElement)Content;
            Content = null;
            _cursorHost = CursorHostGrid.Wrap(content);
            Content = _cursorHost;

            _save = _saveManager.Load();
            NameTextBox.Text = _save.PlayerName;

            RootGrid.Loaded += RootGrid_Loaded;
            Closed += MainWindow_Closed;
            AppWindow.Closing += AppWindow_Closing;
        }

        // Ports onexit goto *virus: the original intercepts the window's
        // close button and redirects into a "can't quit me" prank instead of
        // exiting, only really closing via EndonaScene (reached through the
        // shutdown dialog or the nag dialog's own "もちろんさあ" button).
        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            if (_allowClose || _sceneManager is null || _context?.CloseIntercept is not { } target)
            {
                return;
            }

            args.Cancel = true;
            _sceneManager.ForceTransition(target);
        }

        // WinUI3 opens new windows at a default size, not sized to their content.
        // Measure the chrome (title bar/borders) overhead once the first layout
        // pass has run, then resize the AppWindow so the 640x480 canvas fits
        // exactly with no extra window around it, and lock the size so dragging
        // can't reintroduce the mismatch (the original HSP window wasn't resizable either).
        private void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            RootGrid.Loaded -= RootGrid_Loaded;

            var scale = RootGrid.XamlRoot.RasterizationScale;
            var desiredWidthPx = (int)Math.Round(640 * scale);
            var desiredHeightPx = (int)Math.Round(480 * scale);
            var actualWidthPx = (int)Math.Round(RootGrid.ActualWidth * scale);
            var actualHeightPx = (int)Math.Round(RootGrid.ActualHeight * scale);

            var deltaWidth = desiredWidthPx - actualWidthPx;
            var deltaHeight = desiredHeightPx - actualHeightPx;

            AppWindow.Resize(new SizeInt32(AppWindow.Size.Width + deltaWidth, AppWindow.Size.Height + deltaHeight));

            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
            }
        }

        // CanvasAnimatedControl drives Update/Draw from its own game-loop thread.
        // If the window starts tearing down while that thread is still mid-frame
        // (touching WinRT objects being disposed), the process can fail fast on
        // exit. Pausing the control and stopping all sound synchronously on the
        // UI thread before Close() proceeds avoids that race.
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            MainCanvas.Paused = true;
            _sound?.StopAll();
        }

        private void MainCanvas_CreateResources(CanvasAnimatedControl sender, CanvasCreateResourcesEventArgs args)
        {
            args.TrackAsyncAction(InitializeAsync(sender).AsAsyncAction());
        }

        private async Task InitializeAsync(CanvasAnimatedControl sender)
        {
            var buffers = new BufferManager();
            await buffers.InitializeAsync(sender);
            _sound = new SoundManager();

            _context = new SceneContext
            {
                Sound = _sound,
                Buffers = buffers,
                RequestAppExit = () => DispatcherQueue.TryEnqueue(() =>
                {
                    _allowClose = true;
                    Close();
                }),
                MinimizeWindow = () => DispatcherQueue.TryEnqueue(() =>
                {
                    if (AppWindow.Presenter is OverlappedPresenter p)
                    {
                        p.Minimize();
                    }
                }),
                Save = _save,
                SaveManager = _saveManager,
                AppState = _appState,
            };

            _factories = new Dictionary<SceneId, Func<IScene>>
            {
                [SceneId.IdleDesktop] = () => new IdleDesktopScene(),
                [SceneId.RooPopup] = () => new RooPopupScene(),
                [SceneId.StartMenu] = () => new StartMenuScene(),
                [SceneId.ModoRoo] = () => new ModoRooScene(),
                [SceneId.ShutdownDialog] = () => new ShutdownDialogScene(),
                [SceneId.Endona] = () => new EndonaScene(),
                [SceneId.AboutPopup] = () => new AboutPopupScene(),
                [SceneId.Logoff] = () => new LogoffScene(),
                [SceneId.Screensaver] = () => new ScreensaverScene(),
                [SceneId.VirusNag] = () => new VirusNagScene(),
                [SceneId.BiosPost] = () => new BiosPostScene(),
                [SceneId.BiosMenu] = () => new BiosMenuScene(),
                [SceneId.StartBoot] = () => new StartBootScene(),
                [SceneId.InstallWizard] = () => new InstallWizardScene(),
                [SceneId.Kiss] = () => new KissScene(),
                [SceneId.Bsod] = () => new BsodScene(),
                [SceneId.Kidou] = () => new KidouScene(),
                [SceneId.DodgeGame] = () => new DodgeGameScene(),
                [SceneId.MessengerIntro] = () => new MessengerIntroScene(),
                [SceneId.MessengerChat] = () => new MessengerChatScene(),
                [SceneId.MessengerOffline] = () => new MessengerOfflineScene(),
                [SceneId.MessengerCloseNag] = () => new MessengerCloseNagScene(),
                [SceneId.CmdPrompt] = () => new CmdPromptScene(),
                [SceneId.Notepad] = () => new NotepadScene(),
                [SceneId.RpgIntro] = () => new RpgIntroScene(),
                [SceneId.RpgBattle] = () => new RpgBattleScene(),
                [SceneId.RpgGameOver] = () => new RpgGameOverScene(),
            };

            // The player may have already clicked "起動" on the XAML overlay
            // before this async init finished; start the boot chain now if so.
            if (_bootRequested)
            {
                StartBootChain();
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            _save.PlayerName = NameTextBox.Text;
            _saveManager.Save(_save);

            SetumeiOverlay.Visibility = Visibility.Collapsed;
            _bootRequested = true;

            if (_context is not null && _factories is not null)
            {
                StartBootChain();
            }
        }

        private void StartBootChain()
        {
            _sceneManager = new SceneManager(_context!, _factories!, SceneId.BiosPost);
            RootGrid.Focus(FocusState.Programmatic);
        }

        private void MainCanvas_Update(ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args)
        {
            _sceneManager?.Update(args.Timing.ElapsedTime);

            // Scenes toggle HideCursor from the game-loop thread; applying it
            // has to happen on the UI thread.
            var wantHidden = _context?.HideCursor == true;
            if (wantHidden != _cursorHidden)
            {
                _cursorHidden = wantHidden;
                DispatcherQueue.TryEnqueue(() => _cursorHost.SetCursorHidden(wantHidden));
            }
        }

        private void MainCanvas_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            _sceneManager?.Draw(args.DrawingSession, sender.Size);
        }

        private void MainCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(MainCanvas).Position;
            _sceneManager?.NotifyPointerMoved((float)point.X, (float)point.Y);
        }

        private void MainCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            RootGrid.Focus(FocusState.Programmatic);
            var point = e.GetCurrentPoint(MainCanvas).Position;
            _sceneManager?.NotifyPointerPressed((float)point.X, (float)point.Y);
        }

        private void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            _sceneManager?.NotifyKeyDown(e.Key);
        }
    }
}
