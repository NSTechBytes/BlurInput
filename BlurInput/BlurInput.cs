/*=================================================================================================================================================//
[Rainmeter]
Update=0

[InputHandler]
Measure=Plugin
Plugin=BlurInput
MeasureName=#CURRENTSECTION#
MeterName=
Cursor=|
Password= (0,1)
Multiline= (0,1)
Limit= (0 for not Limit fix)
Width= (0 for not Width fix)
FormatMultiline=0
DefaultValue=
InputType= (String,Integer,Float,Letters,Alphanumeric,Hexadecimal,Email,Custom) any one
OnEnterAction=[!Log "Log:[InputHandler]"]
OnESCAction=[!Log "[InputHandler]"]
DynamicVariables=1
==================================================================================================================================================*/

//=================================================================================================================================//
//                                                   ||||  Main Code  |||||                                                        //
//=================================================================================================================================//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Rainmeter;
using System.Drawing;
using System.Diagnostics;
using System.Timers;



namespace PluginBlurInput
{
    internal class Measure
    {
        private string MeterName = "";
        private string MeasureName = "";
        private string TextBuffer = "";
        private string OnEnterAction;
        private string OnESCAction;
        private string Cursor = "|";
        private int CursorPosition = 0;
        private Rainmeter.API Api;
        private string defaultValue = "";
        private int FormatMultiline = 0;

        private Stack<string> UndoStack = new Stack<string>();
        private Stack<string> RedoStack = new Stack<string>();

        private bool CapsLockActive = false;
        private const string TabSpaces = "    ";

        private bool IsActive = false;
        private bool IsPassword = false;
        private bool IsMultiline = false;
        private int CharacterLimit = 0;
        private int Width = 0;

        private string InputType = "String";
        private string AllowedCharacters = "";
        private bool isTextCleared = false;
        private bool isInitialized = false;
        private System.Timers.Timer updateTimer;
        private const double UpdateInterval = 25; 
      

        public string GetUserInput()
        {
            return TextBuffer;
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        private static extern int ToUnicode(
            uint virtualKey,
            uint scanCode,
            byte[] keyboardState,
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder receivingBuffer,
            int bufferSize,
            uint flags);

        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] lpKeyState);

        //=================================================================================================================================//
        //                                                      Reload                                                                     //
        //=================================================================================================================================//

        internal void Reload(Rainmeter.API api, ref double maxValue)
        {
            Api = api;

            updateTimer = new System.Timers.Timer(UpdateInterval);
            updateTimer.Elapsed += (sender, e) => Update();
            updateTimer.AutoReset = true;
      

            MeterName = api.ReadString("MeterName", "");
            FormatMultiline = api.ReadInt("FormatMultiline", 0);
            MeasureName = api.ReadString("MeasureName", "");
            Cursor = api.ReadString("Cursor", "|");
            IsPassword = api.ReadInt("Password", 0) == 1;
            IsMultiline = api.ReadInt("Multiline", 0) == 1;
            CharacterLimit = api.ReadInt("Limit", 0);
            OnEnterAction = api.ReadString("OnEnterAction", "").Trim();
            OnESCAction = api.ReadString("OnESCAction", "").Trim();
            defaultValue = api.ReadString("DefaultValue", "").Trim();
            Width = api.ReadInt("Width", 0);

            InputType = api.ReadString("InputType", "String").Trim();
            if (InputType != "String" && InputType != "Integer" && InputType != "Float" &&
                InputType != "Letters" && InputType != "Alphanumeric" && InputType != "Hexadecimal" &&
                InputType != "Email" && InputType != "Custom")
            {
                api.Log(API.LogType.Warning, $"Invalid InputType '{InputType}', defaulting to 'String'.");
                InputType = "String";
            }

            if (InputType == "Custom")
            {
                AllowedCharacters = api.ReadString("AllowedCharacters", "");
                if (string.IsNullOrEmpty(AllowedCharacters))
                {
                    api.Log(API.LogType.Warning, "InputType 'Custom' requires 'AllowedCharacters'. Defaulting to 'String'.");
                    InputType = "String";
                }
            }

            if (!isInitialized)
            {
                TextBuffer = defaultValue.Length > CharacterLimit && CharacterLimit > 0
                    ? defaultValue.Substring(0, CharacterLimit)
                    : defaultValue;

                CursorPosition = TextBuffer.Length;
                isInitialized = true;
            }


            if (CharacterLimit > 0 && TextBuffer.Length > CharacterLimit)
            {
                TextBuffer = TextBuffer.Substring(0, CharacterLimit);
                CursorPosition = Math.Min(CursorPosition, CharacterLimit);
            }

            UpdateText();
        }

        //=================================================================================================================================//
        //                                                     Update                                                                      //
        //=================================================================================================================================//
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
        //=================================================================================================================================//
        //                                                      Context                                                                   //
        //=================================================================================================================================//
        public class ContextForm : Form
        {
            private readonly Measure _measure;

