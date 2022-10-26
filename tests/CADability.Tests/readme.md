# CADability.Tests

Run test from commandline:

```bash
SET VsInstallRoot=C:\Program Files\Microsoft Visual Studio\2022\Community
dotnet test CADability.sln
```

> ! Setting VsInstallRoot is required because CADability.csproj references Microsoft.VisualStudio.DebuggerVisualizers with this path.

# Coverage in Visual Studio

* Recommended extension: Fine Code Coverage
* Settings:
  * Enabled: True
  * RunMsCodeCoverage: True

This will show coverage markers directly in Visual Studio and a Coverage Report in the Fine Code Coverage Tool window.

# Generating Test Artifacts

Recommended for generating cobertura.xml and junit.xml (i.e. for GitLab)

```bash
dotnet test CADability.sln  --collect:"XPlat Code Coverage" --logger:"junit;LogFilePath=test-results.xml;MethodFormat=Class;FailureBodyFormat=Verbose" --settings coverlet.runsettings
```

> ! Does currently not work and test run hangs
