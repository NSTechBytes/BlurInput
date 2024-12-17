using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Rainmeter;

namespace PluginBlurInput
{
    internal class Measure
    {
        private string MeterName = "";
        private string TextBuffer = "";
        private string OnEnterAction;
        private string OnESCAction;
        private string Cursor = "|";
        private int CursorPosition = 0;
        private Rainmeter.API Api;
        private string defaultValue = "";

        private Stack<string> UndoStack = new Stack<string>();
        private Stack<string> RedoStack = new Stack<string>();

        private bool CapsLockActive = false;
        private const string TabSpaces = "    ";

        private bool IsActive = false;
        private bool IsPassword = false;
        private bool IsMultiline = false;
        private int CharacterLimit = 0;
        private int Width = 0;
        private string InputType = "String"; // Default to "String"
        private string AllowedCharacters = ""; // For InputType=Custom

        public string GetUserInput()
        {
            return TextBuffer; // Return the current user input
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        private static extern int ToAscii(
            uint uVirtKey,
            uint uScanCode,
            byte[] lpKeyState,
            [Out] StringBuilder lpChar,
            uint uFlags
        );

        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] lpKeyState);

        internal void Reload(Rainmeter.API api, ref double maxValue)
        {
            Api = api;

            MeterName = api.ReadString("MeterName", "");
            Cursor = api.ReadString("Cursor", "|");
            IsPassword = api.ReadInt("Password", 0) == 1;
            IsMultiline = api.ReadInt("Multiline", 0) == 1;
            CharacterLimit = api.ReadInt("Limit", 0);
            Width = api.ReadInt("Width", 0);
            OnEnterAction = api.ReadString("OnEnterAction", "").Trim();
            OnESCAction = api.ReadString("OnESCAction", "").Trim();
            defaultValue = api.ReadString("DefaultValue", "").Trim();

            // Fetch the default value from the measure

            // Read and validate InputType
            InputType = api.ReadString("InputType", "String").Trim();
            if (
                InputType != "String"
                && InputType != "Integer"
                && InputType != "Float"
                && InputType != "Letters"
                && InputType != "Alphanumeric"
                && InputType != "Hexadecimal"
                && InputType != "Email"
                && InputType != "Custom"
            )
            {
                api.Log(
                    API.LogType.Warning,
                    $"Invalid InputType '{InputType}', defaulting to 'String'."
                );
                InputType = "String";
            }

            // Custom character set for InputType=Custom
            if (InputType == "Custom")
            {
                AllowedCharacters = api.ReadString("AllowedCharacters", ""); // User-defined character set
                if (string.IsNullOrEmpty(AllowedCharacters))
                {
                    api.Log(
                        API.LogType.Warning,
                        "InputType 'Custom' requires 'AllowedCharacters'. Defaulting to 'String'."
                    );
                    InputType = "String";
                }
            }

            if (string.IsNullOrEmpty(MeterName))
            {
                api.Log(API.LogType.Error, "BlurInput.dll: MeterName is required.");
            }
            else
            {
                TextBuffer =
                    defaultValue.Length > CharacterLimit && CharacterLimit > 0
                        ? defaultValue.Substring(0, CharacterLimit)
                        : defaultValue;

                CursorPosition = TextBuffer.Length;

                // Truncate text if the limit is reduced dynamically
                if (CharacterLimit > 0 && TextBuffer.Length > CharacterLimit)
                {
                    TextBuffer = TextBuffer.Substring(0, CharacterLimit);
                    CursorPosition = Math.Min(CursorPosition, CharacterLimit);
                }
            }

            UpdateText(); // Ensure the meter text is updated with the cursor
        }

        private void InsertText(string text)
        {
            if (CharacterLimit > 0 && TextBuffer.Length + text.Length > CharacterLimit)
            {
                text = text.Substring(0, CharacterLimit - TextBuffer.Length);
            }

            if (!string.IsNullOrEmpty(text))
            {
                TextBuffer = TextBuffer.Insert(CursorPosition, text);
                CursorPosition += text.Length;
            }
        }

        internal void Update()
        {
            if (!IsActive || string.IsNullOrEmpty(MeterName))
                return;

            bool ctrlPressed = (GetAsyncKeyState(17) & 0x8000) != 0;
            bool shiftPressed = (GetAsyncKeyState(16) & 0x8000) != 0;
            CapsLockActive = (GetKeyState(20) & 0x0001) != 0;

            for (int i = 8; i <= 255; i++)
            {
                if ((GetAsyncKeyState(i) & 0x0001) != 0)
                {
                    if (i == 13 && ctrlPressed)
                    {
                        HandleCtrlEnter();
                        return;
                    }

                    if (ctrlPressed)
                    {
                        HandleCtrlShortcuts(i);
                    }
                    else
                    {
                        HandleSpecialKeys(i, shiftPressed, ctrlPressed);
                    }

                    UpdateText();
                }
            }
        }

