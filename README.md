# matplotlib.net
A wrapper to embed interactive matplotlib in .NET desktop applications using Pythonnet. 
This project is a WIP and supports WPF only at this moment.

![Matplotlib embedded in WPF demo](docs/wpf_matplotlib_demo.gif)

## How it works
This is essentially an implementation of a custom matplotlib GUI backend, which is very well documented 
and has been done for all the popular GUIs like Qt, Tk etc. The only difference being that
Pythonnet is necessary to allow in-process access to matplotlib.

Matplotlib uses Agg and Cairo as Vector rasterizers to draw figures, it is essentially GUI-less
by default. The rasterized buffer can be retrieved and rendered as a bitmap using any .NET GUI framework.
Mouse and key events have to be forwarded to matplotlib such that the figures are updated correctly to 
allow proper interactive behavior.