            public ContextForm(Measure measure)
            {
                _measure = measure;


                Text = "BlurInput Menu";
                Size = new Size(300, 350);
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                StartPosition = FormStartPosition.CenterScreen;
                BackColor = Color.FromArgb(30, 30, 30);


                var undoButton = CreateStyledButton("Undo", new Point(75, 30));
                var redoButton = CreateStyledButton("Redo", new Point(75, 80));
                var copyButton = CreateStyledButton("Copy", new Point(75, 130));
                var pasteButton = CreateStyledButton("Paste", new Point(75, 180));
                var clearButton = CreateStyledButton("Clear Text", new Point(75, 230));


                undoButton.Click += (s, e) => { _measure.Undo(); Close(); };
                redoButton.Click += (s, e) => { _measure.Redo(); Close(); };
                copyButton.Click += (s, e) => { _measure.CopyToClipboard(); Close(); };
                pasteButton.Click += (s, e) => { _measure.PasteFromClipboard(); Close(); };
                clearButton.Click += (s, e) => { _measure.ClearText(); Close(); };

                Controls.Add(undoButton);
                Controls.Add(redoButton);
                Controls.Add(copyButton);
                Controls.Add(pasteButton);
                Controls.Add(clearButton);
            }

            private Button CreateStyledButton(string text, Point location)
            {
                return new Button
                {
                    Text = text,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(70, 70, 70),
                    FlatStyle = FlatStyle.Flat,
                    FlatAppearance = { BorderSize = 0 },
                    Location = location,
                    Size = new Size(150, 40),
                    Cursor = Cursors.Hand
                };
            }
        }

        //=================================================================================================================================//
        //                                                      KeyBoardControl                                                            //
        //=================================================================================================================================//
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
                case 8:
                    if (CursorPosition > 0)
                    {
                        TextBuffer = TextBuffer.Remove(CursorPosition - 1, 1);
                        CursorPosition--;
                    }
                    return;

                case 27:
                    Api.Execute(OnESCAction);
                    Stop();
                    return;

                case 13:
                    if (IsMultiline && !ctrlPressed)
                    {
                        InsertText("\n");
                    }
                    else
                    {
                        UpdateMeasure();
                        Api.Execute(OnEnterAction);
                        ValidateAndSubmitText();
                        Stop();
                    }
                    return;

                case 46:
                    if (CursorPosition < TextBuffer.Length)
                    {
                        TextBuffer = TextBuffer.Remove(CursorPosition, 1);
                    }
                    return;

                case 37:
                    if (CursorPosition > 0)
                        CursorPosition--;
                    return;

                case 39:
                    if (CursorPosition < TextBuffer.Length)
                        CursorPosition++;
                    return;

                case 36:
                    CursorPosition = 0;
                    return;

                case 35:
                    CursorPosition = TextBuffer.Length;
                    return;

                case 9:
                    TextBuffer = TextBuffer.Insert(CursorPosition, TabSpaces);
                    CursorPosition += TabSpaces.Length;
                    return;

