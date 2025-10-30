using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
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

static Version ParseZilfVersion(string v)
{
    if (string.IsNullOrWhiteSpace(v))
        throw new FormatException("Version string is null or empty.");

    // major.minor are mandatory
    // optional .micro
    // optional qualifier (a|b|rc) followed by numeric serial
    var rx = new Regex("^(?<major>\\d+)\\.(?<minor>\\d+)(?:\\.(?<micro>\\d+))?(?:(?<qual>a|b|rc)(?<serial>\\d+))?$", RegexOptions.IgnoreCase);
    var m = rx.Match(v.Trim());
    if (!m.Success)
        throw new FormatException($"Invalid ZILF version format: '{v}'");

    int major = int.Parse(m.Groups["major"].Value);
    int minor = int.Parse(m.Groups["minor"].Value);

    string qual = m.Groups["qual"].Success ? m.Groups["qual"].Value.ToLowerInvariant() : string.Empty;
    int qualCode = 100; // none -> 100
    if (qual == "a") qualCode = 1;
    else if (qual == "b") qualCode = 2;
    else if (qual == "rc") qualCode = 3;
    else if (qual != "") throw new FormatException($"Unknown qualifier '{qual}' in ZILF version '{v}'");

    int revision = 0;
    if (m.Groups["serial"].Success && !string.IsNullOrEmpty(m.Groups["serial"].Value))
    {
        revision = int.Parse(m.Groups["serial"].Value);
    }
    else if (m.Groups["micro"].Success && !string.IsNullOrEmpty(m.Groups["micro"].Value))
    {
        // If there's no serial but a micro is present, use micro as the revision.
        revision = int.Parse(m.Groups["micro"].Value);
    }

    return new Version(major, minor, qualCode, revision);
}

try
{
    project.Version = ParseZilfVersion(shortVersion);
}
catch (Exception ex)
{
    throw new InvalidOperationException($"Failed to parse ZILF_SHORT_VERSION '{shortVersion}': {ex.Message}", ex);
}

// project.AddUIProject("WindowsInstaller.UI"); // name of the 'Custom UI Library' project in the solution
project.UI = WUI.WixUI_InstallDir;
project.RemoveDialogsBetween(NativeDialogs.WelcomeDlg, NativeDialogs.InstallDirDlg);

//project.Load += (e) =>
//{
//    Native.MessageBox("OnLoad", "WixSharp - .NET8");
//    e.Result = ActionResult.Failure;
//};

project.BuildMsi();