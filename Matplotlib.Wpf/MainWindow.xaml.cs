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
        var txtbx  = (TextBox)FindName("Script");
        using var _ = Py.GIL();
        using var scope = Py.CreateScope();
        scope.Exec("import matplotlib.pyplot as plt");
        scope.Exec(txtbx.Text);
    }
    
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