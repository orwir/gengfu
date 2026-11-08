#:property TargetFramework=net10.0-windows
#:property UseWPF=true
#:property OutputType=Exe
#:property PublishAot=false
// Draws Gengfu's icon, the title bar's gong mark in Windows blue, into src/Gengfu.App/Gengfu.ico.
// Up to 32 px the mark has a simpler drawing of its own, so its strokes stay crisp. The design is
// the canvas's App icon, direction A; design_language.md says where it shows.
//   dotnet run scripts/make-icon.cs
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

var target = Path.Combine(AppContext.GetData("EntryPointFileDirectoryPath") as string ?? "scripts", "..", "src", "Gengfu.App", "Gengfu.ico");
var images = new List<(int Size, byte[] Data)>();
var thread = new Thread(() =>
{
    foreach (var size in new[] { 16, 20, 24, 32, 40, 48, 64, 256 })
    {
        var pixels = Draw(size);
        // Windows reads PNG at 256 px; smaller sizes go in the classic bitmap form every reader takes.
        images.Add((size, size == 256 ? Png(pixels) : Dib(pixels, size)));
    }
});
thread.SetApartmentState(ApartmentState.STA);
thread.Start();
thread.Join();

using (var file = new BinaryWriter(File.Create(target)))
{
    file.Write((short)0);
    file.Write((short)1);
    file.Write((short)images.Count);
    var offset = 6 + (16 * images.Count);
    foreach (var (size, data) in images)
    {
        file.Write((byte)(size == 256 ? 0 : size));
        file.Write((byte)(size == 256 ? 0 : size));
        file.Write((byte)0);
        file.Write((byte)0);
        file.Write((short)1);
        file.Write((short)32);
        file.Write(data.Length);
        file.Write(offset);
        offset += data.Length;
    }
    foreach (var (_, data) in images)
    {
        file.Write(data);
    }
}
Console.WriteLine($"Wrote {Path.GetFullPath(target)}: {string.Join(", ", images.Select(image => image.Size))} px.");

static BitmapSource Draw(int size)
{
    var blue = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4));
    var small = size <= 32;
    // The large drawing is on a 256 grid, the small one on a 16 grid: hanger, rim and boss.
    var scale = size / (small ? 16.0 : 256.0);
    var stroke = new Pen(blue, (small ? 1.75 : 24) * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
    var (top, bottom, centre, rim, boss) = small ? (1.25, 3.75, 9.5, 5.0, 1.75) : (24.0, 60.0, 150.0, 80.0, 28.0);
    var middle = (small ? 8.0 : 128.0) * scale;
    var visual = new DrawingVisual();
    using (var context = visual.RenderOpen())
    {
        context.DrawLine(stroke, new Point(middle, top * scale), new Point(middle, bottom * scale));
        context.DrawEllipse(null, stroke, new Point(middle, centre * scale), rim * scale, rim * scale);
        context.DrawEllipse(blue, null, new Point(middle, centre * scale), boss * scale, boss * scale);
    }
    var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
    bitmap.Render(visual);
    return new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
}

static byte[] Png(BitmapSource pixels)
{
    var encoder = new PngBitmapEncoder();
    encoder.Frames.Add(BitmapFrame.Create(pixels));
    using var stream = new MemoryStream();
    encoder.Save(stream);
    return stream.ToArray();
}

// A 32-bit DIB: the header, the colour rows bottom-up, then a 1-bit mask, set where it's transparent.
static byte[] Dib(BitmapSource pixels, int size)
{
    var stride = size * 4;
    var bgra = new byte[stride * size];
    pixels.CopyPixels(bgra, stride, 0);
    var maskStride = (size + 31) / 32 * 4;
    using var stream = new MemoryStream();
    using var writer = new BinaryWriter(stream);
    writer.Write(40);
    writer.Write(size);
    writer.Write(size * 2);
    writer.Write((short)1);
    writer.Write((short)32);
    writer.Write(0);
    writer.Write(stride * size + maskStride * size);
    writer.Write(0L);
    writer.Write(0L);
    for (var row = size - 1; row >= 0; row--)
    {
        writer.Write(bgra, row * stride, stride);
    }
    for (var row = size - 1; row >= 0; row--)
    {
        var mask = new byte[maskStride];
        for (var x = 0; x < size; x++)
        {
            if (bgra[(row * stride) + (x * 4) + 3] == 0)
            {
                mask[x / 8] |= (byte)(0x80 >> (x % 8));
            }
        }
        writer.Write(mask);
    }
    return stream.ToArray();
}
