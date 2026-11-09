using AndroidSyncControl.Mcp.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace AndroidSyncControl.Tests;

[TestClass]
public class WindowsCommandLineTests
{
    [DataTestMethod]
    [DataRow("shell", "pm", "list")]
    [DataRow("shell", "echo hello world")]
    [DataRow("shell", "say \"hi\" there")]
    [DataRow("push", @"C:\dir with space\", "/sdcard/x")]
    [DataRow("shell", @"a\\""b\")]
    [DataRow("shell", "", "tail")]
    [DataRow("shell", "a & del x | y")]
    public void Join_IsSplitBackIntoTheSameArguments(params string[] arguments)
    {
        string commandLine = "adb.exe " + WindowsCommandLine.Join(arguments);

        string[] parsed = Split(commandLine).Skip(1).ToArray();

        CollectionAssert.AreEqual(arguments, parsed);
    }

    [TestMethod]
    public void Join_LeavesPlainArgumentsUnquoted()
    {
        Assert.AreEqual("shell pm list", WindowsCommandLine.Join(new[] { "shell", "pm", "list" }));
    }

    static string[] Split(string commandLine)
    {
        IntPtr argv = CommandLineToArgvW(commandLine, out int count);
        Assert.AreNotEqual(IntPtr.Zero, argv);
        try
        {
            return Enumerable.Range(0, count)
                .Select(i => Marshal.PtrToStringUni(Marshal.ReadIntPtr(argv, i * IntPtr.Size))!)
                .ToArray();
        }
        finally
        {
            LocalFree(argv);
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr CommandLineToArgvW(string commandLine, out int count);

    [DllImport("kernel32.dll")]
    static extern IntPtr LocalFree(IntPtr handle);
}
