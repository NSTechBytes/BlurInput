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
//using System.Windows.Forms.Cursor;
using Rainmeter;
using System.Drawing;
using System.Diagnostics;
using System.Timers;
using System.Text.RegularExpressions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement;



namespace PluginBlurInput
{
  //  private API api; // Define 'api' as an instance variable

    internal class Measure
    {

        public API api; // Define 'api' as an instance variable

        public string myName; // To store the measure name
        public int Disabled; // To store the measure name
        private string MeterName = "";
        private string TextBuffer = "";
        private string OnEnterAction;
        private string OnESCAction;
        private string Cursor = "|";
        private int CursorPosition = 0;
        private Rainmeter.API Api;
        private string defaultValue = "";
        private int FormatMultiline = 0;
    

        private string DismissAction;

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

        private string TargetWindowTitle = string.Empty;
        private int UnFocusDismiss = 0;

        private System.Timers.Timer resetTimer;
        private bool hasResetOnce = false;

        private string substituteRule = "";
        private int useRegex = 0;

        private int MeterX, MeterY, MeterWidth, MeterHeight;
        private int SkinX, SkinY;

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

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        //=================================================================================================================================//
        //                                                      Reload                                                                     //
        //=================================================================================================================================//

        internal void Reload(Rainmeter.API api, ref double maxValue)
        {
          
            Api = api;

            TextBuffer = ApplySubstitution(TextBuffer, substituteRule, useRegex);

            updateTimer = new System.Timers.Timer(UpdateInterval);
            updateTimer.Elapsed += (sender, e) => UpdateTest();
            updateTimer.AutoReset = true;

            MeterName = api.ReadString("MeterName", "");
            FormatMultiline = api.ReadInt("FormatMultiline", 0);
            Disabled = api.ReadInt("Disabled", 0);

            Cursor = api.ReadString("Cursor", "|");
            IsPassword = api.ReadInt("Password", 0) == 1;
            IsMultiline = api.ReadInt("Multiline", 0) == 1;
            CharacterLimit = api.ReadInt("Limit", 0);
            OnEnterAction = api.ReadString("OnEnterAction", "").Trim();
            OnESCAction = api.ReadString("OnESCAction", "").Trim();
            defaultValue = api.ReadString("DefaultValue", "").Trim();
            Width = api.ReadInt("Width", 0);
            myName = api.GetMeasureName();

            UnFocusDismiss = api.ReadInt("SkinUnFocusDismiss", 0);

           // private string MeterName = "";
            string rootConfigPath = api.ReplaceVariables("#CURRENTPATH#");
            string currentFile = api.ReplaceVariables("#CURRENTFILE#");

               MeterX =  api.ReadInt("MeterX", 0);;
              MeterY = api.ReadInt("MeterY", 0);
            MeterWidth = api.ReadInt("MeterW",0);

            MeterHeight = api.ReadInt("MeterH",0);


            SkinX = int.Parse(api.ReplaceVariables("#CURRENTCONFIGX#"));
            SkinY = int.Parse(api.ReplaceVariables("#CURRENTCONFIGY#"));

            useRegex = api.ReadInt("RegExpSubstitute", 0);
            substituteRule = api.ReadString("Substitute", "");

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

            if (!string.IsNullOrEmpty(rootConfigPath) && !string.IsNullOrEmpty(currentFile))
            {
                TargetWindowTitle = $"{rootConfigPath}{currentFile}";
            }
            else
            {
                api.Log(API.LogType.Error, "WindowTitleMatch.dll: Invalid ROOTCONFIGPATH or CURRENTFILE values.");
            }
           // api.Log(API.LogType.Notice,$"Width {MeterWidth}.");

            // UpdateText();
        }








        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Update Helper to get ride from the default Rainmeter Section.
        // Due to the reasonable delay listner keys.
        // Not Find any proper solution. So Find this hacky Solution.

        internal void UpdateTest()
        {
           
            Api.Execute($"!UpdateMeasure  \"{myName}\"");
        }








        private Point GetMousePosition()
        {
            // Use fully qualified name to avoid conflicts
            return System.Windows.Forms.Cursor.Position;
        }

