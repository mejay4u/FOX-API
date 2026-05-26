using System.Collections.Concurrent;
using System.Text.Json;
using IdCard.Domain.Interfaces;
using IdCard.Domain.Models;
using IdCard.Infrastructure.Options;
using IdCard.Infrastructure.Templates.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace IdCard.Infrastructure.Rendering;

/// <summary>
/// Thread-safe Singleton renderer.
///
/// Performance: all expensive resources are loaded once and cached —
///   • SKTypeface  — loaded in constructor, reused across all renders
///   • IdCardTemplate (parsed JSON) — cached by file path
///   • Background SKImage (decoded PNG) — cached by file path
///   • Back-image byte[] — cached by file path
///
/// After the first request per LOB there is zero disk I/O per render.
///
/// Text quality:
///   • SubpixelAntialias edging — LCD-quality edges, critical at 8–12 pt
///   • Subpixel=true — fractional-pixel positioning, eliminates spacing jitter
///   • Hinting=Full — crisp strokes at small sizes
///   • Baseline from font.Metrics.Ascent — exact for any typeface, not an approximation
///   • Line height from font metrics with a minimum leading floor
///   • Mitchell bicubic sampling for background — sharp at any scale
/// </summary>
public sealed class SkiaIdCardRenderer : IIdCardRenderer, IDisposable
{
    // ── Minimum inter-line gap as a fraction of the line box height.
    //    Used when the typeface reports zero Leading (common for system fonts).
    private const float MinLeadingRatio = 0.15f;

    // ── Extra multiplier applied between a header line and its value line.
    private const float HeaderValueGap = 1.10f;

    // ── Mitchell bicubic — sharp resampling for any scale factor.
    private static readonly SKSamplingOptions HQSampling = new(SKCubicResampler.Mitchell);

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    // ── Typefaces — loaded once from file (or system), shared across all renders.
    private readonly SKTypeface _regular;
    private readonly SKTypeface _bold;
    private readonly SKTypeface _italic;
    private readonly SKTypeface _boldItalic;

    // ── Per-path caches — populated on first access, read-only thereafter.
    //    Lazy<T> ensures the factory runs exactly once even under concurrent load.
    private readonly ConcurrentDictionary<string, Lazy<IdCardTemplate>> _templateCache = new();
    private readonly ConcurrentDictionary<string, Lazy<SKImage>>        _bgCache       = new();
    private readonly ConcurrentDictionary<string, Lazy<byte[]>>         _backCache     = new();

    private readonly IBindingResolver _binding;
    private readonly IQrCodeService   _qrCode;
    private readonly string           _assetsRoot;
    private readonly ILogger<SkiaIdCardRenderer> _logger;

    public SkiaIdCardRenderer(
        IBindingResolver binding,
        IQrCodeService qrCode,
        IOptions<IdCardOptions> options,
        ILogger<SkiaIdCardRenderer> logger)
    {
        _binding    = binding;
        _qrCode     = qrCode;
        _assetsRoot = Path.Combine(options.Value.BasePath, "Assets");
        _logger     = logger;

        var fontsRoot = Path.Combine(options.Value.BasePath, "Fonts");
        _regular    = LoadTypeface(fontsRoot, bold: false, italic: false);
        _bold       = LoadTypeface(fontsRoot, bold: true,  italic: false);
        _italic     = LoadTypeface(fontsRoot, bold: false, italic: true);
        _boldItalic = LoadTypeface(fontsRoot, bold: true,  italic: true);
    }

    // ─────────────────────────────────────────────────────────────────
    // Entry point
    // ─────────────────────────────────────────────────────────────────

    public Task<IdCardResult> RenderAsync(string templatePath, IdCardContext context)
    {
        // Skia rendering is purely CPU-bound. Offload from the ASP.NET thread pool
        // so it does not block I/O threads while the render runs.
        return Task.Run(() =>
        {
            var template = GetTemplate(templatePath);
            var assetDir = Path.Combine(_assetsRoot, context.Lob.ToUpperInvariant(), template.TemplateCode);

            return new IdCardResult
            {
                FrontImageBytes = RenderFront(template, context, assetDir),
                BackImageBytes  = GetBack(template, assetDir)
            };
        });
    }

    // ─────────────────────────────────────────────────────────────────
    // Front rendering
    // ─────────────────────────────────────────────────────────────────

