using System.Windows;
using System.Windows.Controls;
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
        using var _ = Py.GIL();
        using var scope = Py.CreateScope();
        scope.Exec(Colormesh);
    }
    
    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        using var _ = Py.GIL();
        using var scope = Py.CreateScope();
        //Mpl1.ExecPython(Script.Text);
        Mpl2.ExecPython(GameOfLife);
    }
    
    private void ButtonBase_OnClick2(object sender, RoutedEventArgs e)
    {
        using var _ = Py.GIL();
        using var scope = Py.CreateScope();
        Mpl2.ExecPython(Script2.Text);
    }

    private static string GameOfLife = @"
import numpy as np
import matplotlib.pyplot as plt
import matplotlib.animation as animation

from scipy.signal import convolve2d



#fig.set_figwidth(19.2)
#fig.set_figheight(10.8)


def init(n, ax):
    import numpy as np
    state = np.random.choice(a=[False, True], size=(n, n))*1
    img = ax.imshow(state, interpolation='nearest', cmap='gray')

    con = np.array([
        [1, 1, 1],
        [1, 0, 1],
        [1, 1, 1]])

    def game_of_life(state):
        from scipy.signal import convolve2d
        nghbd = convolve2d(state, con, mode='same')
        state = (state*(nghbd == 2) + (nghbd == 3))
        return state

    def _update(frame):
        nonlocal state
        state = game_of_life(state*1)
        img.set_array(state)
        return img,

    return state, _update

s, update = init(100, ax)

ani = animation.FuncAnimation(
    fig=fig, func=update, frames=360, interval=30, blit=False, repeat=False)

fig.canvas.draw()
";
    
    private static string Colormesh = @"
import matplotlib.pyplot as plt
import numpy as np
nrows = 3
ncols = 5
Z = np.arange(nrows * ncols).reshape(nrows, ncols)
x = np.arange(ncols + 1)
y = np.arange(nrows + 1)

#fig, ax = plt.subplots()
plt.pcolormesh(x, y, Z, shading='flat', vmin=Z.min(), vmax=Z.max())
plt.show()


def _annotate(ax, x, y, title):
    # this all gets repeated below:
    X, Y = np.meshgrid(x, y)
    ax.plot(X.flat, Y.flat, 'o', color='m')
    ax.set_xlim(-0.7, 5.2)
    ax.set_ylim(-0.7, 3.2)
    ax.set_title(title)

#_annotate(ax, x, y, ""shading='flat'"")
";
}