        private bool IsMouseInsideMeter(Point mousePosition)
        {
            // Add SkinX and SkinY to meter coordinates to get the global position
            int meterGlobalX = MeterX + SkinX;
            int meterGlobalY = MeterY + SkinY;

            // Check if the mouse is within the meter's boundaries
            return mousePosition.X >= meterGlobalX &&
                   mousePosition.X <= meterGlobalX + MeterWidth &&
                   mousePosition.Y >= meterGlobalY &&
                   mousePosition.Y <= meterGlobalY + MeterHeight;
        }
        //=================================================================================================================================//
        //                                                     Update                                                                      //
        //=================================================================================================================================//
        internal void Update()
        {
            if (Control.MouseButtons == MouseButtons.Left || Control.MouseButtons == MouseButtons.Right)
            {


                // Get the mouse position
                Point mousePosition = GetMousePosition();

                // Check if the mouse is inside or outside the meter
                if (IsMouseInsideMeter(mousePosition))
                {
                    Api.Log(API.LogType.Debug, "Mouse clicked inside the meter.");

                    Api.Execute($"!Log  \"Skin In Focus\"");
                }
                else
                {
                    if (UnFocusDismiss == 1 && IsActive)
                    {
                        //Api.Execute($"!Log  \"Skin In UnFocus\"");
                        ESCHandler();
                        Api.Execute(DismissAction);
                    }

                    Api.Log(API.LogType.Debug, "Mouse clicked outside the meter.");

                    Api.Execute($"!Log  \"Skin In UnFocus\"");
                }
            }



            bool ctrlPressed = (GetAsyncKeyState(17) & 0x8000) != 0;
            bool shiftPressed = (GetAsyncKeyState(16) & 0x8000) != 0;
            CapsLockActive = (GetKeyState(20) & 0x0001) != 0;


          /*  string foregroundTitle = GetForegroundWindowTitle();
            if (!string.IsNullOrEmpty(foregroundTitle) && TargetWindowTitle.Equals(foregroundTitle, StringComparison.OrdinalIgnoreCase))
            {
               //  Api.Execute($"!Log  \"Skin In Focus\"");
            }
            else
            {
                if (UnFocusDismiss == 1 && IsActive)
                {
                    //Api.Execute($"!Log  \"Skin In UnFocus\"");
                    ESCHandler();
                    Api.Execute(DismissAction);
                }
            }*/


            if (!IsActive || string.IsNullOrEmpty(MeterName))
                return;


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

            private System.Windows.Forms.Button CreateStyledButton(string text, Point location)
            {
                return new System.Windows.Forms.Button
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
        //                                                    Get Window Title                                                             //
        //=================================================================================================================================//

     /*   private string GetForegroundWindowTitle()
        {
            IntPtr hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero)
            {
                return null;
            }

            int length = GetWindowTextLength(hWnd);
            if (length == 0)
            {
                return null;
            }

            StringBuilder windowText = new StringBuilder(length + 1);
            GetWindowText(hWnd, windowText, windowText.Capacity);
            return windowText.ToString();
        }*/

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
                    ESCHandler();
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
                        Stop();
                        ValidateAndSubmitText();
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

            if (Disabled == 1) return;
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
                if (FormatMultiline == 1)
                {
                    TextBuffer = TextBuffer.Replace("\r\n", " ").Replace("\n", " ");
                }
            }
        }


        //=================================================================================================================================//
        //                                                      KeyBoard Functions                                                         //
        //=================================================================================================================================//

        // ESC Helper to Terminate for ESC key and 
        // for the unfocus skin.
        public void ESCHandler()
        {

            if (!IsActive) return;

            // Api.Execute($"!Log  \"Active\"");
            IsActive = false;
            hasResetOnce = false;
            Api.Execute(OnESCAction);

            TextBuffer = defaultValue;

            CursorPosition = 0;
            UndoStack.Clear();
            RedoStack.Clear();
            updateTimer.Stop();

            if (!string.IsNullOrEmpty(MeterName))
            {
                Api.Execute($"!SetOption  \"{MeterName}\" Text \"{defaultValue}\" ");
                Api.Execute($"!UpdateMeter  \"{MeterName}\" ");
                Api.Execute($"!Redraw");
            }
            Api.Execute(OnESCAction);
        }
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

      
            CursorPosition = Math.Max(0, Math.Min(CursorPosition, TextBuffer.Length));

          
            string displayText = IsPassword
                ? new string('*', TextBuffer.Length).Insert(CursorPosition, Cursor)
                : TextBuffer.Insert(CursorPosition, Cursor);

            
            string substituteRule = Api.ReadString("Substitute", "");
            if (!string.IsNullOrEmpty(substituteRule))
            {
                int originalLength = displayText.Length; 
                displayText = ApplySubstitution(displayText, substituteRule, useRegex);

                
                int newLength = displayText.Length;
                if (newLength != originalLength)
                {
                    int change = newLength - originalLength;
                    CursorPosition = Math.Max(0, Math.Min(CursorPosition + change, newLength));
                }
            }

            
            if (Width > 0 && displayText.Length > Width)
            {
                int startIndex = Math.Max(0, CursorPosition - Width / 2);
                startIndex = Math.Min(startIndex, displayText.Length - Width);
                displayText = displayText.Substring(startIndex, Width);
            }

            Api.Execute($"!SetOption  \"{MeterName}\" Text \"{displayText}\"");
            Api.Execute($"!UpdateMeter \"{MeterName}\"");
            Api.Execute("!Redraw");
        }


