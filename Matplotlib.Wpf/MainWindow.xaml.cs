using System.Buffers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Python.Runtime;

namespace Matplotlib.Net;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        var txtbx  = (TextBox)FindName("Script");
        using var _ = Py.GIL();
        using var scope = Py.CreateScope();
        scope.Exec("import matplotlib.pyplot as plt");
        scope.Exec(txtbx.Text);
    }
}

public class MplPanel : Panel
{
    private static PyObject AggRenderer;
    private static PyObject FigManager;
    private static string Testcase = @"
import numpy as np
nrows = 3
ncols = 5
Z = np.arange(nrows * ncols).reshape(nrows, ncols)
x = np.arange(ncols + 1)
y = np.arange(nrows + 1)

#fig, ax = plt.subplots()
plt.pcolormesh(x, y, Z, shading='flat', vmin=Z.min(), vmax=Z.max())


def _annotate(ax, x, y, title):
    # this all gets repeated below:
    X, Y = np.meshgrid(x, y)
    ax.plot(X.flat, Y.flat, 'o', color='m')
    ax.set_xlim(-0.7, 5.2)
    ax.set_ylim(-0.7, 3.2)
    ax.set_title(title)

#_annotate(ax, x, y, ""shading='flat'"")
";
    
    
    private static ArrayPool<byte> pool = ArrayPool<byte>.Create();

    static MplPanel()
    {
        Pythonnet.Init();
        using var _ = Py.GIL();
        using var scope = Py.CreateScope();
        scope.Exec("from matplotlib.backends.backend_agg import FigureCanvasAgg");
        scope.Exec("import matplotlib.pyplot as plt");
        scope.Exec("import logging");
        scope.Exec("import matplotlib");
        //scope.Exec("matplotlib.rcParams[\"backend\"] = \"Agg\"");
        scope.Exec("logging.basicConfig(level=logging.DEBUG)");
        var canvas = scope.Eval("plt.get_current_fig_manager().canvas");
        FigManager = scope.Eval("plt.get_current_fig_manager()");
        AggRenderer = scope.Eval("canvas.switch_backends(FigureCanvasAgg)",
            new Py.KeywordArguments() { ["canvas"] = canvas });
        scope.Exec("agg.mpl_connect(\"key_press_event\", lambda ev: logging.debug(ev.key))", new Py.KeywordArguments { ["agg"] = AggRenderer });
        scope.Exec("plt.ion()");
        scope.Exec(Testcase);
    }

    public MplPanel()
    {
        SizeChanged += OnSizeChanged;
        KeyUp += HandleKeyUpEvent;
        KeyDown += HandleKeyDown;
        MouseMove += HandleMouseMove;
        MouseLeftButtonDown += HandleMouseDown;
        MouseLeftButtonUp += HandleMouseUp;
        MouseRightButtonUp += (sender, args) =>
        {
            using var _ = Py.GIL();
            using var scope = Py.CreateScope();
            scope.Exec("figmanager.toolbar.pan()", new Py.KeywordArguments { ["figmanager"] = FigManager });
        };
    }

    private void HandleMouseMove(object sender, MouseEventArgs e)
    {
        using var _ = Py.GIL();
        using var scope = Py.CreateScope();
        var pos = e.GetPosition(this);
        var posl = new PyList();
        var bboxheight = scope.Eval("agg.figure.bbox.height - y", new Py.KeywordArguments { ["agg"] = AggRenderer , ["y"] = pos.Y.ToPython()});
        posl.Append(pos.X.ToPython());
        posl.Append(bboxheight);
        scope.Exec("from matplotlib.backend_bases import MouseEvent");
        scope.Exec("MouseEvent(\"motion_notify_event\", agg, *coords)._process()", new Py.KeywordArguments { ["agg"] = AggRenderer , ["coords"] = posl});
        InvalidateVisual();
    }

    private void HandleMouseDown(object sender, MouseEventArgs e)
    {
        using var _ = Py.GIL();
        using var scope = Py.CreateScope();
        var pos = e.GetPosition(this);
        var posl = new PyList();
        var bboxheight = scope.Eval("agg.figure.bbox.height - y", new Py.KeywordArguments { ["agg"] = AggRenderer , ["y"] = pos.Y.ToPython()});
        posl.Append(pos.X.ToPython());
        posl.Append(bboxheight);
        scope.Exec("from matplotlib.backend_bases import MouseEvent, MouseButton");
        scope.Exec("MouseEvent(\"button_press_event\", agg, *coords, MouseButton.LEFT)._process()", new Py.KeywordArguments { ["agg"] = AggRenderer , ["coords"] = posl});
        InvalidateVisual();
    }

    private void HandleMouseUp(object sender, MouseEventArgs e)
    {
        using var _ = Py.GIL();
        using var scope = Py.CreateScope();
        var pos = e.GetPosition(this);
        var posl = new PyList();
        var bboxheight = scope.Eval("agg.figure.bbox.height - y", new Py.KeywordArguments { ["agg"] = AggRenderer , ["y"] = pos.Y.ToPython()});
        posl.Append(pos.X.ToPython());
        posl.Append(bboxheight);
        scope.Exec("from matplotlib.backend_bases import MouseEvent, MouseButton");
        scope.Exec("MouseEvent(\"button_release_event\", agg, *coords, MouseButton.LEFT)._process()", new Py.KeywordArguments { ["agg"] = AggRenderer , ["coords"] = posl});
        InvalidateVisual();
    }

