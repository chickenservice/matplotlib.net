using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Python.Runtime;

namespace Matplotlib.Net;

public class MplPanel : Panel
{
    private readonly NetMplAdapter _adapter;
    private WriteableBitmap _buffer = new(800, 600, 96, 96, PixelFormats.Pbgra32, null);
    private readonly PyObject _axes;
    private readonly PyObject _fig;

    static MplPanel()
    {
        Pythonnet.Init();
        using var _ = Py.GIL();
        using var scope = Py.CreateScope();
        scope.Exec("""

                   import matplotlib
                   matplotlib.use('module://backend_wpfagg')

                   """
        );
    }

    public MplPanel()
    {
        using var _ = Py.GIL();
        using var scope = Py.CreateScope();
        scope.Exec("import matplotlib");
        var figax = scope.Eval("matplotlib.pyplot.subplots()");
        _adapter = figax[0].GetAttr("canvas").GetAttr("manager").GetAttr("_dotnet_manager").As<NetMplAdapter>();
        _adapter.SetBuffer(_buffer);
        _fig = figax[0];
        _axes = figax[1];
        
        MouseMove += HandleMouseMove;
        MouseLeftButtonDown += HandleMouseDown;
        MouseRightButtonDown += HandleMouseDown;
        MouseRightButtonUp += HandleMouseUp;
        MouseLeftButtonUp += HandleMouseUp;
        MouseWheel += HandleMouseWheel;
    }

    public void ExecPython(string code)
    {
        using var _ = Py.GIL();
        using var scope = Py.CreateScope();
        scope.Exec(code, new Py.KeywordArguments{["ax"] = _axes, ["fig"] = _fig});
    }

    private void HandleMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var pos = e.GetPosition(this);
        _adapter.HandleWheel(pos, e.Delta);
        InvalidateVisual();
    }

    private void HandleMouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(this);
        _adapter.HandleMouseMove(pos, ButtonToMpl(e));
        if (e.RightButton == MouseButtonState.Pressed || e.LeftButton == MouseButtonState.Pressed)
            InvalidateVisual();
    }
    
    private void HandleMouseUp(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(this);
        _adapter.HandleMouseUp(pos, ButtonToMpl(e));
        InvalidateVisual();
    }

    private void HandleMouseDown(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(this);
        _adapter.HandleMouseDown(pos, ButtonToMpl(e));
        InvalidateVisual();
    }

    private static string ButtonToMpl(MouseEventArgs state)
    {
        return state switch
        {
            { LeftButton: MouseButtonState.Pressed } => "left",
            { RightButton: MouseButtonState.Pressed } => "right",
            _ => ""
        };
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var w = ActualWidth;
        var h = ActualHeight;
        if (w > 0 || h > 0)
        {
            if (w > _buffer.Width || h > _buffer.Height)
            {
                _buffer = new WriteableBitmap((int)w + 200, (int)h + 200, 96, 96, PixelFormats.Pbgra32, null);
                _adapter.SetBuffer(_buffer);
            }
            
            _adapter.SetFigureSize(w, h);
        }

        dc.DrawImage(_buffer, new Rect(new Point(), new Point(_buffer.PixelWidth, _buffer.PixelHeight)));
    }
}