        private string ApplySubstitution(string text, string substituteRule, int useRegex = 1)
        {
            if (string.IsNullOrEmpty(substituteRule))
                return text;

            string[] rules = substituteRule.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string rule in rules)
            {
               
                if (TryParseRule(rule, out string pattern, out string replacement))
                {
                    if (string.IsNullOrEmpty(pattern))
                    {
                        Api.Log(API.LogType.Warning, $"Skipping substitution rule with empty pattern: {rule}");
                        continue;
                    }

                    try
                    {
                        if (useRegex==1)
                        {
                          
                            Regex.Match("", pattern);
                            text = Regex.Replace(text, pattern, replacement);
                        }
                        else
                        {
                            text = text.Replace(pattern, replacement);
                        }
                    }
                    catch (Exception ex)
                    {
                        Api.Log(API.LogType.Error, $"Failed to apply rule: {rule}. {ex.Message}");
                    }
                }
                else
                {
                    Api.Log(API.LogType.Warning, $"Invalid rule format: {rule}");
                }
            }

            return text;
        }

        private bool TryParseRule(string rule, out string pattern, out string replacement)
        {
            string[] parts = rule.Split(new[] { "\":\"", "\':'", "'\":\"", "'\':'" }, StringSplitOptions.None);
            if (parts.Length == 2)
            {
                pattern = parts[0].Trim('\"', '\'');
                replacement = parts[1].Trim('\"', '\'');
                return true;
            }
            pattern = replacement = null;
            return false;
        }

        // This handler is use to update the text and measure dynamically.
        // As this is to fix the unrecognize updation.

        private void UpdateMeasure()
        {
           
            if (!IsActive || string.IsNullOrEmpty(MeterName))
                return;

            Api.Execute($"!SetOption  \"{MeterName}\" Text \"{TextBuffer}\" ");
            Api.Execute($"!SetOption  \"{myName}\" DefaultValue \"{TextBuffer}\" ");
            Api.Execute($"!UpdateMeter   \"{MeterName}\" ");
            Api.Execute($"!UpdateMeasure  \"{myName}\" ");
          //Api.Execute($"!Log \"{myName}\"");

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

        internal void GetPos()
        {
            Api.Execute($"!SetOption  \"{myName}\" MeterX \"[{MeterName}:X]\" ");
            Api.Execute($"!SetOption  \"{myName}\" MeterY \"[{MeterName}:Y]\" ");
            Api.Execute($"!SetOption  \"{myName}\" MeterW \"[{MeterName}:W]\" ");
            Api.Execute($"!SetOption  \"{myName}\" MeterH \"[{MeterName}:H]\" ");
            Api.Execute($"!UpdateMeasure  \"{myName}\"");
        }

        internal void Start()
        {
            if (Disabled == 1) return;

            if (IsActive)
            {
                Api.Log(API.LogType.Debug, "Plugin is already running. Start operation skipped.");
                return;
            }

            IsActive = true;
            hasResetOnce = false;
            GetPos();
            CursorPosition = TextBuffer.Length;
            updateTimer.Start();
         //Api.Execute($"!Log  \"{TextBuffer}\"");


            if (!hasResetOnce)
            {
                hasResetOnce = true;
                resetTimer = new System.Timers.Timer(50);
                resetTimer.Elapsed += (sender, e) =>
                { 
                    ResetToDefaultValue();
                    resetTimer.Stop();
                };
                resetTimer.AutoReset = false;
                resetTimer.Start();
            }

        }

        // This is used to reset textbuffer to default value as
        // the plugin why not listening when the plugin is Stop.
        private void ResetToDefaultValue()
        {
            TextBuffer = defaultValue;
            CursorPosition = TextBuffer.Length;
            UndoStack.Clear();
            RedoStack.Clear();

            UpdateText();
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
            if (!IsActive)
                return;

            IsActive = false;
            hasResetOnce = false;

            //TextBuffer = defaultValue;
            CursorPosition = 0;
            UndoStack.Clear();
            RedoStack.Clear();
            updateTimer.Stop();
            updateTimer.Stop();
            //UpdateMeasure();

            /* if (!string.IsNullOrEmpty(MeterName))
             {
                 Api.Execute($"!SetOption  \"{MeterName}\" Text \"{TextBuffer}\" ");
                 Api.Execute($"!UpdateMeter   \"{MeterName}\" ");
                 Api.Execute($"!Redraw");
             }*/

            //  TextBuffer = defaultValue;
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

      /*  // New Function: GetWidth
        [DllExport]
        public static double GetWidth(IntPtr data)
        {
            var measure = (Measure)GCHandle.FromIntPtr(data).Target;
            return measure.api.GetMeterWidth();
        }*/




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