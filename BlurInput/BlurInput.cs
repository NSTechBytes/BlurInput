using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Rainmeter;
using System.Drawing;
using System.Diagnostics;
using System.Text.RegularExpressions;


namespace PluginBlurInput
{
    internal class Measure
    {
        public API api;
        public string myName;
        public int Disabled;
        private string MeterName = "";
        private string TextBuffer = "";
        private string OnEnterAction;
        private string OnESCAction;
        private string Cursor = "|";
        private int CursorPosition = 0;
        private Rainmeter.API Api;
        private string defaultValue = "";
        private int FormatMultiline = 0;
        private int ShowErrorForm = 1;
        private string DismissAction;
        private int ForceValidInput = 1;
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
        private int UnFocusDismiss = 0;
        private System.Timers.Timer resetTimer;
        private bool hasResetOnce = false;
        private string substituteRule = "";
        private int useRegex = 0;
        private int MeterX, MeterY, MeterWidth, MeterHeight;
        private int SkinX, SkinY;
        private bool ContextFocusForm = false;
        private bool ContextFormOpen = false;
        private string UnValidAction = "";
        private Color BackgroundColor = Color.FromArgb(30, 30, 30); // Default color
        private Color ButtonColor = Color.FromArgb(70, 70, 70);     // Default button color
        private Color TextColor = Color.White;                     // Default text color

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


        public string GetUserInput()
        {
            return TextBuffer;
        }
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
            UnFocusDismiss = api.ReadInt("UnFocusDismiss", 0);
            DismissAction = api.ReadString("DismissAction", "");
            MeterX = api.ReadInt("MeterX", 0); ;
            MeterY = api.ReadInt("MeterY", 0);
            MeterWidth = api.ReadInt("MeterW", 0);
            MeterHeight = api.ReadInt("MeterH", 0);
            SkinX = int.Parse(api.ReplaceVariables("#CURRENTCONFIGX#"));
            SkinY = int.Parse(api.ReplaceVariables("#CURRENTCONFIGY#"));
            useRegex = api.ReadInt("RegExpSubstitute", 0);
            substituteRule = api.ReadString("Substitute", "");
            ShowErrorForm = api.ReadInt("ShowErrorForm", 0);
            ForceValidInput = api.ReadInt("ForceValidInput", 0);
            UnValidAction = api.ReadString("UnValidInputAction", "");
            string backgroundColorString = api.ReadString("BackgroundColor", "30,30,30");
            string buttonColorString = api.ReadString("ButtonColor", "70,70,70");
            string textColorString = api.ReadString("TextColor", "255,255,255");
            BackgroundColor = ParseColor(backgroundColorString, BackgroundColor);
            ButtonColor = ParseColor(buttonColorString, ButtonColor);
            TextColor = ParseColor(textColorString, TextColor);


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

        }

        internal void UpdateTest()
        {
            if (!IsActive)
                return;
            GetPos();
        }

        internal void GetPos()
        {
            Api.Execute($"!SetOption  \"{myName}\" MeterX \"[{MeterName}:X]\" ");
            Api.Execute($"!SetOption  \"{myName}\" MeterY \"[{MeterName}:Y]\" ");
            Api.Execute($"!SetOption  \"{myName}\" MeterW \"[{MeterName}:W]\" ");
            Api.Execute($"!SetOption  \"{myName}\" MeterH \"[{MeterName}:H]\" ");
            Api.Execute($"!UpdateMeasure  \"{myName}\"");
        }

        private Point GetMousePosition()
        {

            return System.Windows.Forms.Cursor.Position;
        }