        private void HandleCtrlShortcuts(int keyCode)
        {
            switch (keyCode)
            {
                case 67:
                    CopyToClipboard();
                    break;
                case 86:
                    PasteFromClipboard();
                    break;
                case 88:
                    CutToClipboard();
                    break;
                case 90:
                    Undo();
                    break;
                case 89:
                    Redo();
                    break;
            }
        }

        private void HandleSpecialKeys(int keyCode, bool shiftPressed, bool ctrlPressed)
        {
            SaveStateForUndo();

            switch (keyCode)
            {
                case 8: // Backspace
                    if (CursorPosition > 0)
                    {
                        TextBuffer = TextBuffer.Remove(CursorPosition - 1, 1);
                        CursorPosition--;
                    }
                    return;

                case 27: // Escape
                    Api.Execute(OnESCAction);
                    Stop();
                    return;

                case 13: // Enter
                    if (IsMultiline && !ctrlPressed) // Multiline mode: insert newline
                    {
                        InsertText("\n");
                    }
                    else // Single-line mode or Ctrl+Enter: submit text
                    {
                        ValidateAndSubmitText();
                        Stop(); // Stop the plugin after returning value
                    }
                    return;

                case 46: // Delete
                    if (CursorPosition < TextBuffer.Length)
                    {
                        TextBuffer = TextBuffer.Remove(CursorPosition, 1);
                    }
                    return;

                case 37: // Left Arrow
                    if (CursorPosition > 0)
                        CursorPosition--;
                    return;

                case 39: // Right Arrow
                    if (CursorPosition < TextBuffer.Length)
                        CursorPosition++;
                    return;

                case 36: // Home
                    CursorPosition = 0; // Move cursor to the beginning
                    return;

                case 35: // End
                    CursorPosition = TextBuffer.Length; // Move cursor to the end
                    return;

                case 9: // Tab
                    TextBuffer = TextBuffer.Insert(CursorPosition, TabSpaces);
                    CursorPosition += TabSpaces.Length; // Move cursor after the inserted spaces
                    return;

                case 20: // Caps Lock
                    CapsLockActive = !CapsLockActive; // Toggle Caps Lock state
                    return;
            }

            char keyChar = MapKeyToCharacterDynamic(keyCode);

            if (keyChar != '\0') // If it's a valid printable character
            {
                if (IsValidInput(keyChar)) // Validate based on InputType
                {
                    InsertText(keyChar.ToString());
                }
            }
        }

        private bool IsValidInput(char keyChar)
        {
            switch (InputType)
            {
                case "String":
                    return true; // Allow all characters
                case "Integer":
                    return char.IsDigit(keyChar) || (keyChar == '-' && CursorPosition == 0); // Allow digits and a negative sign at the start
                case "Letters":
                    return char.IsLetter(keyChar); // Allow only alphabetic characters
                case "Default":
                    return true; // Default behavior (accept everything initially)
                default:
                    return true; // Fallback
            }
        }

        private void ValidateAndSubmitText()
        {
            switch (InputType)
            {
                case "Integer":
                    if (!int.TryParse(TextBuffer, out _))
                    {
                        TextBuffer = Api.ReadString("DefaultValue", "0"); // Revert to default value
                    }
                    break;

                case "Float":
                    if (!float.TryParse(TextBuffer, out _))
                    {
                        TextBuffer = Api.ReadString("DefaultValue", "0.0"); // Revert to default value
                    }
                    break;

                case "Letters":
                    if (!IsAllLetters(TextBuffer))
                    {
                        TextBuffer = Api.ReadString("DefaultValue", ""); // Revert to default value
                    }
                    break;

                case "Alphanumeric":
                    if (!IsAllAlphanumeric(TextBuffer))
                    {
                        TextBuffer = Api.ReadString("DefaultValue", ""); // Revert to default value
                    }
                    break;

                case "Hexadecimal":
                    if (!IsHexadecimal(TextBuffer))
                    {
                        TextBuffer = Api.ReadString("DefaultValue", ""); // Revert to default value
                    }
                    break;

                case "Email":
                    if (!IsValidEmail(TextBuffer))
                    {
                        TextBuffer = Api.ReadString("DefaultValue", ""); // Revert to default value
                    }
                    break;

                case "Custom":
                    if (!IsValidCustom(TextBuffer))
                    {
                        TextBuffer = Api.ReadString("DefaultValue", ""); // Revert to default value
                    }
                    break;
            }

            ReturnValueToRainmeter(); // Submit the validated value
        }

        private bool IsAllLetters(string input)
        {
            foreach (char c in input)
            {
                if (!char.IsLetter(c))
                    return false;
            }
            return true;
        }

        private bool IsAllAlphanumeric(string input)
        {
            foreach (char c in input)
            {
                if (!char.IsLetterOrDigit(c))
                    return false;
            }
            return true;
        }

        private bool IsHexadecimal(string input)
        {
            foreach (char c in input)
            {
                if (!char.IsDigit(c) && !"ABCDEFabcdef".Contains(c))
                    return false;
            }
            return true;
        }

