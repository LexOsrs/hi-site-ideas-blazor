using SkiaSharp;
using hi_site_ideas_blazor.Models;

namespace hi_site_ideas_blazor.Services;

public class BoardImageService
{
    private readonly HttpClient _http;

    public BoardImageService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; HISiteBingo/1.0)");
    }
    private readonly Dictionary<string, SKBitmap?> _imageCache = new();

    async Task<SKBitmap?> FetchImage(string? url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        if (_imageCache.TryGetValue(url, out var cached)) return cached;

        try
        {
            var data = await _http.GetByteArrayAsync(url);
            var bmp = SKBitmap.Decode(data);
            _imageCache[url] = bmp;
            return bmp;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to fetch image {url}: {ex.Message}");
            _imageCache[url] = null;
            return null;
        }
    }

    // Match site CSS variables exactly
    static readonly SKColor BgColor = SKColor.Parse("#0f0d09");
    static readonly SKColor BgElevated = SKColor.Parse("#241f17");
    static readonly SKColor Border = SKColor.Parse("#3d3324");
    static readonly SKColor BorderLight = SKColor.Parse("#5c4a32");
    static readonly SKColor Gold = SKColor.Parse("#ff981f");
    static readonly SKColor GoldDim = SKColor.Parse("#b8860b");
    static readonly SKColor TextColor = SKColor.Parse("#c8b89a");
    static readonly SKColor TextDim = SKColor.Parse("#8a7a60");
    static readonly SKColor TextBright = SKColor.Parse("#e8d8b8");
    static readonly SKColor Green = SKColor.Parse("#4ade80");

    public async Task<byte[]> RenderBoard(BingoEvent ev, BingoTeam team)
    {
        var size = ev.BoardSize;
        var cellSize = size <= 3 ? 140 : 120;
        var gap = 3;
        var margin = 12;

        var boardW = cellSize * size + gap * (size - 1);
        var imgW = boardW + margin * 2;
        var imgH = boardW + margin * 2;

        using var surface = SKSurface.Create(new SKImageInfo(imgW, imgH));
        var canvas = surface.Canvas;
        canvas.Clear(BgColor);

        // Fonts
        var fontFamily = SKTypeface.FromFamilyName("Inter") ?? SKTypeface.FromFamilyName("Segoe UI") ?? SKTypeface.Default;
        var fontBold = SKTypeface.FromFamilyName("Inter", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright) ?? fontFamily;
        var fontSemiBold = SKTypeface.FromFamilyName("Inter", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright) ?? fontFamily;

        // Prefetch tile images
        var tileImages = new Dictionary<int, SKBitmap?>();
        foreach (var tile in ev.Tiles)
            tileImages[tile.Position] = await FetchImage(tile.ImageUrl);

        // Build completion state
        var completedPositions = new HashSet<int>();
        var tileState = new Dictionary<int, string>(); // position -> "complete" | "progress" | "none"

        foreach (var tile in ev.Tiles)
        {
            var tt = tile.TeamTiles.FirstOrDefault(t => t.BingoTeamId == team.Id);
            var approved = tt?.GetApprovedSubmissions() ?? Enumerable.Empty<BingoSubmission>();
            var complete = tile.HasRequirements && tile.IsComplete(approved);
            var hasProgress = approved.Any(s => s.Entries.Any(e => e.Amount > 0));
            tileState[tile.Position] = complete ? "complete" : hasProgress ? "progress" : "none";
            if (complete) completedPositions.Add(tile.Position);
        }

        // Draw cells
        var boardY = margin;
        foreach (var tile in ev.Tiles.OrderBy(t => t.Position))
        {
            var row = tile.Position / size;
            var col = tile.Position % size;
            var x = margin + col * (cellSize + gap);
            var y = boardY + row * (cellSize + gap);
            var state = tileState.GetValueOrDefault(tile.Position, "none");

            DrawCell(canvas, x, y, cellSize, tile, state, fontFamily, fontSemiBold, tileImages.GetValueOrDefault(tile.Position));
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    void DrawCell(SKCanvas canvas, float x, float y, int cellSize, BingoTile tile, string state, SKTypeface font, SKTypeface fontSemiBold, SKBitmap? image)
    {
        var (bgColor, borderColor) = state switch
        {
            "complete" => (SKColor.Parse("#122e1e"), Green),
            "progress" => (SKColor.Parse("#2e2816"), GoldDim),
            _ => (BgElevated, Border),
        };

        var borderWidth = state == "complete" ? 2.5f : 1.5f;
        var radius = 8f;
        var rect = new SKRoundRect(new SKRect(x, y, x + cellSize, y + cellSize), radius);

        // Background
        using (var paint = new SKPaint { Color = bgColor, IsAntialias = true, Style = SKPaintStyle.Fill })
            canvas.DrawRoundRect(rect, paint);

        // Border
        if (state == "progress")
        {
            using var paint = new SKPaint
            {
                Color = GoldDim, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f,
                PathEffect = SKPathEffect.CreateDash([4, 6], 0),
            };
            canvas.DrawRoundRect(rect, paint);
        }
        else
        {
            using var paint = new SKPaint { Color = borderColor, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = borderWidth };
            canvas.DrawRoundRect(rect, paint);
        }

        // Points (top right)
        using (var paint = new SKPaint { Color = GoldDim, IsAntialias = true, TextSize = 10, Typeface = font })
        {
            var ptsText = $"{tile.Points} pt";
            var ptsW = paint.MeasureText(ptsText);
            canvas.DrawText(ptsText, x + cellSize - ptsW - 8, y + 14, paint);
        }

        // Calculate content layout: points top-right, then [image] + [title] centered as a group
        var imgSize = 40f;
        var titleColor = state == "complete" ? Green : TextColor;
        var lines = WrapText(tile.Title, cellSize > 130 ? 16 : 14);
        var lineHeight = 15f;
        var titleH = lines.Count * lineHeight;
        var gapBetween = image != null ? 6f : 0f;
        var imageH = image != null ? imgSize : 0f;
        var contentH = imageH + gapBetween + titleH;
        var contentStartY = y + (cellSize - contentH) / 2;

        // Draw image (maintain aspect ratio, fit within imgSize box)
        if (image != null)
        {
            var scale = Math.Min(imgSize / image.Width, imgSize / image.Height);
            var drawW = image.Width * scale;
            var drawH = image.Height * scale;
            var destRect = new SKRect(
                x + (cellSize - drawW) / 2, contentStartY + (imgSize - drawH) / 2,
                x + (cellSize + drawW) / 2, contentStartY + (imgSize + drawH) / 2);
            using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
            canvas.DrawBitmap(image, destRect, paint);
        }

        // Draw title
        var textY = contentStartY + imageH + gapBetween + 12; // +12 for text baseline
        using (var paint = new SKPaint { Color = titleColor, IsAntialias = true, TextSize = 12, Typeface = fontSemiBold })
        {
            foreach (var line in lines)
            {
                var lineW = paint.MeasureText(line);
                canvas.DrawText(line, x + (cellSize - lineW) / 2, textY, paint);
                textY += lineHeight;
            }
        }

        // Completed overlay with checkmark drawn as path
        if (state == "complete")
        {
            // Semi-transparent overlay
            using (var paint = new SKPaint { Color = new SKColor(0, 0, 0, 100), IsAntialias = true })
                canvas.DrawRoundRect(rect, paint);

            // Draw checkmark as a path (no font dependency)
            var cx = x + cellSize / 2;
            var cy = y + cellSize / 2;
            var s = cellSize * 0.18f;
            using var path = new SKPath();
            path.MoveTo(cx - s, cy);
            path.LineTo(cx - s * 0.3f, cy + s * 0.7f);
            path.LineTo(cx + s, cy - s * 0.6f);
            using var paint2 = new SKPaint
            {
                Color = Green, IsAntialias = true, Style = SKPaintStyle.Stroke,
                StrokeWidth = 4f, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round,
            };
            canvas.DrawPath(path, paint2);
        }
    }

    static int CountLines(HashSet<int> completed, int size)
    {
        int count = 0;
        for (int r = 0; r < size; r++)
            if (Enumerable.Range(0, size).All(c => completed.Contains(r * size + c))) count++;
        for (int c = 0; c < size; c++)
            if (Enumerable.Range(0, size).All(r => completed.Contains(r * size + c))) count++;
        if (Enumerable.Range(0, size).All(i => completed.Contains(i * size + i))) count++;
        if (Enumerable.Range(0, size).All(i => completed.Contains(i * size + (size - 1 - i)))) count++;
        return count;
    }

    static List<string> WrapText(string text, int maxChars)
    {
        var result = new List<string>();
        var words = text.Split(' ');
        var line = "";
        foreach (var w in words)
        {
            if (line.Length > 0 && line.Length + 1 + w.Length > maxChars)
            {
                result.Add(line);
                line = w;
            }
            else
            {
                line = line.Length > 0 ? $"{line} {w}" : w;
            }
        }
        if (line.Length > 0) result.Add(line);
        return result;
    }
}
