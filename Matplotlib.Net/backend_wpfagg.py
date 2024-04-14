"""
A work-in-progress backend for .NET WPF and potentially other .NET GUI frameworks in the future.
::

    import matplotlib
    matplotlib.use("template")

Copy this file to a directory where Python can import it (by adding the 
directory to your ``sys.path`` or by packaging it as a normal Python package); 
e.g. ``import backend_wpfagg`` you can then select it using ::

    import matplotlib
    matplotlib.use("module://backend_wpfagg")

"""

from matplotlib import backend_bases
from matplotlib.backend_bases import (
    FigureManagerBase, MouseEvent, MouseButton, TimerBase)
from matplotlib.backends.backend_agg import FigureCanvasAgg

from System.Windows.Threading import DispatcherTimer, DispatcherPriority
from System import TimeSpan, EventHandler

from Matplotlib.Net import NetMplAdapter




class TimerWpf(TimerBase):
    def __init__(self, *args, **kwargs):
        self._timer = DispatcherTimer(DispatcherPriority.Render)
        self._timer.Tick += EventHandler(self._on_tick)
        super().__init__(*args, **kwargs)
    
    def _timer_start(self):
        self._timer.Stop()
        self._timer.Start()
    
    def _timer_stop(self):
        self._timer.Stop()
    
    def _timer_set_interval(self):
        self._timer.Interval = TimeSpan.FromMilliseconds(self._interval)
    
    def _on_tick(self, a, b):
        super()._on_timer()


class NavigationToolbar2WpfAgg(backend_bases.NavigationToolbar2):
    toolitems = [
        (text, tooltip_text, image_file, name_of_method)
        for text, tooltip_text, image_file, name_of_method
        in (*backend_bases.NavigationToolbar2.toolitems,
            ('Download', 'Download plot', 'filesave', 'download'))
    ]

    def __init__(self, canvas):
        super().__init__(canvas)


class FigureManagerWpfAgg(FigureManagerBase):
    _toolbar2_class = NavigationToolbar2WpfAgg

    def __init__(self, canvas, num):
        super().__init__(canvas, num)
        self._dotnet_manager = NetMplAdapter(self, canvas)
        self.toolbar.pan()

    def handle_resize(self, w, h):
        fig = self.canvas.figure
        fig.set_size_inches(w/fig.dpi, h/fig.dpi, forward=False)
        from matplotlib.backend_bases import ResizeEvent
        self.resize(*fig.bbox.size)
        ResizeEvent('resize_event', self.canvas)._process()
        self.show()

    def show(self):
        if self._dotnet_manager is not None:
            self.canvas.draw_idle()


class FigureCanvasWpf(FigureCanvasAgg):
    manager_class = FigureManagerWpfAgg
    _timer_cls = TimerWpf
    
    def draw(self):
        super().draw()
        buf = self.buffer_rgba()
        w, h = self.get_width_height()
        self.manager._dotnet_manager.Draw(buf, w, h)
    
    def handle_wheel(self, pos, delta):
        x, y = pos
        actual_y = self.figure.bbox.height - y
        MouseEvent('scroll_event', self, x, actual_y, step=delta,
                   )._process()

    def handle_mouse_move(self, pos):
        x, y = pos
        actual_y = self.figure.bbox.height - y
        MouseEvent("motion_notify_event", self, x, actual_y)._process()

    def handle_mouse_up(self, pos, button):
        x, y = pos
        actual_y = self.figure.bbox.height - y
        MouseEvent("button_release_event", self, x, actual_y, MouseButton.LEFT if button == "left" else MouseButton.RIGHT)._process()

    def handle_mouse_down(self, pos, button):
        x, y = pos
        actual_y = self.figure.bbox.height - y
        MouseEvent("button_press_event", self, x, actual_y, MouseButton.LEFT if button == "left" else MouseButton.RIGHT)._process()


FigureCanvas = FigureCanvasWpf
FigureManager = FigureManagerWpfAgg