                case 20:
                    CapsLockActive = !CapsLockActive;
                    return;
            }

            char keyChar = MapKeyToCharacterDynamic(keyCode);

            if (keyChar != '\0')
            {
                if (IsValidInput(keyChar))
                {
                    InsertText(keyChar.ToString());
                }
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

            int charsWritten = ToUnicode((uint)keyCode, 0, keyboardState, result, result.Capacity, 0);

            if (charsWritten == 1)
            {
                return result[0];
            }

            return '\0';
        }

        //=================================================================================================================================//
        //                                                      ValidInputs                                                                //
        //=================================================================================================================================//
        private bool IsValidInput(char keyChar)
        {
            switch (InputType)
            {
                case "String":
                    return true;
                case "Integer":
                    return char.IsDigit(keyChar) || (keyChar == '-' && CursorPosition == 0);
                case "Letters":
                    return char.IsLetter(keyChar);
                case "Default":
                    return true;
                default:
                    return true;
            }
        }

        private void ValidateAndSubmitText()
        {

            if (string.IsNullOrEmpty(TextBuffer))
            {
                isTextCleared = true;
                return;
            }

            isTextCleared = false;

            switch (InputType)
            {
                case "Integer":
                    if (!int.TryParse(TextBuffer, out _))
                    {
                        ResetToDefault();
                    }
                    break;

                case "Float":
                    if (!float.TryParse(TextBuffer, out _))
                    {
                        ResetToDefault();
                    }
                    break;

                case "Letters":
                    if (!IsAllLetters(TextBuffer))
                    {
                        ResetToDefault();
                    }
                    break;

                case "Alphanumeric":
                    if (!IsAllAlphanumeric(TextBuffer))
                    {
                        ResetToDefault();
                    }
                    break;

                case "Hexadecimal":
                    if (!IsHexadecimal(TextBuffer))
                    {
                        ResetToDefault();
                    }
                    break;

                case "Email":
                    if (!IsValidEmail(TextBuffer))
                    {
                        ResetToDefault();
                    }
                    break;

                case "Custom":
                    if (!IsValidCustom(TextBuffer))
                    {
                        ResetToDefault();
                    }
                    break;
            }

            UpdateText();
        }
        private void ResetToDefault()
        {
            if (!isTextCleared)
            {
                TextBuffer = Api.ReadString("DefaultValue", "");
            }
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

        internal void ConvertTextBufferToSingleLine()
        {
            if (!string.IsNullOrEmpty(TextBuffer))
            {
                if(FormatMultiline == 1)
                {
                    TextBuffer = TextBuffer.Replace("\r\n", " ").Replace("\n", " ");
                }
            }
        }


        //=================================================================================================================================//
        //                                                      KeyBoard Functions                                                         //
        //=================================================================================================================================//

        private void HandleCtrlEnter()
        {
            ConvertTextBufferToSingleLine();
            UpdateMeasure();
            Api.Execute(OnEnterAction);
            Stop();
        }
     
        internal void CopyToClipboard()
        {
            if (!string.IsNullOrEmpty(TextBuffer))
            {
                Clipboard.SetText(TextBuffer);
            }
        }
        internal void PasteFromClipboard()
        {
            if (Clipboard.ContainsText())
            {

                SaveStateForUndo();
                string clipboardText = Clipboard.GetText();
                InsertText(clipboardText.Trim());
            }
            else
            {

                Api.Log(API.LogType.Warning, "Clipboard does not contain text.");
            }
        }
        internal void CutToClipboard()
        {
            if (!string.IsNullOrEmpty(TextBuffer))
            {
                Clipboard.SetText(TextBuffer);
                SaveStateForUndo();
                TextBuffer = "";
                CursorPosition = 0;
            }
        }
        internal void Undo()
        {
            if (UndoStack.Count > 0)
            {
                RedoStack.Push(TextBuffer);
                TextBuffer = UndoStack.Pop();
                CursorPosition = TextBuffer.Length;
            }
        }
        internal void Redo()
        {
            if (RedoStack.Count > 0)
            {
                UndoStack.Push(TextBuffer);
                TextBuffer = RedoStack.Pop();
                CursorPosition = TextBuffer.Length;
            }
        }
        internal void SaveStateForUndo()
        {
            UndoStack.Push(TextBuffer);
            RedoStack.Clear();
        }
        //=================================================================================================================================//
        //                                                     CommonFunctions                                                             //
        //=================================================================================================================================//

        private void InsertText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (CharacterLimit > 0 && TextBuffer.Length + text.Length > CharacterLimit)
            {
                text = text.Substring(0, CharacterLimit - TextBuffer.Length);
            }
            TextBuffer = TextBuffer.Insert(CursorPosition, text);
            CursorPosition = Math.Min(TextBuffer.Length, CursorPosition + text.Length);
        }
        private void UpdateText()
        {
            if (!IsActive || string.IsNullOrEmpty(MeterName))
                return;

            string displayText = IsPassword
                ? new string('*', TextBuffer.Length).Insert(CursorPosition, Cursor)
                : TextBuffer.Insert(CursorPosition, Cursor);

            if (Width > 0 && displayText.Length > Width)
            {
                int startIndex = Math.Max(0, CursorPosition - Width / 2);
                startIndex = Math.Min(startIndex, displayText.Length - Width);
                displayText = displayText.Substring(startIndex, Width);
            }

            Api.Execute($"!SetOption {MeterName} Text \"{displayText}\"");
            Api.Execute($"!UpdateMeter *");          
            Api.Execute($"!Redraw");
        }

        private void UpdateMeasure()
        {
            if (!IsActive || string.IsNullOrEmpty(MeasureName) || string.IsNullOrEmpty(MeterName))
                return;

            Api.Execute($"!SetOption {MeterName} Text \"{TextBuffer}\"");
            Api.Execute($"!SetOption {MeasureName} DefaultValue \"{TextBuffer}\"");
            Api.Execute($"!UpdateMeter {MeterName}");
            Api.Execute($"!UpdateMeasure {MeasureName}");
          
        }

        //=================================================================================================================================//
        //                                                     Bangs Functions                                                             //
        //=================================================================================================================================//

        internal void ClearText()
        {
            TextBuffer = "";
            CursorPosition = 0;
            UndoStack.Clear();
            RedoStack.Clear();
            UpdateText();
        }
        internal void Start()
        {
            IsActive = true;
            CursorPosition = TextBuffer.Length;
            UpdateText();
            updateTimer.Start();
        }
        internal void ShowContextForm()
        {
            if (!IsActive)
                return;


            ContextForm contextForm = new ContextForm(this);
            contextForm.ShowDialog();
        }
        internal void Stop()
        {
            IsActive = false;
            TextBuffer = defaultValue;
            CursorPosition = 0;
            UndoStack.Clear();
            RedoStack.Clear();
            updateTimer.Stop();
            UpdateMeasure();

            if (!string.IsNullOrEmpty(MeterName))
            {
                Api.Execute($"!SetOption {MeterName} Text \"{defaultValue}\"");
                Api.Execute($"!UpdateMeter *");
                Api.Execute($"!Redraw");
            }
        }
    }
    //=================================================================================================================================//
    //                                                     Rainmeter Class                                                             //
    //=================================================================================================================================//
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
}