using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PluginBlurInput
{
    /// <summary>
    /// Handles keyboard input detection and character mapping using Win32 API
    /// </summary>
    public class InputHandler
    {
        #region Win32 API Declarations
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        private static extern int ToUnicode(uint virtualKey, uint scanCode, byte[] keyboardState,
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder receivingBuffer, int bufferSize, uint flags);

        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] lpKeyState);
        #endregion

        #region Virtual Key Codes
        public const int VK_BACKSPACE = 8;
        public const int VK_TAB = 9;
        public const int VK_RETURN = 13;
        public const int VK_SHIFT = 16;
        public const int VK_CONTROL = 17;
        public const int VK_CAPSLOCK = 20;
        public const int VK_ESCAPE = 27;
        public const int VK_END = 35;
        public const int VK_HOME = 36;
        public const int VK_LEFT = 37;
        public const int VK_UP = 38;
        public const int VK_RIGHT = 39;
        public const int VK_DOWN = 40;
        public const int VK_DELETE = 46;

        // Ctrl key combinations
        public const int VK_C = 67;
        public const int VK_V = 86;
        public const int VK_X = 88;
        public const int VK_Y = 89;
        public const int VK_Z = 90;
        #endregion

        #region Keyboard State Detection
        /// <summary>
        /// Checks if a key is currently pressed
        /// </summary>
        public static bool IsKeyPressed(int virtualKeyCode)
        {
            return (GetAsyncKeyState(virtualKeyCode) & 0x8000) != 0;
        }

        /// <summary>
        /// Checks if a key was just pressed (state changed)
        /// </summary>
        public static bool IsKeyJustPressed(int virtualKeyCode)
        {
            return (GetAsyncKeyState(virtualKeyCode) & 0x0001) != 0;
        }

        /// <summary>
        /// Checks if Caps Lock is active
        /// </summary>
        public static bool IsCapsLockActive()
        {
            return (GetKeyState(VK_CAPSLOCK) & 0x0001) != 0;
        }

        /// <summary>
        /// Checks if Ctrl key is pressed
        /// </summary>
        public static bool IsCtrlPressed()
        {
            return IsKeyPressed(VK_CONTROL);
        }

        /// <summary>
        /// Checks if Shift key is pressed
        /// </summary>
        public static bool IsShiftPressed()
        {
            return IsKeyPressed(VK_SHIFT);
        }
        #endregion

        #region Key Scanning
        /// <summary>
        /// Scans for any key press and returns the virtual key code
        /// </summary>
        /// <param name="startKey">Starting virtual key code (default: 8)</param>
        /// <param name="endKey">Ending virtual key code (default: 255)</param>
        /// <returns>Virtual key code of the pressed key, or -1 if no key is pressed</returns>
        public static int ScanForKeyPress(int startKey = 8, int endKey = 255)
        {
            for (int i = startKey; i <= endKey; i++)
            {
                if (IsKeyJustPressed(i))
                {
                    return i;
                }
            }
            return -1;
        }
        #endregion

        #region Character Mapping
        /// <summary>
        /// Maps a virtual key code to its corresponding character using the current keyboard layout
        /// </summary>
        /// <param name="keyCode">Virtual key code</param>
        /// <returns>The character representation, or '\0' if mapping fails</returns>
        public static char MapKeyToCharacter(int keyCode)
        {
            var result = new StringBuilder(2);
            var keyboardState = new byte[256];

            if (!GetKeyboardState(keyboardState))
                return '\0';

            int charsWritten = ToUnicode((uint)keyCode, 0, keyboardState, result, result.Capacity, 0);
            return charsWritten == 1 ? result[0] : '\0';
        }

        /// <summary>
        /// Gets the current keyboard state
        /// </summary>
        /// <returns>Byte array representing the keyboard state, or null if failed</returns>
        public static byte[] GetCurrentKeyboardState()
        {
            var keyboardState = new byte[256];
            return GetKeyboardState(keyboardState) ? keyboardState : null;
        }
        #endregion

        #region Keyboard Input Info
        /// <summary>
        /// Contains information about a keyboard input event
        /// </summary>
        public class KeyboardInputInfo
        {
            public int VirtualKeyCode { get; set; }
            public char Character { get; set; }
            public bool IsCtrlPressed { get; set; }
            public bool IsShiftPressed { get; set; }
            public bool IsCapsLockActive { get; set; }
            public bool IsSpecialKey { get; set; }

            public KeyboardInputInfo(int keyCode)
            {
                VirtualKeyCode = keyCode;
                Character = MapKeyToCharacter(keyCode);
                IsCtrlPressed = InputHandler.IsCtrlPressed();
                IsShiftPressed = InputHandler.IsShiftPressed();
                IsCapsLockActive = InputHandler.IsCapsLockActive();
                IsSpecialKey = IsSpecialKeyCode(keyCode);
            }

            private bool IsSpecialKeyCode(int keyCode)
            {
                return keyCode == VK_BACKSPACE ||
                       keyCode == VK_TAB ||
                       keyCode == VK_RETURN ||
                       keyCode == VK_ESCAPE ||
                       keyCode == VK_DELETE ||
                       keyCode == VK_LEFT ||
                       keyCode == VK_RIGHT ||
                       keyCode == VK_UP ||
                       keyCode == VK_DOWN ||
                       keyCode == VK_HOME ||
                       keyCode == VK_END;
            }
        }

        /// <summary>
        /// Scans for keyboard input and returns detailed information
        /// </summary>
        /// <returns>KeyboardInputInfo if a key was pressed, null otherwise</returns>
        public static KeyboardInputInfo GetKeyboardInput()
        {
            int keyCode = ScanForKeyPress();
            return keyCode != -1 ? new KeyboardInputInfo(keyCode) : null;
        }
        #endregion
    }
}
