using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Controls
{
    // Input surface laid directly over the Win2D canvas.
    //
    // WinUI resolves the pointer cursor from the element the pointer is
    // actually over, so setting ProtectedCursor on an ANCESTOR of the canvas
    // has no effect — the canvas itself still supplies the default arrow. This
    // grid sits on top of the canvas with a transparent background so that it
    // is the hit-test target, which makes its ProtectedCursor the one that
    // wins; assigning null there hides the cursor outright, which is what the
    // dodge game's `mouse -1` needs. Win32 SetCursor doesn't work either,
    // because the XAML input system reasserts its own cursor per message.
    //
    // It also owns keyboard focus and key handling for the window: the Win2D
    // SwapChainPanel doesn't reliably take focus.
    //
    // Instantiated from code rather than markup because this project's XAML
    // compiler runs without a LocalAssembly reference (build warning WMC1509)
    // and so cannot resolve project-local types used in markup.
    public sealed partial class CursorHostGrid : Grid
    {
        private readonly InputCursor _arrow = InputSystemCursor.Create(InputSystemCursorShape.Arrow);

        public CursorHostGrid()
        {
            IsTabStop = true;
            UseSystemFocusVisuals = false;
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }

        public void SetCursorHidden(bool hidden) => ProtectedCursor = hidden ? null : _arrow;
    }
}
