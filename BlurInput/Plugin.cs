using PluginBlurInput;
using Rainmeter;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System;

public static class Plugin
{
    static IntPtr stringBuffer = IntPtr.Zero;

    [DllExport]
    public static void Initialize(ref IntPtr data, IntPtr rm)
    {

        data = GCHandle.ToIntPtr(GCHandle.Alloc(new Measure()));
    }

    [DllExport]
    public static void Finalize(IntPtr data)
    {
        Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
        measure.Stop();
        GCHandle.FromIntPtr(data).Free();
        if (stringBuffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(stringBuffer);
            stringBuffer = IntPtr.Zero;
        }
    }

    [DllExport]
    public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
    {
        Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
        measure.Reload(new Rainmeter.API(rm), ref maxValue);
    }

    [DllExport]
    public static double Update(IntPtr data)
    {
        Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
        measure.Update();
        return 0.0;
    }

    [DllExport]
    public static void ExecuteBang(IntPtr data, [MarshalAs(UnmanagedType.LPWStr)] string args)
    {
        Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;

        switch (args.ToLowerInvariant())
        {
            case "start":
                measure.Start();
                break;

            case "stop":
                measure.Stop();
                break;

            case "context":
                measure.ShowContextForm();
                break;

            case "cleartext":
                measure.ClearText();
                break;

            case "copy":
                measure.CopyToClipboard();
                break;

            case "paste":
                measure.PasteFromClipboard();
                break;

            case "redo":
                measure.Redo();
                break;

            case "undo":
                measure.Undo();
                break;

            case "cut":
                measure.CutToClipboard();
                break;

            default:
                Debug.WriteLine($"Unknown command: {args}");
                break;
        }
    }
    [DllExport]
    public static IntPtr GetString(IntPtr data)
    {
        Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
        string result = measure.GetUserInput();

        if (stringBuffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(stringBuffer);
        }

        stringBuffer = Marshal.StringToHGlobalUni(result);
        return stringBuffer;
    }
}
