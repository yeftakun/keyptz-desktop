using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Key2Xbox.Rewrite;

public sealed class GlobalKeyBlocker : IDisposable
{
    private readonly object _lock = new();
    private readonly NativeMethods.LowLevelKeyboardProc _proc;
    private readonly HashSet<Keys> _blockedKeys = new();
    private readonly HashSet<Keys> _pressedKeys = new();
    private nint _hook;
    private bool _enabled;

    public GlobalKeyBlocker()
    {
        _proc = HookCallback;
    }

    public void SetEnabled(bool enabled)
    {
        lock (_lock)
        {
            _enabled = enabled;
        }
    }

    public void SetBlockedKeys(IEnumerable<Keys> keys)
    {
        lock (_lock)
        {
            _blockedKeys.Clear();
            foreach (var key in keys)
            {
                _blockedKeys.Add(key);
            }
        }
    }

    public void Start()
    {
        lock (_lock)
        {
            if (_hook != 0)
            {
                return;
            }

            _hook = NativeMethods.SetWindowsHookEx(NativeMethods.WhKeyboardLl, _proc, 0, 0);
            if (_hook == 0)
            {
                throw new InvalidOperationException($"Failed to install keyboard hook. Win32Error={Marshal.GetLastWin32Error()}");
            }
        }
    }

    public bool IsKeyPressed(Keys key)
    {
        lock (_lock)
        {
            return key switch
            {
                Keys.ShiftKey => _pressedKeys.Contains(Keys.ShiftKey) || _pressedKeys.Contains(Keys.LShiftKey) || _pressedKeys.Contains(Keys.RShiftKey),
                Keys.ControlKey => _pressedKeys.Contains(Keys.ControlKey) || _pressedKeys.Contains(Keys.LControlKey) || _pressedKeys.Contains(Keys.RControlKey),
                Keys.Menu => _pressedKeys.Contains(Keys.Menu) || _pressedKeys.Contains(Keys.LMenu) || _pressedKeys.Contains(Keys.RMenu),
                _ => _pressedKeys.Contains(key)
            };
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_hook == 0)
            {
                return;
            }

            NativeMethods.UnhookWindowsHookEx(_hook);
            _hook = 0;
            _blockedKeys.Clear();
            _pressedKeys.Clear();
            _enabled = false;
        }
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode < 0)
        {
            return NativeMethods.CallNextHookEx(_hook, nCode, wParam, lParam);
        }

        var message = (int)wParam;
        if (message != NativeMethods.WmKeyDown &&
            message != NativeMethods.WmKeyUp &&
            message != NativeMethods.WmSysKeyDown &&
            message != NativeMethods.WmSysKeyUp)
        {
            return NativeMethods.CallNextHookEx(_hook, nCode, wParam, lParam);
        }

        var info = Marshal.PtrToStructure<NativeMethods.KbdLlHookStruct>(lParam);
        var key = (Keys)info.VkCode;
        var isDown = message == NativeMethods.WmKeyDown || message == NativeMethods.WmSysKeyDown;
        var isUp = message == NativeMethods.WmKeyUp || message == NativeMethods.WmSysKeyUp;

        lock (_lock)
        {
            if (isDown)
            {
                _pressedKeys.Add(key);
            }
            else if (isUp)
            {
                _pressedKeys.Remove(key);
            }

            if (_enabled && _blockedKeys.Contains(key))
            {
                return 1;
            }
        }

        return NativeMethods.CallNextHookEx(_hook, nCode, wParam, lParam);
    }
}