        private bool IsMouseInsideMeter(Point mousePosition)
        {

            int meterGlobalX = MeterX + SkinX;
            int meterGlobalY = MeterY + SkinY;


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
            if (!IsActive || string.IsNullOrEmpty(MeterName))
                return;

            if (Control.MouseButtons == MouseButtons.Left || Control.MouseButtons == MouseButtons.Right)
            {

                Point mousePosition = GetMousePosition();

                if (IsMouseInsideMeter(mousePosition))
                {


                    if (Control.MouseButtons == MouseButtons.Right)
                    {
                        ShowContextForm();
                    }
                    //  Api.Execute($"!Log  \"Meter In Focus\"");
                }
                else
                {
                    if (UnFocusDismiss == 1 && IsActive && ContextFocusForm)
                    {
                        UnFocusDismissHandler();
                        //Api.Execute($"!Log  \"Meter In UnFocus\"");

                    }
                }
            }

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
                        ValidateTextBuffer(InputType);

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
        //                                                      KeyBoard Functions                                                         //
        //=================================================================================================================================//

        public void ESCHandler()
        {

            if (!IsActive) return;
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
            // Api.Execute(OnESCAction);
        }
        public void DismissHandler()
        {

            if (!IsActive) return;

            IsActive = false;
            hasResetOnce = false;
            Api.Execute(UnValidAction);
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

        }

        public void UnFocusDismissHandler()
        {
            if (!IsActive) return;
            IsActive = false;
            hasResetOnce = false;
            Api.Execute(DismissAction);
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
            // Api.Execute(DismissAction);
        }


        private void HandleCtrlEnter()
        {
            ValidateTextBuffer(InputType);
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
                UpdateText();


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
                UpdateText();

            }
        }
        internal void Undo()
        {
            if (UndoStack.Count > 0)
            {
                RedoStack.Push(TextBuffer);
                TextBuffer = UndoStack.Pop();
                CursorPosition = TextBuffer.Length;
                UpdateText();

            }
        }
        internal void Redo()
        {
            if (RedoStack.Count > 0)
            {
                UndoStack.Push(TextBuffer);
                TextBuffer = RedoStack.Pop();
                CursorPosition = TextBuffer.Length;
                UpdateText();

            }
        }
        internal void SaveStateForUndo()
        {
            UndoStack.Push(TextBuffer);
            RedoStack.Clear();
            UpdateText();
        }
        //=================================================================================================================================//
        //                                                     CommonFunctions                                                             //
        //=================================================================================================================================//
        private Color ParseColor(string colorString, Color defaultColor)
        {
            try
            {
                string[] parts = colorString.Split(',');
                if (parts.Length == 3)
                {
                    int r = int.Parse(parts[0].Trim());
                    int g = int.Parse(parts[1].Trim());
                    int b = int.Parse(parts[2].Trim());
                    return Color.FromArgb(r, g, b);
                }
            }
            catch
            {
                Api.Log(Rainmeter.API.LogType.Warning, $"Invalid color format: '{colorString}'. Using default color.");

            }
            return defaultColor;
        }
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
                        if (useRegex == 1)
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
                /* else
                 {
                     Api.Log(API.LogType.Warning, $"Invalid rule format: {rule}");
                 }*/
            }

            text = text.Replace("#CRLF#", "\n");
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


        private void UpdateMeasure()
        {

            if (!IsActive || string.IsNullOrEmpty(MeterName))
                return;

            Api.Execute($"!SetOption  \"{MeterName}\" Text \"{TextBuffer}\" ");
            Api.Execute($"!SetOption  \"{myName}\" DefaultValue \"{TextBuffer}\" ");
            Api.Execute($"!UpdateMeter   \"{MeterName}\" ");
            Api.Execute($"!UpdateMeasure  \"{myName}\" ");
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
            if (Disabled == 1) return;

            if (IsActive)
            {
                Api.Log(API.LogType.Debug, "Plugin is already running. Start operation skipped.");
                return;
            }
            if (UnFocusDismiss == 1)
            {
                ContextFocusForm = true;
            }
            IsActive = true;
            hasResetOnce = false;
            CursorPosition = TextBuffer.Length;
            updateTimer.Start();

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

            if (ContextFormOpen)
                return;


            ContextFocusForm = false;
            ContextFormOpen = true;
            ContextForm contextForm = new ContextForm(
    this,
    BackgroundColor,  // Custom background color
    ButtonColor,      // Custom button color
    TextColor         // Custom text color
);
            contextForm.ShowDialog();


        }
        internal void Stop()
        {
            if (!IsActive)
                return;

            IsActive = false;
            hasResetOnce = false;
            CursorPosition = 0;
            UndoStack.Clear();
            RedoStack.Clear();
            updateTimer.Stop();
            updateTimer.Stop();
        }

        //=================================================================================================================================//
        //                                                      ValidInputs                                                                //
        //=================================================================================================================================//
        private bool IsValidInput(char keyChar)
        {
            if (ForceValidInput == 1)
            {
                switch (InputType)
                {
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
            else
            {
                return true;
            }

        }


        public class ErrorForm : Form
        {
            private Label errorLabel;
            private Button closeButton;

            public ErrorForm(string errorMessage, Color backgroundColor, Color buttonColor, Color textColor)
            {
                // Set up the form
                this.Text = "Error";
                this.Size = new Size(400, 250);
                this.StartPosition = FormStartPosition.CenterScreen;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.ControlBox = false;
                this.BackColor = backgroundColor;
                this.ForeColor = textColor;
                this.ShowInTaskbar = false;

                // Configure error message label
                errorLabel = new Label
                {
                    Text = errorMessage,
                    Font = new Font("Segoe UI", 12, FontStyle.Regular),
                    ForeColor = textColor,
                    Location = new Point(20, 30),
                    Size = new Size(360, 120),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = false
                };

                // Configure close button
                closeButton = new Button
                {
                    Text = "Close",
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    BackColor = buttonColor,
                    ForeColor = textColor,
                    FlatStyle = FlatStyle.Flat,
                    Location = new Point(150, 160),
                    Size = new Size(100, 40),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                // Remove button border and add hover effect
                closeButton.FlatAppearance.BorderSize = 0;
                closeButton.Click += (sender, e) => { this.Close(); };
                closeButton.MouseEnter += (sender, e) => closeButton.BackColor = ControlPaint.Dark(buttonColor, 0.2f);
                closeButton.MouseLeave += (sender, e) => closeButton.BackColor = buttonColor;

                // Add controls to the form
                this.Controls.Add(errorLabel);
                this.Controls.Add(closeButton);
            }


            protected override void WndProc(ref Message m)
            {

                if (m.Msg == 0xA3)
                {

                    return;
                }


                base.WndProc(ref m);
            }
        }

        private bool IsTextBufferString()
        {
            if (string.IsNullOrWhiteSpace(TextBuffer))
                return true;

            return true;
        }

        private bool IsTextBufferInteger()
        {
            if (string.IsNullOrWhiteSpace(TextBuffer))
                return true;

            return int.TryParse(TextBuffer, out _);
        }

        private bool IsTextBufferFloat()
        {
            if (string.IsNullOrWhiteSpace(TextBuffer))
                return true;

            return float.TryParse(TextBuffer, out _);
        }

        private bool IsTextBufferHexadecimal()
        {
            if (string.IsNullOrWhiteSpace(TextBuffer))
                return true;

            return Regex.IsMatch(TextBuffer, @"\A\b(0[xX])?[0-9a-fA-F]+\b\Z");
        }

        private bool IsTextBufferEmail()
        {
            if (string.IsNullOrWhiteSpace(TextBuffer))
                return true;

            return Regex.IsMatch(TextBuffer, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        private bool IsTextBufferAlphanumeric()
        {
            if (string.IsNullOrWhiteSpace(TextBuffer))
                return true;

            return Regex.IsMatch(TextBuffer, @"^[a-zA-Z0-9]+$");
        }

        private bool IsTextBufferLetters()
        {
            if (string.IsNullOrWhiteSpace(TextBuffer))
                return true;

            return Regex.IsMatch(TextBuffer, @"^[a-zA-Z]+$");
        }

        private bool IsTextBufferCustom()
        {
            if (string.IsNullOrWhiteSpace(TextBuffer))
                return true;

            if (string.IsNullOrEmpty(AllowedCharacters))
            {
                Api.Log(API.LogType.Warning, "AllowedCharacters is empty. Custom validation cannot proceed.");
                return false;
            }

            string pattern = $"^[{Regex.Escape(AllowedCharacters)}]+$";
            return Regex.IsMatch(TextBuffer, pattern);
        }


        private void ValidateTextBuffer(string inputType)
        {
            bool isValid = false;

            switch (inputType)
            {
                case "String":
                    isValid = IsTextBufferString();
                    break;
                case "Integer":
                    isValid = IsTextBufferInteger();
                    break;
                case "Float":
                    isValid = IsTextBufferFloat();
                    break;
                case "Hexadecimal":
                    isValid = IsTextBufferHexadecimal();
                    break;
                case "Email":
                    isValid = IsTextBufferEmail();
                    break;
                case "Alphanumeric":
                    isValid = IsTextBufferAlphanumeric();
                    break;
                case "Letters":
                    isValid = IsTextBufferLetters();
                    break;
                case "Custom":
                    isValid = IsTextBufferCustom();
                    break;
                default:
                    Api.Log(API.LogType.Warning, $"Unknown InputType: {inputType}. Validation skipped.");
                    return;
            }

            if (!isValid)
            {
                DismissHandler();
                ShowError($"Input is not a valid.Only allow {inputType}.");
            }
            else
            {
                Api.Log(API.LogType.Debug, $"TextBuffer is a valid {inputType}.");

                ConvertTextBufferToSingleLine();
                UpdateMeasure();
                Api.Execute(OnEnterAction);
                Stop();

            }
        }
        private void ShowError(string errorMessage)
        {

            if (ShowErrorForm == 1)
            {

                ErrorForm errorForm = new ErrorForm(errorMessage, BackgroundColor, ButtonColor, TextColor);
                errorForm.ShowDialog();
            }
            else
            {

                Api.Log(API.LogType.Error, errorMessage);
            }
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
        //                                                      Context                                                                   //
        //=================================================================================================================================//
        public class ContextForm : Form
        {
            private readonly Measure _measure;

            public ContextForm(Measure measure, Color backgroundColor, Color buttonColor, Color textColor)
            {
                _measure = measure;

                // Apply custom colors
                BackColor = backgroundColor;

                Text = "BlurInput Menu";
                Size = new Size(200, 350);
                FormBorderStyle = FormBorderStyle.None;
                MaximizeBox = false;
                MinimizeBox = false;
                ControlBox = false;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.Manual;

                var cursorPosition = Cursor.Position;
                Location = new Point(cursorPosition.X - (Size.Width / 2), cursorPosition.Y);

                // Create buttons with custom colors
                var undoButton = CreateStyledButton("Undo", new Point(30, 30), "\uE10E", buttonColor, textColor);
                var redoButton = CreateStyledButton("Redo", new Point(30, 80), "\uE10D", buttonColor, textColor);
                var copyButton = CreateStyledButton("Copy", new Point(30, 130), "\uE16F", buttonColor, textColor);
                var pasteButton = CreateStyledButton("Paste", new Point(30, 180), "\uE16D", buttonColor, textColor);
                var clearButton = CreateStyledButton("Clear Text", new Point(30, 230), "\uE107", buttonColor, textColor);
                var cancelButton = CreateStyledButton("Cancel", new Point(30, 280), "\uE10A", buttonColor, textColor);

                // Add button actions
                undoButton.Click += (s, e) => { _measure.Undo(); Close(); };
                redoButton.Click += (s, e) => { _measure.Redo(); Close(); };
                copyButton.Click += (s, e) => { _measure.CopyToClipboard(); Close(); };
                pasteButton.Click += (s, e) => { _measure.PasteFromClipboard(); Close(); };
                clearButton.Click += (s, e) => { _measure.ClearText(); Close(); };
                cancelButton.Click += (s, e) => { Close(); };

                // Add buttons to the form
                Controls.Add(undoButton);
                Controls.Add(redoButton);
                Controls.Add(copyButton);
                Controls.Add(pasteButton);
                Controls.Add(clearButton);
                Controls.Add(cancelButton);
            }

            private Button CreateStyledButton(string text, Point location, string iconUnicode, Color buttonColor, Color textColor)
            {
                var button = new Button
                {
                    Text = $"{iconUnicode} {text}",
                    Font = new Font("Segoe UI Symbol", 10, FontStyle.Bold),
                    ForeColor = textColor,
                    BackColor = buttonColor,
                    FlatStyle = FlatStyle.Flat,
                    FlatAppearance = { BorderSize = 0 },
                    Location = location,
                    Size = new Size(150, 40),
                    Cursor = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleCenter
                };

                return button;
            }


            protected override void OnFormClosing(FormClosingEventArgs e)
            {
                base.OnFormClosing(e);
                _measure.ContextFocusForm = true;
                _measure.ContextFormOpen = false;
            }

            protected override void OnDeactivate(EventArgs e)
            {
                base.OnDeactivate(e);


                var mousePos = Cursor.Position;
                var formRect = new Rectangle(Location, Size);

                if (!formRect.Contains(mousePos))
                {
                    _measure.ContextFocusForm = true;
                    _measure.ContextFormOpen = false;
                    Close();
                }
            }

            public bool ContextFocus { get; private set; }
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