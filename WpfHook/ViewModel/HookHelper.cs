using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WpfHook.ViewModel
{
    //注意：
    //如果运行中出现SetWindowsHookEx的返回值为0，这是因为.net 调试模式的问题，具体的做法是禁用宿主进程，在 Visual Studio 中打开项目。
    //在“项目”菜单上单击“属性”。
    //单击“调试”选项卡。
    //清除“启用 Visual Studio 宿主进程(启用windows承载进程)”复选框 或 勾选启用非托管代码调试

    //Declare wrapper managed POINT class.
    [StructLayout(LayoutKind.Sequential)]
    public class POINT
    {
        public int x;
        public int y;
    }

    //Declare wrapper managed MouseHookStruct class.
    [StructLayout(LayoutKind.Sequential)]
    public class MouseHookStruct
    {
        public POINT pt;
        public int hwnd;
        public int wHitTestCode;
        public int dwExtraInfo;
    }

    //Declare wrapper managed KeyboardHookStruct class.
    [StructLayout(LayoutKind.Sequential)]
    public class KeyboardHookStruct
    {
        public int vkCode; //Specifies a virtual-key code. The code must be a value in the range 1 to 254. 
        public int scanCode; // Specifies a hardware scan code for the key. 
        public int flags; // Specifies the extended-key flag, event-injected flag, context code, and transition-state flag.
        public int time; // Specifies the time stamp for this message.
        public int dwExtraInfo; // Specifies extra information associated with the message. 
    }

    public class HookHelper
    {
        private static readonly Lazy<HookHelper>
            Lazy = new Lazy<HookHelper>(() => new HookHelper());

        public static HookHelper Instance => Lazy.Value;

        public delegate int HookProc(int nCode, Int32 wParam, IntPtr lParam);
        public delegate int GlobalHookProc(int nCode, Int32 wParam, IntPtr lParam);

        public HookHelper()
        {

        }
        ~HookHelper()
        {
            Stop();
        }

        public event MouseEventHandler OnMouseActivity;
        public event KeyEventHandler KeyDown;
        public event KeyPressEventHandler KeyPress;
        public event KeyEventHandler KeyUp;

        //鼠标
        private const int WmMousemove = 0x200;
        private const int WmLButtonDown = 0x201;
        private const int WmRButtonDown = 0x204;
        private const int WmMButtonDown = 0x207;
        private const int WmLButtonUp = 0x202;
        private const int WmRButtonUp = 0x205;
        private const int WmMButtonUp = 0x208;
        private const int WmLButtonDBlclk = 0x203;
        private const int WmRbuttondblclk = 0x206;
        private const int WmMbuttondblclk = 0x209;

        //键盘
        private const int WmKeydown = 0x100;
        private const int WmKeyup = 0x101;
        private const int WmSyskeydown = 0x104;
        private const int WmSyskeyup = 0x105;

        /// <summary>
        /// 定义鼠标钩子句柄.
        /// </summary>
        private static int _hMouseHook = 0;
        public int HMouseHook => _hMouseHook;

        /// <summary>
        /// 定义键盘钩子句柄
        /// </summary>
        private static int _hKeyboardHook = 0;
        public int HKeyboardHook => _hKeyboardHook;

        /// <summary>
        /// 鼠标钩子常量(from Microsoft SDK  Winuser.h )
        /// </summary>
        public const int WH_MOUSE_LL = 14;
        /// <summary>
        /// 键盘钩子常量(from Microsoft SDK  Winuser.h )
        /// </summary>
        public const int WhKeyboardLl = 13;

        /// <summary>
        /// 定义鼠标处理过程的委托对象
        /// </summary>
        private GlobalHookProc _mouseHookProcedure;
        /// <summary>
        /// 键盘处理过程的委托对象
        /// </summary>
        private GlobalHookProc _keyboardHookProcedure;

        /// <summary>
        /// 安装钩子方法
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetWindowsHookEx(int idHook, GlobalHookProc lpfn, IntPtr hInstance, int threadId);

        /// <summary>
        /// 卸载钩子方法
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern bool UnhookWindowsHookEx(int idHook);

        //Import for CallNextHookEx.
        /// <summary>
        /// 使用这个函数钩信息传递给链中的下一个钩子过程。
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int CallNextHookEx(int idHook, int nCode, Int32 wParam, IntPtr lParam);

        public bool Start()
        {
            if (_hMouseHook == 0) //安装鼠标钩子
            {
                // Create an instance of HookProc.
                _mouseHookProcedure = new GlobalHookProc(MouseHookProc);
                try
                {
                    _hMouseHook = SetWindowsHookEx(WH_MOUSE_LL,
                        _mouseHookProcedure,
                        Marshal.GetHINSTANCE(
                            Assembly.GetExecutingAssembly().GetModules()[0]),
                        0);
                }
                catch /*(Exception e)*/
                {
                    //Console.WriteLine(e);
                    //throw;
                }

                //如果安装鼠标钩子失败
                if (_hMouseHook == 0)
                {
                    Stop();
                    return false;
                    //throw new Exception("SetWindowsHookEx failed.");
                }
            }

            //安装键盘钩子
            if (_hKeyboardHook == 0)
            {
                _keyboardHookProcedure = new GlobalHookProc(KeyboardHookProc);
                try
                {
                    _hKeyboardHook = SetWindowsHookEx(WhKeyboardLl,
                        _keyboardHookProcedure,
                        Marshal.GetHINSTANCE(
                            Assembly.GetExecutingAssembly().GetModules()[0]),
                        0);
                }
                catch /*(Exception e)*/
                {
                    //Console.WriteLine(e);
                    //throw;
                }

                //如果安装键盘钩子失败
                if (_hKeyboardHook == 0)
                {
                    Stop();
                    return false;
                    //throw new Exception("SetWindowsHookEx ist failed.");
                }
            }

            return true;
        }

        /// <summary>
        /// 卸载hook,如果进程强制结束,记录上次钩子id,并把根据钩子id来卸载它
        /// </summary>
        public void Stop(int hMouseHook, int hKeyboardHook)
        {
            if (hMouseHook != 0)
            {
                UnhookWindowsHookEx(hMouseHook);
            }
            if (hKeyboardHook != 0)
            {
                UnhookWindowsHookEx(hKeyboardHook);
            }
        }

        public void Stop()
        {
            bool retMouse = true;
            bool retKeyboard = true;
            if (_hMouseHook != 0)
            {
                retMouse = UnhookWindowsHookEx(_hMouseHook);
                _hMouseHook = 0;
            }
            if (_hKeyboardHook != 0)
            {
                retKeyboard = UnhookWindowsHookEx(_hKeyboardHook);
                _hKeyboardHook = 0;
            }
            //If UnhookWindowsHookEx fails.
            if (!(retMouse && retKeyboard))
            {
                //throw new Exception("UnhookWindowsHookEx ist failed.");
            }
        }

        private int MouseHookProc(int nCode, Int32 wParam, IntPtr lParam)
        {
            if ((nCode >= 0) && (OnMouseActivity != null))
            {
                MouseButtons button = MouseButtons.None;
                switch (wParam)
                {
                    case WmLButtonDown:    //左键按下
                        //case WM_LBUTTONUP:    //右键按下
                        //case WM_LBUTTONDBLCLK:   //同时按下
                        button = MouseButtons.Left;
                        break;
                    case WmRButtonDown:
                        //case WM_RBUTTONUP: 
                        //case WM_RBUTTONDBLCLK: 
                        button = MouseButtons.Right;
                        break;
                }
                int clickCount = 0;
                if (button != MouseButtons.None)
                    if (wParam == WmLButtonDBlclk || wParam == WmRbuttondblclk)
                        clickCount = 2;
                    else clickCount = 1;

                //Marshall the data from callback.
                MouseHookStruct? myMouseHookStruct =
                    (MouseHookStruct)Marshal.PtrToStructure(lParam, typeof(MouseHookStruct))!;
                MouseEventArgs e = new MouseEventArgs(
                    button,
                    clickCount,
                    myMouseHookStruct.pt.x,
                    myMouseHookStruct.pt.y,
                    0);
                OnMouseActivity(this, e);
            }
            return CallNextHookEx(_hMouseHook, nCode, wParam, lParam);
        }

        [DllImport("user32")]
        public static extern int ToAscii(int uVirtKey, //[in] Specifies the virtual-key code to be translated. 
            int uScanCode, // [in] Specifies the hardware scan code of the key to be translated. The high-order bit of this value is set if the key is up (not pressed). 
            byte[] lpbKeyState, // [in] Pointer to a 256-byte array that contains the current keyboard state. Each element (byte) in the array contains the state of one key. If the high-order bit of a byte is set, the key is down (pressed). The low bit, if set, indicates that the key is toggled on. In this function, only the toggle bit of the CAPS LOCK key is relevant. The toggle state of the NUM LOCK and SCROLL LOCK keys is ignored.
            byte[] lpwTransKey, // [out] Pointer to the buffer that receives the translated character or characters. 
            int fuState); // [in] Specifies whether a menu is active. This parameter must be 1 if a menu is active, or 0 otherwise. 
        //The GetKeyboardState function copies the status of the 256 virtual keys to the specified buffer. 
        [DllImport("user32")]
        public static extern int GetKeyboardState(byte[] pbKeyState);
        private int KeyboardHookProc(int nCode, Int32 wParam, IntPtr lParam)
        {
            // it was ok and someone listens to events
            if ((nCode >= 0) && (KeyDown != null || KeyUp != null || KeyPress != null))
            {
                KeyboardHookStruct myKeyboardHookStruct =
                    (KeyboardHookStruct)Marshal.PtrToStructure(lParam,
                        typeof(KeyboardHookStruct));
                // raise KeyDown
                if (KeyDown != null && (wParam == WmKeydown || wParam == WmSyskeydown))
                {
                    Keys keyData = (Keys)myKeyboardHookStruct.vkCode;
                    KeyEventArgs e = new KeyEventArgs(keyData);
                    KeyDown(this, e);
                }
                // raise KeyPress
                if (KeyPress != null && wParam == WmKeydown)
                {
                    byte[] keyState = new byte[256];
                    GetKeyboardState(keyState);
                    byte[] inBuffer = new byte[2];
                    if (ToAscii(myKeyboardHookStruct.vkCode,
                            myKeyboardHookStruct.scanCode,
                            keyState,
                            inBuffer,
                            myKeyboardHookStruct.flags) == 1)
                    {
                        KeyPressEventArgs e = new KeyPressEventArgs((char)inBuffer[0]);
                        KeyPress(this, e);
                    }
                }
                // raise KeyUp
                if (KeyUp != null && (wParam == WmKeyup || wParam == WmSyskeyup))
                {
                    Keys keyData = (Keys)myKeyboardHookStruct.vkCode;
                    KeyEventArgs e = new KeyEventArgs(keyData);
                    KeyUp(this, e);
                }
            }
            return CallNextHookEx(_hKeyboardHook, nCode, wParam, lParam);
        }
    }

}