        private bool IsValidEmail(string input)
        {
            // Simplistic email validation
            return input.Contains("@")
                && input.Contains(".")
                && !input.StartsWith("@")
                && !input.EndsWith(".");
        }

        private bool IsValidCustom(string input)
        {
            foreach (char c in input)
            {
                if (!AllowedCharacters.Contains(c))
                    return false;
            }
            return true;
        }

        private void HandleCtrlEnter()
        {
            ReturnValueToRainmeter();
            Api.Execute(OnEnterAction);
            Stop();
        }

        private void ReturnValueToRainmeter()
        {
            if (!string.IsNullOrEmpty(MeterName))
            {
                string returnValue = TextBuffer;
                Api.Log(API.LogType.Notice, $"Input: {returnValue}");
            }
        }

        private char MapKeyToCharacterDynamic(int keyCode)
        {
            StringBuilder result = new StringBuilder(2);

            byte[] keyboardState = new byte[256];
            if (!GetKeyboardState(keyboardState))
            {
                return '\0';
            }

            int charsWritten = ToAscii((uint)keyCode, 0, keyboardState, result, 0);

            if (charsWritten == 1)
            {
                return result[0];
            }

            return '\0';
        }

        private void CopyToClipboard()
        {
            if (!string.IsNullOrEmpty(TextBuffer))
            {
                Clipboard.SetText(TextBuffer);
            }
        }

        private void PasteFromClipboard()
        {
            if (Clipboard.ContainsText())
            {
                SaveStateForUndo();
                string clipboardText = Clipboard.GetText();
                InsertText(clipboardText);
            }
        }

        private void CutToClipboard()
        {
            if (!string.IsNullOrEmpty(TextBuffer))
            {
                Clipboard.SetText(TextBuffer);
                SaveStateForUndo();
                TextBuffer = "";
                CursorPosition = 0;
            }
        }

        private void Undo()
        {
            if (UndoStack.Count > 0)
            {
                RedoStack.Push(TextBuffer);
                TextBuffer = UndoStack.Pop();
                CursorPosition = TextBuffer.Length;
            }
        }

        private void Redo()
        {
            if (RedoStack.Count > 0)
            {
                UndoStack.Push(TextBuffer);
                TextBuffer = RedoStack.Pop();
                CursorPosition = TextBuffer.Length;
            }
        }

        private void SaveStateForUndo()
        {
            UndoStack.Push(TextBuffer);
            RedoStack.Clear();
        }

        private void UpdateText()
        {
            if (!IsActive || string.IsNullOrEmpty(MeterName))
                return;

            // Render text buffer with cursor
            string displayText = IsPassword
                ? new string('*', TextBuffer.Length).Insert(CursorPosition, Cursor)
                : TextBuffer.Insert(CursorPosition, Cursor);

            if (Width > 0 && displayText.Length > Width)
            {
                // Truncate the text to fit within the specified width
                int startIndex = Math.Max(0, CursorPosition - Width / 2);
                startIndex = Math.Min(startIndex, displayText.Length - Width);
                displayText = displayText.Substring(startIndex, Width);
            }

            // Update the meter text
            Api.Execute($"!SetOption {MeterName} Text \"{displayText}\"");
            Api.Execute("!UpdateMeter *");
            Api.Execute("!Redraw");
        }

        internal void ClearText()
        {
            TextBuffer = Api.ReadString("DefaultValue", "").Trim(); // Reset to default value
            CursorPosition = TextBuffer.Length;
            UndoStack.Clear();
            RedoStack.Clear();
            UpdateText();
        }

        internal void Start()
        {
            IsActive = true; // Activate the plugin

            // Ensure the meter text reflects the current TextBuffer and cursor
            CursorPosition = TextBuffer.Length; // Place the cursor at the end
            UpdateText();
        }

        internal void Stop()
        {
            IsActive = false; // Deactivate the plugin
            TextBuffer = defaultValue; // Clear the internal text buffer
            CursorPosition = 0;
            UndoStack.Clear();
            RedoStack.Clear();

            if (!string.IsNullOrEmpty(MeterName))
            {
                Api.Execute($"!SetOption {MeterName} Text \"{defaultValue}\""); // Reset the meter text
                Api.Execute("!UpdateMeter *");
                Api.Execute("!Redraw");
            }
        }
    }

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

            if (args.Equals("Start", StringComparison.OrdinalIgnoreCase))
            {
                measure.Start();
            }
            else if (args.Equals("Stop", StringComparison.OrdinalIgnoreCase))
            {
                measure.Stop();
            }
            else if (args.Equals("ClearText", StringComparison.OrdinalIgnoreCase))
            {
                measure.ClearText();
            }
        }

        // Add GetString to return user input
        [DllExport]
        public static IntPtr GetString(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            string result = measure.GetUserInput(); // Retrieve user input from Measure

            if (stringBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(stringBuffer);
            }

            stringBuffer = Marshal.StringToHGlobalUni(result); // Pass the string to Rainmeter
            return stringBuffer;
        }
    }
}
