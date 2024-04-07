using System.Windows;
using System.Windows.Media.Imaging;
using Python.Runtime;

namespace Matplotlib.Net;

public class NetMplAdapter
{
    public NetMplAdapter(PyObject manager, PyObject canvas)
    {
        Canvas = canvas;
        FigManager = manager;
        _white = 255 << 24;
        _white |= 255 << 16;
        _white |= 255 << 8;
        _white |= 255 << 0;
    }

    private readonly int _white; 
    private int _lastHeight;
    private int _lastWidth;
    private WriteableBitmap AggBuffer { get; set; }
    private PyObject FigManager { get; }
    private PyObject Canvas { get; }
    
    /// <summary>
    /// GIL should not be acquired as this method should only be called from the python WpfAgg backend.
    /// </summary>
    /// <param name="buf"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public unsafe void Draw(PyObject buf, int width, int height)
    {
        try
        {
            AggBuffer.Lock();
            var backBufferPtr = AggBuffer.BackBuffer;
            var stride = AggBuffer.BackBufferStride;
            var buffer = buf.GetBuffer(PyBUF.STRIDES);
            var pyStrides = buffer.Strides[0];
            var source = (byte*)(int*)buffer.Buffer;
            
            // clear 
            if (_lastHeight > height)
            {
                for (int y = height; y < _lastHeight; y++)
                {
                    for (int x = 0; x < _lastWidth; x++)
                    {
                        var dest = backBufferPtr;
                        dest += y * stride;
                        dest += x * 4;
                        *((int*)dest) = _white;
                    }
                }
                
                AggBuffer.AddDirtyRect(new Int32Rect(0, height, _lastWidth, _lastHeight - height));
            }

            if (_lastWidth > width)
            {
                for (int y = 0; y < _lastHeight; y++)
                {
                    for (int x = width; x < _lastWidth; x++)
                    {
                        var dest = backBufferPtr;
                        dest += y * stride;
                        dest += x * 4;
                        *((int*)dest) = _white;
                    }
                }
                
                AggBuffer.AddDirtyRect(new Int32Rect(width, 0, _lastWidth -  width, _lastHeight));
            }
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var dest = backBufferPtr;
                    dest += y * stride;
                    dest += x * 4;
                    var dstPtr = (byte*)(int*)dest;
                    byte* srcPtr = source + pyStrides * y + 4 * x;
                    byte a = *(srcPtr + 3);
                    *(dstPtr++) = (byte)(*(srcPtr + 2) * a / 256); // pre-multiplied B
                    *(dstPtr++) = (byte)(*(srcPtr + 1) * a / 256); // pre-multiplied G
                    *(dstPtr++) = (byte)(*srcPtr * a / 256); // pre-multiplied R
                    *(dstPtr++) = a;
                }
            }

            AggBuffer.AddDirtyRect(new Int32Rect(0, 0, width, height));
            _lastWidth = width;
            _lastHeight = height;
        }
        finally
        {
            AggBuffer.Unlock();
        }
    }

    public void SetBuffer(WriteableBitmap buffer)
    {
        AggBuffer = buffer;
        _lastHeight = AggBuffer.PixelHeight;
        _lastWidth = AggBuffer.PixelWidth;
    }

    public void SetFigureSize(double w, double h)
    {
        using var _ = Py.GIL();
        using var scope = Py.CreateScope();
        FigManager.InvokeMethod("handle_resize", new []{ w.ToPython(), h.ToPython()});
    }

    public void HandleMouseMove(Point pos, string buttonToMpl)
    {
        using var _ = Py.GIL();
        using var scope = Py.CreateScope();
        var p = new PyList();
        p.Append(pos.X.ToPython());
        p.Append(pos.Y.ToPython());
        Canvas.InvokeMethod("handle_mouse_move", new []{p});
    }

    public void HandleMouseDown(Point pos, string buttonToMpl)
    {
        using var _ = Py.GIL();
        using var scope = Py.CreateScope();
        var p = new PyList();
        p.Append(pos.X.ToPython());
        p.Append(pos.Y.ToPython());
        Canvas.InvokeMethod("handle_mouse_down", new []{p, buttonToMpl.ToPython()});
    }

    public void HandleMouseUp(Point pos, string buttonToMpl)
    {
        using var _ = Py.GIL();
        using var scope = Py.CreateScope();
        var p = new PyList();
        p.Append(pos.X.ToPython());
        p.Append(pos.Y.ToPython());
        Canvas.InvokeMethod("handle_mouse_up", new []{p, buttonToMpl.ToPython()});
    }
    
    public void HandleWheel(Point pos, int eDelta)
    {
        using var _ = Py.GIL();
        using var scope = Py.CreateScope();
        var p = new PyList();
        p.Append(pos.X.ToPython());
        p.Append(pos.Y.ToPython());
        Canvas.InvokeMethod("handle_wheel", new [] {p, eDelta.ToPython()});
    }
}