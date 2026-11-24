param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$PreviewDirectory = ''
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$drawingReferences = @('System.Drawing')
if ($PSVersionTable.PSEdition -eq 'Core') {
    $drawingReferences = @('System.Drawing', 'System.Drawing.Common', 'System.Drawing.Primitives', 'System.Runtime', 'System.Collections')
    foreach ($assemblyName in @('System.Private.Windows.GdiPlus.dll', 'System.Private.Windows.Core.dll')) {
        $assemblyPath = Join-Path $PSHOME $assemblyName
        if (Test-Path -LiteralPath $assemblyPath) { $drawingReferences += $assemblyPath }
    }
}
if (-not ('CompanionIconBuilder' -as [type])) {
    Add-Type -ReferencedAssemblies $drawingReferences -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public static class CompanionIconBuilder {
    static GraphicsPath Rounded(float x, float y, float width, float height, float radius) {
        var path = new GraphicsPath(); float d = radius * 2;
        path.AddArc(x, y, d, d, 180, 90); path.AddArc(x + width - d, y, d, d, 270, 90);
        path.AddArc(x + width - d, y + height - d, d, d, 0, 90); path.AddArc(x, y + height - d, d, d, 90, 90);
        path.CloseFigure(); return path;
    }
    // Use the existing GPT character's first idle frame, cropped to her face and horns.
    // The lavender tile preserves contrast on both light and dark Explorer surfaces.
    static Bitmap Portrait(string sourcePath) {
        var portrait = new Bitmap(256, 256, PixelFormat.Format32bppArgb);
        using (var source = new Bitmap(sourcePath)) using (var graphics = Graphics.FromImage(portrait)) {
            graphics.Clear(Color.Transparent); graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var tile = Rounded(4, 4, 248, 248, 58)) {
                using (var fill = new SolidBrush(Color.FromArgb(247, 242, 255))) graphics.FillPath(fill, tile);
                graphics.SetClip(tile);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                float frameWidth = source.Width / 4f, frameHeight = source.Height / 2f;
                var crop = new RectangleF(frameWidth * .215f, 0, frameWidth * .66f, frameHeight * .66f);
                using (var attributes = new ImageAttributes()) {
                    attributes.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(source, new Rectangle(9, 10, 238, 238), crop.X, crop.Y, crop.Width, crop.Height, GraphicsUnit.Pixel, attributes);
                }
                graphics.ResetClip();
                using (var border = new Pen(Color.FromArgb(119, 86, 216), 7)) graphics.DrawPath(border, tile);
            }
        }
        return portrait;
    }
    static Bitmap Resize(Bitmap source, int size) {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap)) using (var attributes = new ImageAttributes()) {
            graphics.Clear(Color.Transparent); graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality; attributes.SetWrapMode(WrapMode.TileFlipXY);
            graphics.DrawImage(source, new Rectangle(0, 0, size, size), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
        }
        return bitmap;
    }
    static byte[] EncodeDib(Bitmap bitmap) {
        int size = bitmap.Width, maskStride = ((size + 31) / 32) * 4;
        using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream)) {
            writer.Write(40); writer.Write(size); writer.Write(size * 2); writer.Write((ushort)1); writer.Write((ushort)32);
            writer.Write(0); writer.Write(size * size * 4 + maskStride * size); writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(0);
            // ICO DIB rows are bottom-up BGRA, followed by the 1-bit transparency mask.
            for (int y = size - 1; y >= 0; y--) for (int x = 0; x < size; x++) {
                var color = bitmap.GetPixel(x, y); writer.Write(color.B); writer.Write(color.G); writer.Write(color.R); writer.Write(color.A);
            }
            for (int y = size - 1; y >= 0; y--) {
                var mask = new byte[maskStride];
                for (int x = 0; x < size; x++) if (bitmap.GetPixel(x, y).A == 0) mask[x / 8] |= (byte)(0x80 >> (x % 8));
                writer.Write(mask);
            }
            return stream.ToArray();
        }
    }
    public static void Create(string sourcePath, string outputPath, string previewDirectory) {
        int[] sizes = {16, 20, 24, 32, 40, 48, 64, 96, 128, 256};
        var frames = new List<byte[]>();
        using (var portrait = Portrait(sourcePath)) {
            foreach (int size in sizes) using (var bitmap = Resize(portrait, size)) {
                int visible = 0;
                for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) if (bitmap.GetPixel(x, y).A >= 128) visible++;
                if (visible < size * size / 2) throw new InvalidDataException("GPT app icon is unexpectedly transparent at " + size + "px.");
                if (!String.IsNullOrEmpty(previewDirectory)) bitmap.Save(Path.Combine(previewDirectory, "app-icon-" + size + ".png"), ImageFormat.Png);
                if (size == 256) using (var png = new MemoryStream()) { bitmap.Save(png, ImageFormat.Png); frames.Add(png.ToArray()); }
                else frames.Add(EncodeDib(bitmap));
            }
        }
        using (var stream = File.Create(outputPath)) using (var writer = new BinaryWriter(stream)) {
            writer.Write((ushort)0); writer.Write((ushort)1); writer.Write((ushort)sizes.Length);
            int offset = 6 + sizes.Length * 16;
            for (int n = 0; n < sizes.Length; n++) {
                writer.Write((byte)(sizes[n] == 256 ? 0 : sizes[n])); writer.Write((byte)(sizes[n] == 256 ? 0 : sizes[n]));
                writer.Write((byte)0); writer.Write((byte)0); writer.Write((ushort)1); writer.Write((ushort)32);
                writer.Write(frames[n].Length); writer.Write(offset); offset += frames[n].Length;
            }
            foreach (var frame in frames) writer.Write(frame);
        }
    }
}
'@
}
if ($PreviewDirectory) { New-Item -ItemType Directory -Path $PreviewDirectory -Force | Out-Null }
$sourcePath = Join-Path $ProjectRoot 'assets/pixel.png'
$iconPath = Join-Path $ProjectRoot 'assets/app.ico'
[CompanionIconBuilder]::Create($sourcePath, $iconPath, $PreviewDirectory)
Write-Output "Generated GPT app icon (16, 20, 24, 32, 40, 48, 64, 96, 128, 256 px): $iconPath"
