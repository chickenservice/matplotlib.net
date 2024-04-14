using System.Windows;
using Python.Runtime;

namespace Matplotlib.Net;

public static class Pythonnet
{
    public static void Init()
    {
        var pathToVirtualEnv = @"C:\Users\chickenservice\AppData\Local\Programs\Python\Python312";

        var path = Environment.GetEnvironmentVariable("PATH").TrimEnd(';');
        path = string.IsNullOrEmpty(path) ? pathToVirtualEnv : path + ";" + pathToVirtualEnv;
        Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("PATH", pathToVirtualEnv, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("PYTHONHOME", pathToVirtualEnv, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("PYTHONPATH", $"{pathToVirtualEnv}\\Lib\\site-packages;{pathToVirtualEnv}\\Lib;{pathToVirtualEnv}\\DLLs;C:\\Users\\chickenservice\\Projects\\Matplotlib.Net\\Matplotlib.Net\\bin\\Debug\\net7.0-windows", EnvironmentVariableTarget.Process);

        Runtime.PythonDLL = $"{pathToVirtualEnv}/python312.dll";
        PythonEngine.PythonHome = pathToVirtualEnv;
        PythonEngine.PythonPath = Environment.GetEnvironmentVariable("PYTHONPATH", EnvironmentVariableTarget.Process);
        PythonEngine.Initialize();
        PythonEngine.BeginAllowThreads();

        using (Py.GIL())
        {
            PythonEngine.Exec("import matplotlib.pyplot as plt");
        }
    }
}