    private byte[] RenderFront(IdCardTemplate template, IdCardContext context, string assetDir)
    {
        var bgImage = GetBackground(Path.Combine(assetDir, template.Front.BackgroundAsset));

        var info = new SKImageInfo(bgImage.Width, bgImage.Height,
                                   SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        // Draw background at its natural size — Mitchell bicubic for sharp result.
        canvas.DrawImage(bgImage,
            new SKRect(0, 0, bgImage.Width, bgImage.Height), HQSampling);

        foreach (var el in template.Front.Elements)
        {
            try
            {
                if (el.Type.Equals("image", StringComparison.OrdinalIgnoreCase))
                    RenderImageElement(canvas, el, context);
                else
                    RenderTextElement(canvas, el, context);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipping element at ({X},{Y}): {Msg}", el.X, el.Y, ex.Message);
            }
        }

        // surface.Snapshot() flushes implicitly — canvas.Flush() is redundant.
        using var snapshot = surface.Snapshot();
        using var encoded  = snapshot.Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }

    // ─────────────────────────────────────────────────────────────────
    // Text rendering — four cases, no LOB logic, no switch-case
    // ─────────────────────────────────────────────────────────────────

    private void RenderTextElement(SKCanvas canvas, TemplateElement el, IdCardContext context)
    {
        var color = ParseColor(el.Color);
        using var paint = new SKPaint { Color = color, IsAntialias = true };

        // ── Case 1: Static text (literal label, no binding) ──────────
        if (!string.IsNullOrEmpty(el.StaticText))
        {
            using var font = MakeFont(SelectTypeface(el.Bold, el.Italic), el.FontSize);
            DrawLines(canvas, el.StaticText, el.X,
                      BaselineY(el.Y, font), el.Width, LineH(font), el.Wrap, font, paint);
            return;
        }

        var value = el.Binding is not null
            ? _binding.Resolve(el.Binding, context)
            : string.Empty;

        // ── Case 2: Inline — "Header: value" on one line ─────────────
        if (el.Inline)
        {
            using var hf = MakeFont(SelectTypeface(el.HeaderBold, el.HeaderItalic), el.FontSize);
            using var rf = MakeFont(SelectTypeface(el.Bold,       el.Italic),       el.FontSize);
            // Use the taller of the two for baseline so both fonts sit on the same line.
            float by = BaselineY(el.Y, hf);

            if (!string.IsNullOrEmpty(el.Header))
            {
                var headerText = el.Header + el.HeaderSeparator;
                canvas.DrawText(headerText, el.X, by, hf, paint);
                canvas.DrawText(value, el.X + hf.MeasureText(headerText), by, rf, paint);
            }
            else
            {
                canvas.DrawText(value, el.X, by, rf, paint);
            }
            return;
        }

        // ── Case 3: Header on first line, value on subsequent lines ──
        if (!string.IsNullOrEmpty(el.Header))
        {
            using var bf = MakeFont(SelectTypeface(el.HeaderBold, el.HeaderItalic), el.FontSize);
            float headerBaseline = BaselineY(el.Y, bf);
            canvas.DrawText(el.Header, el.X, headerBaseline, bf, paint);

            if (!string.IsNullOrEmpty(value))
            {
                using var vf = MakeFont(SelectTypeface(el.Bold, el.Italic), el.FontSize);
                // Advance by bold line height × gap so header and value breathe slightly.
                float valueStart = headerBaseline + LineH(bf) * HeaderValueGap;
                DrawLines(canvas, value, el.X, valueStart, el.Width, LineH(vf), el.Wrap, vf, paint);
            }
            return;
        }

        // ── Case 4: Value only (no header) ───────────────────────────
        if (!string.IsNullOrEmpty(value))
        {
            using var vf = MakeFont(SelectTypeface(el.Bold, el.Italic), el.FontSize);
            DrawLines(canvas, value, el.X,
                      BaselineY(el.Y, vf), el.Width, LineH(vf), el.Wrap, vf, paint);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Image element
    // ─────────────────────────────────────────────────────────────────

    private void RenderImageElement(SKCanvas canvas, TemplateElement el, IdCardContext context)
    {
        byte[]? bytes = null;

        if (el.Binding?.Equals("QRCode", StringComparison.OrdinalIgnoreCase) == true)
        {
            if (!string.IsNullOrWhiteSpace(context.QrData))
                bytes = _qrCode.Generate(context.QrData);
        }
        else if (el.Binding is not null)
        {
            var resolved = _binding.Resolve(el.Binding, context);
            if (!string.IsNullOrEmpty(resolved) && File.Exists(resolved))
                bytes = File.ReadAllBytes(resolved);
        }

        if (bytes is null || bytes.Length == 0) return;

        using var data   = SKData.CreateCopy(bytes);
        using var bitmap = SKBitmap.Decode(data);
        if (bitmap is null) return;

        using var img = SKImage.FromBitmap(bitmap);
        canvas.DrawImage(img,
            new SKRect(el.X, el.Y, el.X + el.Width, el.Y + el.Height), HQSampling);
    }

    // ─────────────────────────────────────────────────────────────────
    // Back image — raw bytes, never re-rendered
    // ─────────────────────────────────────────────────────────────────

    private byte[] GetBack(IdCardTemplate template, string assetDir)
    {
        var path = Path.Combine(assetDir, template.Back.BackgroundAsset);
        return _backCache
            .GetOrAdd(path, p => new Lazy<byte[]>(() =>
            {
                if (File.Exists(p)) return File.ReadAllBytes(p);
                _logger.LogWarning("Back asset not found: {Path} — using placeholder.", p);
                return GeneratePlaceholderBytes();
            }, isThreadSafe: true))
            .Value;
    }

    // ─────────────────────────────────────────────────────────────────
    // Font helpers — quality settings applied in one place
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates an SKFont with high-quality rendering settings.
    /// SubpixelAntialias = LCD-quality edges (critical at 8–12 pt).
    /// Subpixel = true   = fractional pixel positioning, eliminates spacing jitter.
    /// Hinting = Full    = crisp stroke geometry at small sizes.
    /// Caller owns and must dispose.
    /// </summary>
    private SKTypeface SelectTypeface(bool bold, bool italic) => (bold, italic) switch
    {
        (true,  true)  => _boldItalic,
        (true,  false) => _bold,
        (false, true)  => _italic,
        _              => _regular,
    };

    private static SKFont MakeFont(SKTypeface typeface, float size) =>
        new(typeface, size)
        {
            Edging   = SKFontEdging.SubpixelAntialias,
            Subpixel = true,
            Hinting  = SKFontHinting.Full,
        };

    /// <summary>
    /// Converts the template's top-Y coordinate to SkiaSharp's baseline Y.
    /// Uses actual font metrics (not a fixed ratio) so any typeface is positioned correctly.
    /// font.Metrics.Ascent is always negative in SkiaSharp (distance above baseline).
    /// Therefore: baselineY = topY - Ascent = topY + |Ascent|.
    /// </summary>
    private static float BaselineY(float topY, SKFont font)
        => topY - font.Metrics.Ascent;

    /// <summary>
    /// Line height derived from actual font metrics.
    /// Uses the typeface's Leading if present; falls back to 15% of line box height
    /// so lines always have breathing room even for fonts that report Leading = 0.
    /// </summary>
    private static float LineH(SKFont font)
    {
        var m       = font.Metrics;
        float box   = m.Descent - m.Ascent;          // total glyph height (Ascent is negative)
        float lead  = MathF.Max(m.Leading, box * MinLeadingRatio);
        return box + lead;
    }

    /// <summary>
    /// Draws text across multiple lines.
    /// Splits on explicit '\n' first, then word-wraps each segment within maxWidth.
    /// All Y values are baseline positions (SkiaSharp convention).
    /// </summary>
    private static void DrawLines(
        SKCanvas canvas, string text,
        float x, float baselineY, float maxWidth, float lineH, bool wrap,
        SKFont font, SKPaint paint)
    {
        float curY = baselineY;
        foreach (var segment in text.Split('\n'))
        {
            if (wrap)
            {
                foreach (var line in WordWrap(segment, font, maxWidth))
                {
                    canvas.DrawText(line, x, curY, font, paint);
                    curY += lineH;
                }
            }
            else
            {
                canvas.DrawText(segment, x, curY, font, paint);
                curY += lineH;
            }
        }
    }

    /// <summary>
    /// Word-wraps text to fit within maxWidth.
    /// Measures each word exactly once (not growing candidate strings),
    /// then accumulates advances — O(n) MeasureText calls instead of O(n²).
    /// </summary>
    private static IEnumerable<string> WordWrap(string text, SKFont font, float maxWidth)
    {
        if (string.IsNullOrWhiteSpace(text)) { yield return text; yield break; }

        var   words      = text.Split(' ');
        var   wordWidths = Array.ConvertAll(words, w => font.MeasureText(w));
        float spaceW     = font.MeasureText(" ");

        float lineW    = 0f;
        int   lineStart = 0;

        for (int i = 0; i < words.Length; i++)
        {
            float needed = lineW == 0f
                ? wordWidths[i]
                : lineW + spaceW + wordWidths[i];

            if (needed > maxWidth && lineW > 0f)
            {
                yield return string.Join(" ", words[lineStart..i]);
                lineStart = i;
                lineW     = wordWidths[i];
            }
            else
            {
                lineW = needed;
            }
        }

        if (lineStart < words.Length)
            yield return string.Join(" ", words[lineStart..]);
    }

    // ─────────────────────────────────────────────────────────────────
    // Cache loaders — each called exactly once per unique path
    // ─────────────────────────────────────────────────────────────────

    private IdCardTemplate GetTemplate(string path) =>
        _templateCache
            .GetOrAdd(path, p => new Lazy<IdCardTemplate>(() =>
            {
                var json = File.ReadAllText(p);
                return JsonSerializer.Deserialize<IdCardTemplate>(json, JsonOpts)
                       ?? throw new InvalidDataException($"Could not deserialize: {p}");
            }, isThreadSafe: true))
            .Value;

    private SKImage GetBackground(string path) =>
        _bgCache
            .GetOrAdd(path, p => new Lazy<SKImage>(() =>
            {
                if (File.Exists(p))
                {
                    using var data = SKData.Create(p);
                    using var bmp  = SKBitmap.Decode(data);
                    if (bmp is not null) return SKImage.FromBitmap(bmp);
                }
                _logger.LogWarning("Background not found: {Path} — using placeholder.", p);
                return CreatePlaceholderSKImage();
            }, isThreadSafe: true))
            .Value;

    private static SKTypeface LoadTypeface(string fontsRoot, bool bold, bool italic)
    {
        var fileName = (bold, italic) switch
        {
            (true,  true)  => "bolditalic.ttf",
            (true,  false) => "bold.ttf",
            (false, true)  => "italic.ttf",
            _              => "regular.ttf",
        };
        var file = Path.Combine(fontsRoot, fileName);
        if (File.Exists(file)) return SKTypeface.FromFile(file);

        var style = (bold, italic) switch
        {
            (true,  true)  => SKFontStyle.BoldItalic,
            (true,  false) => SKFontStyle.Bold,
            (false, true)  => SKFontStyle.Italic,
            _              => SKFontStyle.Normal,
        };
        return SKTypeface.FromFamilyName("Arial", style) ?? SKTypeface.Default;
    }

    private static SKColor ParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return SKColors.Black;
        return SKColor.TryParse(hex, out var c) ? c : SKColors.Black;
    }

    // ── Placeholder — only used when actual card assets are absent ────

    private static SKImage CreatePlaceholderSKImage()
    {
        const int W = 638, H = 400;
        using var bmp = new SKBitmap(W, H);
        using var c   = new SKCanvas(bmp);
        c.Clear(SKColors.White);

        using var border = new SKPaint
        {
            Color       = new SKColor(0x2B, 0x5F, 0xBE),
            IsStroke    = true,
            StrokeWidth = 6,
            IsAntialias = true
        };
        c.DrawRoundRect(new SKRoundRect(new SKRect(3, 3, W - 3, H - 3), 16), border);

        using var header = new SKPaint { Color = new SKColor(0x2B, 0x5F, 0xBE) };
        c.DrawRect(new SKRect(0, 0, W, 50), header);
        c.Flush();

        return SKImage.FromBitmap(bmp);
    }

    private static byte[] GeneratePlaceholderBytes()
    {
        using var img     = CreatePlaceholderSKImage();
        using var encoded = img.Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }

    // ─────────────────────────────────────────────────────────────────
    // Disposal — Singleton lives for app lifetime; clean up native handles at shutdown
    // ─────────────────────────────────────────────────────────────────

    public void Dispose()
    {
        _regular.Dispose();
        _bold.Dispose();
        _italic.Dispose();
        _boldItalic.Dispose();
        foreach (var lazy in _bgCache.Values)
        {
            if (lazy.IsValueCreated) lazy.Value.Dispose();
        }
    }
}
