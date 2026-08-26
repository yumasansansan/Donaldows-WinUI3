using Microsoft.Graphics.Canvas.Text;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Rendering
{
    // HSP prints with the system GUI font, which on a 640x480 canvas works out
    // to roughly 16px. Only the size is pinned here — the typeface is left to
    // the system default.
    public static class HspFont
    {
        public const float DefaultSize = 16f;
        public const float LineHeight = 16f;

        public static CanvasTextFormat Create(float size = DefaultSize) => new()
        {
            FontSize = size,
            WordWrapping = CanvasWordWrapping.NoWrap,
            LineSpacingMode = CanvasLineSpacingMode.Uniform,
            LineSpacing = size,
            LineSpacingBaseline = size * 0.85f,
        };

        // Centred inside a bounding rectangle. The original positions button
        // captions with a bare `pos x,y` tuned to HSP's own font metrics;
        // reusing those coordinates verbatim pushes the text out of the button
        // at this font size, so buttons centre their labels instead.
        public static CanvasTextFormat CreateCentered(float size = DefaultSize) => new()
        {
            FontSize = size,
            WordWrapping = CanvasWordWrapping.NoWrap,
            HorizontalAlignment = CanvasHorizontalAlignment.Center,
            VerticalAlignment = CanvasVerticalAlignment.Center,
        };

        // Same size but allowed to wrap inside a bounding rectangle, for the
        // few screens that are laid out as paragraphs rather than fixed lines.
        public static CanvasTextFormat CreateWrapping(float size = DefaultSize) => new()
        {
            FontSize = size,
            WordWrapping = CanvasWordWrapping.Wrap,
            LineSpacingMode = CanvasLineSpacingMode.Uniform,
            LineSpacing = size,
            LineSpacingBaseline = size * 0.85f,
        };
    }
}
