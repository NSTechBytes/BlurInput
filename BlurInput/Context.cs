using System;
using System.Drawing;
using System.Windows.Forms;
using PluginBlurInput;

namespace PluginBlurInput
{
    public class ContextForm : Form
    {
        #region Constants
        private const int FORM_WIDTH = 200;
        private const int FORM_HEIGHT = 350;
        private const int BUTTON_WIDTH = 150;
        private const int BUTTON_HEIGHT = 40;
        private const int BUTTON_MARGIN = 50;
        private const int FIRST_BUTTON_Y = 30;
        #endregion

        #region Fields
        private readonly Measure _measure;
        private readonly Color _backgroundColor;
        private readonly Color _buttonColor;
        private readonly Color _textColor;
        #endregion

        #region Constructor
        public ContextForm(Measure measure, Color backgroundColor, Color buttonColor, Color textColor)
        {
            _measure = measure ?? throw new ArgumentNullException(nameof(measure));
            _backgroundColor = backgroundColor;
            _buttonColor = buttonColor;
            _textColor = textColor;

            InitializeForm();
            SetupKeyHandling();
            CreateButtons();
            CenterFormAtCursor();
        }
        #endregion

        #region Form Initialization
        private void InitializeForm()
        {
            Text = "BlurInput Menu";
            Size = new Size(FORM_WIDTH, FORM_HEIGHT);
            BackColor = _backgroundColor;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true; // Ensure form stays on top
        }

        private void SetupKeyHandling()
        {
            KeyPreview = true;
            KeyDown += ContextForm_KeyDown;
        }

        private void CreateButtons()
        {
            var buttons = new[]
            {
                CreateContextButton("Undo", "\uE10E", (s, e) => ExecuteAction(_measure.Undo)),
                CreateContextButton("Redo", "\uE10D", (s, e) => ExecuteAction(_measure.Redo)),
                CreateContextButton("Copy", "\uE16F", (s, e) => ExecuteAction(_measure.CopyToClipboard)),
                CreateContextButton("Paste", "\uE16D", (s, e) => ExecuteAction(_measure.PasteFromClipboard)),
                CreateContextButton("Clear Text", "\uE107", (s, e) => ExecuteAction(_measure.ClearText)),
                CreateContextButton("Cancel", "\uE10A", (s, e) => Close())
            };

            int yPosition = FIRST_BUTTON_Y;
            foreach (var button in buttons)
            {
                button.Location = new Point((FORM_WIDTH - BUTTON_WIDTH) / 2, yPosition);
                Controls.Add(button);
                yPosition += BUTTON_MARGIN;
            }
        }

        private void CenterFormAtCursor()
        {
            var cursorPosition = Cursor.Position;
            Location = new Point(
                cursorPosition.X - (Size.Width / 2),
                cursorPosition.Y
            );

            // Ensure form stays within screen bounds
            EnsureFormWithinScreenBounds();
        }

        private void EnsureFormWithinScreenBounds()
        {
            var screen = Screen.FromPoint(Location);
            var workingArea = screen.WorkingArea;

            int x = Math.Max(workingArea.Left, Math.Min(Location.X, workingArea.Right - Width));
            int y = Math.Max(workingArea.Top, Math.Min(Location.Y, workingArea.Bottom - Height));

            Location = new Point(x, y);
        }
        #endregion

        #region Button Creation
        private Button CreateContextButton(string text, string iconUnicode, EventHandler clickHandler)
        {
            var button = new Button
            {
                Text = $"{iconUnicode} {text}",
                Font = new Font("Segoe UI Symbol", 10, FontStyle.Bold),
                ForeColor = _textColor,
                BackColor = _buttonColor,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(BUTTON_WIDTH, BUTTON_HEIGHT),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false
            };

            button.FlatAppearance.BorderSize = 0;
            button.Click += clickHandler;

            // Add hover effects
            button.MouseEnter += (s, e) => button.BackColor = ControlPaint.Light(_buttonColor, 0.2f);
            button.MouseLeave += (s, e) => button.BackColor = _buttonColor;

            return button;
        }
        #endregion

        #region Event Handlers
        private void ContextForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                CloseForm();
                e.Handled = true;
            }
        }

        private void ExecuteAction(Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                // Log error but don't crash the application
                System.Diagnostics.Debug.WriteLine($"Error executing context menu action: {ex.Message}");
            }
            finally
            {
                CloseForm();
            }
        }

        // Public method to close the form programmatically
        public void CloseForm()
        {
            if (!IsDisposed && !Disposing)
            {
                _measure.ContextFocusForm = true;
                _measure.ContextFormOpen = false;

                // Use BeginInvoke to close on UI thread
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => Close()));
                }
                else
                {
                    Close();
                }
            }
        }

        #endregion
    }
}