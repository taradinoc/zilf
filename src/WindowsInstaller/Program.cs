using System.Runtime.CompilerServices;
using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.Controls;
using WixToolset.Dtf.WindowsInstaller;
using File = WixSharp.File;

[assembly: InternalsVisibleTo(assemblyName: "WindowsInstaller.aot")] // assembly name + '.aot suffix

string shortVersion = Environment.GetEnvironmentVariable("ZILF_SHORT_VERSION") ??
    throw new InvalidOperationException("Missing ZILF_SHORT_VERSION environment variable");
string longVersion = Environment.GetEnvironmentVariable("ZILF_LONG_VERSION") ??
    throw new InvalidOperationException("Missing ZILF_LONG_VERSION environment variable");
string arch = Environment.GetEnvironmentVariable("ZILF_ARCH") ??
    throw new InvalidOperationException("Missing ZILF_ARCH environment variable");

var project =
    new ManagedProject("ZILF",
        new Dir(@"%ProgramFiles%\ZILF",
            new DirFiles(@".\*.*"),
            new Dir("bin",
                new File(@"bin\Zilf.exe"),
                new File(@"bin\Zapf.exe")),
            new Dir("sample",
                new Files(@"sample\*.*")),
            new Dir("zillib",
                new Files(@"zillib\*.*"))),
        //new Property("PropName", "<your value>")
        new EnvironmentVariable("Path", "[INSTALLDIR]bin")
        {
            Id = "Path_INSTALLDIR",
            Action = EnvVarAction.set,
            //Condition = Condition.Installed,
            Part = EnvVarPart.last,
            Permanent = false,
            System = true,
        });

project.OutFileName = $"zilf-{longVersion}-{arch}";
project.GUID = new Guid("F16A15CF-E840-43C7-86D6-C592A7DEB1D8");
project.SourceBaseDir = @$"..\..\Package\Release\Stage\zilf-{longVersion}-{arch}";
project.Version = new Version(shortVersion);

// project.AddUIProject("WindowsInstaller.UI"); // name of the 'Custom UI Library' project in the solution
project.UI = WUI.WixUI_InstallDir;
project.RemoveDialogsBetween(NativeDialogs.WelcomeDlg, NativeDialogs.InstallDirDlg);

//project.Load += (e) =>
//{
//    Native.MessageBox("OnLoad", "WixSharp - .NET8");
//    e.Result = ActionResult.Failure;
//};

project.BuildMsi();