    private void HandleKeyDown(object sender, KeyEventArgs e)
    {
        using var _ = Py.GIL();
        using var scope = Py.CreateScope();
        scope.Exec("from matplotlib.backend_bases import KeyEvent");
        scope.Exec("KeyEvent(\"key_press_event\", agg, key)._process()", new Py.KeywordArguments { ["agg"] = AggRenderer, ["key"] = e.Key.ToString().ToPython() });
        InvalidateVisual();
    }

    private void HandleKeyUpEvent(object sender, KeyEventArgs e)
    {
        using var _ = Py.GIL();
        using var scope = Py.CreateScope();
        scope.Exec("from matplotlib.backend_bases import KeyEvent");
        scope.Exec("KeyEvent(\"key_release_event\", agg, key)._process()", new Py.KeywordArguments { ["agg"] = AggRenderer, ["key"] = e.Key.ToString().ToPython() });
    }

    void OnSizeChanged(object sender, EventArgs e)
    {
        var w = ActualWidth;
        var h = ActualHeight;
        if (w <= 0 || h <= 0)
            return;
        using var _ = Py.GIL();
        using var scope = Py.CreateScope();
        var dpi = scope.Eval("agg.figure.dpi", new Py.KeywordArguments { ["agg"] = AggRenderer }).As<double>();
        scope.Exec("agg.figure.set_size_inches(w, h, forward=False)", new Py.KeywordArguments { ["agg"] = AggRenderer, ["h"] = (h/dpi).ToPython(), ["w"] = (w/dpi).ToPython()});
        scope.Exec("from matplotlib.backend_bases import ResizeEvent");
        scope.Exec("ResizeEvent(\"resize_event\", agg)._process()", new Py.KeywordArguments { ["agg"] = AggRenderer });
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        using var _ = Py.GIL();
        using var scope = Py.CreateScope();
        scope.Exec("agg.draw()", new Py.KeywordArguments { ["agg"] = AggRenderer });
        var buf = scope.Eval("agg.buffer_rgba()", new Py.KeywordArguments { ["agg"] = AggRenderer });
        var shape = buf.GetAttr("shape");
        var h = shape[0].As<int>();
        var w = shape[1].As<int>();
        var arr = pool.Rent(w * h * 4);
        using var pyBuf = buf.GetBuffer();
        pyBuf.Read(arr, 0, w * h * 4 - 1, 0);
        var img =  new RgbaBitmapSource(arr, w);
        dc.DrawImage(img, new Rect(0, 0, w, h));
        pool.Return(arr);
    }
}

/// <summary>
/// Taken from <see href="https://stackoverflow.com/questions/21428272/show-rgba-image-from-memory">
/// </summary>
public class RgbaBitmapSource : BitmapSource
{
    private byte[] rgbaBuffer;
    private int pixelWidth;
    private int pixelHeight;

    public RgbaBitmapSource(byte[] rgbaBuffer, int pixelWidth)
    {
        this.rgbaBuffer = rgbaBuffer;
        this.pixelWidth = pixelWidth;
        this.pixelHeight = rgbaBuffer.Length / (4 * pixelWidth);
    }
    
    unsafe public override void CopyPixels(
        Int32Rect sourceRect, Array pixels, int stride, int offset)
    {
        fixed (byte* source = rgbaBuffer, destination = (byte[])pixels)
        {
            byte* dstPtr = destination + offset;

            for (int y = sourceRect.Y; y < sourceRect.Y + sourceRect.Height; y++)
            {
                for (int x = sourceRect.X; x < sourceRect.X + sourceRect.Width; x++)
                {
                    byte* srcPtr = source + stride * y + 4 * x;
                    byte a = *(srcPtr + 3);
                    *(dstPtr++) = (byte)(*(srcPtr + 2) * a / 256); // pre-multiplied B
                    *(dstPtr++) = (byte)(*(srcPtr + 1) * a / 256); // pre-multiplied G
                    *(dstPtr++) = (byte)(*srcPtr * a / 256); // pre-multiplied R
                    *(dstPtr++) = a;
                }
            }
        }
    }

    protected override Freezable CreateInstanceCore()
    {
        return new RgbaBitmapSource(rgbaBuffer, pixelWidth);
    }

    public override event EventHandler<DownloadProgressEventArgs> DownloadProgress;
    public override event EventHandler DownloadCompleted;
    public override event EventHandler<ExceptionEventArgs> DownloadFailed;
    public override event EventHandler<ExceptionEventArgs> DecodeFailed;

    public override double DpiX
    {
        get { return 96; }
    }

    public override double DpiY
    {
        get { return 96; }
    }

    public override PixelFormat Format
    {
        get { return PixelFormats.Pbgra32; }
    }

    public override int PixelWidth
    {
        get { return pixelWidth; }
    }

    public override int PixelHeight
    {
        get { return pixelHeight; }
    }

    public override double Width
    {
        get { return pixelWidth; }
    }

    public override double Height
    {
        get { return pixelHeight; }
    }
}
