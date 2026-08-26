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
        private bool _allowClose;
        private bool _canvasShutDown;
        private bool _cursorHidden;
        private readonly CursorHostGrid _cursorHost;
        private PointInt32? _restingWindowPosition;

        public MainWindow()
        {
            InitializeComponent();

            // Input surface layered directly over the canvas — it must be the
            // pointer hit-test target for the cursor to be hideable, so all
            // pointer and key handling lives on it. See CursorHostGrid.
            _cursorHost = new CursorHostGrid
            {
                Width = 640,
                Height = 480,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };
            _cursorHost.PointerMoved += MainCanvas_PointerMoved;
            _cursorHost.PointerPressed += MainCanvas_PointerPressed;
            RootGrid.Children.Insert(RootGrid.Children.IndexOf(MainCanvas) + 1, _cursorHost);

            _save = _saveManager.Load();
            NameTextBox.Text = _save.PlayerName;

            // buffer.hsp's opening animation runs before *setumei, so the name
            // entry overlay starts hidden and is revealed by SetumeiScene.
            SetumeiOverlay.Visibility = Visibility.Collapsed;

            LockWindowSize();

            // Keys are handled on the root with handledEventsToo so they still
            // reach the scene even when a focused child marks them handled
            // (arrow keys and Tab are otherwise eaten by focus navigation).
            RootGrid.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(RootGrid_KeyDown), true);

            RootGrid.Loaded += RootGrid_Loaded;
            Activated += MainWindow_Activated;
            Closed += MainWindow_Closed;
            AppWindow.Closing += AppWindow_Closing;
        }

        // Ports onexit goto *virus: the original intercepts the window's
        // close button and redirects into a "can't quit me" prank instead of
        // exiting, only really closing via EndonaScene (reached through the
        // shutdown dialog or the nag dialog's own "もちろんさあ" button).
        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            if (!_allowClose && _sceneManager is not null && _context?.CloseIntercept is { } target)
            {
                args.Cancel = true;
                _sceneManager.ForceTransition(target);
                return;
            }

            ShutDownCanvas();
        }

        // The original's window is a fixed 640x480. Applying this once in the
        // constructor does not stick — launching by double-clicking the exe
        // activates the window before the presenter is ready — so it is
        // re-applied after layout and on every activation.
        private void LockWindowSize()
        {
            if (AppWindow?.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
            }
        }

        // WinUI3 opens windows at a default size rather than sized to content,
        // so the chrome overhead is measured once the first layout pass has run
        // and the window resized to make the 640x480 canvas fit exactly.
        private void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            RootGrid.Loaded -= RootGrid_Loaded;
            LockWindowSize();

            if (RootGrid.XamlRoot is not { } xamlRoot)
            {
                DispatcherQueue.TryEnqueue(LockWindowSize);
                return;
            }

            var scale = xamlRoot.RasterizationScale;
            var deltaWidth = (int)Math.Round((640 - RootGrid.ActualWidth) * scale);
            var deltaHeight = (int)Math.Round((480 - RootGrid.ActualHeight) * scale);

            AppWindow.Resize(new SizeInt32(AppWindow.Size.Width + deltaWidth, AppWindow.Size.Height + deltaHeight));
            _cursorHost.Focus(FocusState.Programmatic);

            // Re-applied after the resize has been processed; AppWindow.Resize
            // can otherwise land after the style change and undo it.
            DispatcherQueue.TryEnqueue(LockWindowSize);
        }

        // Keyboard focus can be lost when the window is deactivated (or when a
        // scene swap happens while another element briefly holds it), so it is
        // reclaimed whenever the window comes back to the foreground.
        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                return;
            }

            LockWindowSize();

            if (SetumeiOverlay.Visibility != Visibility.Visible)
            {
                _cursorHost.Focus(FocusState.Programmatic);
            }
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args) => ShutDownCanvas();

        // CanvasAnimatedControl drives Update/Draw on its own game-loop thread.
        // Merely pausing it does not wait for an in-flight frame, so tearing the
        // window down could leave that thread touching resources that were
        // already released — which surfaced as a 0x80000003 fail-fast on exit.
        // RemoveFromVisualTree is Win2D's documented shutdown for this control:
        // it stops the loop and releases its resources deterministically. This
        // runs from AppWindow.Closing, while the window is still alive, and is
        // idempotent because Closed fires afterwards too.
        private void ShutDownCanvas()
        {
            if (_canvasShutDown)
            {
                return;
            }

            _canvasShutDown = true;
            _sceneManager = null;
            _sound?.Shutdown();

            MainCanvas.Paused = true;
            MainCanvas.RemoveFromVisualTree();
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
                    if (_canvasShutDown)
                    {
                        return;
                    }

                    _allowClose = true;
                    Close();
                }),
                ShakeWindow = (amplitudeX, amplitudeY) =>
                {
                    // Nothing to do, and nothing to undo — don't queue work.
                    if (amplitudeX <= 0 && amplitudeY <= 0 && _restingWindowPosition is null)
                    {
                        return;
                    }

                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (_canvasShutDown)
                        {
                            return;
                        }

                        if (amplitudeX <= 0 && amplitudeY <= 0)
                        {
                            if (_restingWindowPosition is { } resting)
                            {
                                AppWindow.Move(resting);
                                _restingWindowPosition = null;
                            }

                            return;
                        }

                        // Captured on the first frame of each shake, not once
                        // for the lifetime of the window — otherwise dragging
                        // the window and then shaking would snap it back to
                        // where it first opened.
                        _restingWindowPosition ??= AppWindow.Position;
                        var home = _restingWindowPosition.Value;

                        AppWindow.Move(new PointInt32(
                            home.X + (amplitudeX > 0 ? Random.Shared.Next(-amplitudeX, amplitudeX + 1) : 0),
                            home.Y + (amplitudeY > 0 ? Random.Shared.Next(-amplitudeY, amplitudeY + 1) : 0)));
                    });
                },
                ShowNameEntry = () => DispatcherQueue.TryEnqueue(() =>
                {
                    SetumeiOverlay.Visibility = Visibility.Visible;
                    NameTextBox.Focus(FocusState.Programmatic);
                }),
                MinimizeWindow = () => DispatcherQueue.TryEnqueue(() =>
                {
                    if (!_canvasShutDown && AppWindow.Presenter is OverlappedPresenter p)
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
                [SceneId.BootIntro] = () => new BootIntroScene(),
                [SceneId.Setumei] = () => new SetumeiScene(),
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

            _sceneManager = new SceneManager(_context, _factories, SceneId.BootIntro);
            DispatcherQueue.TryEnqueue(() => _cursorHost.Focus(FocusState.Programmatic));
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            _save.PlayerName = NameTextBox.Text;
            _saveManager.Save(_save);

            SetumeiOverlay.Visibility = Visibility.Collapsed;
            _sceneManager?.ForceTransition(SceneId.BiosPost);
            _cursorHost.Focus(FocusState.Programmatic);
        }

        private void MainCanvas_Update(ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args)
        {
            if (_canvasShutDown)
            {
                return;
            }

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
            if (_canvasShutDown)
            {
                return;
            }

            _sceneManager?.Draw(args.DrawingSession, sender, sender.Size);
        }

        private void MainCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(_cursorHost).Position;
            _sceneManager?.NotifyPointerMoved((float)point.X, (float)point.Y);
        }

        private void MainCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _cursorHost.Focus(FocusState.Programmatic);
            var point = e.GetCurrentPoint(_cursorHost).Position;
            _sceneManager?.NotifyPointerPressed((float)point.X, (float)point.Y);
        }

        private void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            _sceneManager?.NotifyKeyDown(e.Key);
        }
    }
}
