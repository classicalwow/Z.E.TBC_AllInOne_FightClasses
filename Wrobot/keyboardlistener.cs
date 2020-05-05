using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

public class KeyBoardHook
{
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x101;
    private static Keys _lastKeyDown = Keys.None;

    private static readonly LowLevelKeyboardProc MouseProc = MouseHookCallback;
    private static IntPtr _hookMouseId = IntPtr.Zero;
    private static readonly LowLevelKeyboardProc Proc = HookCallback;
    private static IntPtr _hookId = IntPtr.Zero;
    public delegate void KeyHookEventHandler(string buttons);
    public static event KeyHookEventHandler OnKeyDown = delegate { };
    public static event KeyHookEventHandler OnKeyUp = delegate { };

    internal static void Initialize()
    {
        Task.Factory.StartNew(() =>//avoid thread blocking from Application.Run();
        {
            _hookId = SetHook(Proc);
            _hookMouseId = SetMouseHook(MouseProc);
            
            Application.Run(); //important! need to run in its own "context"
        });
    }
    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (var curProcess = Process.GetCurrentProcess())
        using (var curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(13, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    internal static void Dispose()
    {
        UnhookWindowsHookEx(_hookMouseId);
        UnhookWindowsHookEx(_hookId);
        Application.Exit();
    }


    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        if (wParam == (IntPtr)WM_KEYDOWN)
        {
            var vkCode = (Keys)Marshal.ReadInt32(lParam);
            if (_lastKeyDown == vkCode)
                return CallNextHookEx(_hookId, nCode, wParam, lParam);
            OnKeyDown(""+new KeyArgs(vkCode).Key);
            _lastKeyDown = vkCode;
        }
        else if (wParam == (IntPtr)WM_KEYUP)
        {
            var vkCode = (Keys)Marshal.ReadInt32(lParam);
            OnKeyUp(""+new KeyArgs(vkCode).Key);
            if (_lastKeyDown == vkCode)
                _lastKeyDown = Keys.None;
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private static IntPtr SetMouseHook(LowLevelKeyboardProc proc)
    {
        using (var curProcess = Process.GetCurrentProcess())
        using (var curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(14, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private static IntPtr SetKeyBHook(LowLevelKeyboardProc proc)
    {
        using (var curProcess = Process.GetCurrentProcess())
        using (var curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(13, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
            return CallNextHookEx(_hookMouseId, nCode, wParam, lParam);

        //xbutton
        if(wParam == (IntPtr)523)
        {
            var marshalledMouseStruct = (MouseStruct)Marshal.PtrToStructure(lParam, typeof(MouseStruct));
            OnKeyDown("xbtn"+marshalledMouseStruct.MouseData);

        }else if (wParam == (IntPtr)524)
        {
            var marshalledMouseStruct = (MouseStruct)Marshal.PtrToStructure(lParam, typeof(MouseStruct));
            OnKeyUp("xbtn" + marshalledMouseStruct.MouseData);
        }
        //鼠标中间
        else if (wParam == (IntPtr)519)
        {
            OnKeyDown("xbtn3");
        }
        else if (wParam == (IntPtr)520)
        {
            OnKeyUp("xbtn3");
        }
        return CallNextHookEx(_hookMouseId, nCode, wParam, lParam);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    public class KeyArgs : EventArgs
    {
        public Keys Key { get; private set; }

        public KeyArgs(Keys key)
        {
            this.Key = key;
        }
    }
}



/// <summary>
///     The <see cref="MouseStruct" /> structure contains information about a mouse input event.
/// </summary>
/// <remarks>
///     See full documentation at http://globalmousekeyhook.codeplex.com/wikipage?title=MouseStruct
/// </remarks>
[StructLayout(LayoutKind.Explicit)]
internal struct MouseStruct
{
    /// <summary>
    ///     Specifies a Point structure that contains the X- and Y-coordinates of the cursor, in screen coordinates.
    /// </summary>
    [FieldOffset(0x00)] public Point Point;

    /// <summary>
    ///     Specifies information associated with the message.
    /// </summary>
    /// <remarks>
    ///     The possible values are:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>0 - No Information</description>
    ///         </item>
    ///         <item>
    ///             <description>1 - X-Button1 Click</description>
    ///         </item>
    ///         <item>
    ///             <description>2 - X-Button2 Click</description>
    ///         </item>
    ///         <item>
    ///             <description>120 - Mouse Scroll Away from User</description>
    ///         </item>
    ///         <item>
    ///             <description>-120 - Mouse Scroll Toward User</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    [FieldOffset(0x0A)] public short MouseData;

    /// <summary>
    ///     Returns a Timestamp associated with the input, in System Ticks.
    /// </summary>
    [FieldOffset(0x10)] public int Timestamp;
}