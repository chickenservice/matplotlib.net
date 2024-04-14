using System.IO;
using Microsoft.Extensions.Configuration;
using Python.Runtime;

namespace Matplotlib.Net;

public static class Pythonnet
{
    public static IConfiguration Config { get; private set; }
    
    public static void Init()
    {
        Config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var pathToVirtualEnv = Config.GetSection("Python")["PythonHome"];

        var currDir = Directory.GetCurrentDirectory();
        var path = Environment.GetEnvironmentVariable("PATH").TrimEnd(';');
        path = string.IsNullOrEmpty(path) ? pathToVirtualEnv : path + ";" + pathToVirtualEnv;
        Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("PATH", pathToVirtualEnv, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("PYTHONHOME", pathToVirtualEnv, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("PYTHONPATH", $"{pathToVirtualEnv}\\Lib\\site-packages;{pathToVirtualEnv}\\Lib;{pathToVirtualEnv}\\DLLs;{currDir}", EnvironmentVariableTarget.Process);

        Runtime.PythonDLL = $"{pathToVirtualEnv}/python{Config.GetSection("Python")["PythonVersion"]}.dll";
        PythonEngine.PythonHome = pathToVirtualEnv;
        PythonEngine.PythonPath = Environment.GetEnvironmentVariable("PYTHONPATH", EnvironmentVariableTarget.Process);
        PythonEngine.Initialize();
        PythonEngine.BeginAllowThreads();
    }
}