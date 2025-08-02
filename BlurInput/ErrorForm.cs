using System;
using System.Drawing;
using System.Windows.Forms;

namespace PluginBlurInput
{
    public class ErrorForm : Form
    {
        #region Constants
        private const int FORM_WIDTH = 400;
        private const int FORM_HEIGHT = 250;
        private const int LABEL_MARGIN = 20;
        private const int BUTTON_WIDTH = 100;
        private const int BUTTON_HEIGHT = 40;
        private const int BUTTON_MARGIN_BOTTOM = 30;
        #endregion

        #region Fields
        private readonly string _errorMessage;
        private readonly Color _backgroundColor;
        private readonly Color _buttonColor;
        private readonly Color _textColor;

        private Label _errorLabel;
        private Button _closeButton;
        #endregion

        #region Constructor
        public ErrorForm(string errorMessage, Color backgroundColor, Color buttonColor, Color textColor)
        {
            _errorMessage = errorMessage ?? "An unknown error occurred.";
            _backgroundColor = backgroundColor;
            _buttonColor = buttonColor;
            _textColor = textColor;

            InitializeForm();
            CreateControls();
            SetupEventHandlers();
        }
        #endregion

        #region Form Initialization
        private void InitializeForm()
        {
            Text = "Input Error";
            Size = new Size(FORM_WIDTH, FORM_HEIGHT);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ControlBox = false;
            BackColor = _backgroundColor;
            ForeColor = _textColor;
            ShowInTaskbar = false;
            MaximizeBox = false;
            MinimizeBox = false;
            TopMost = true;
        }

        private void CreateControls()
        {
            CreateErrorLabel();
            CreateCloseButton();
        }

        private void CreateErrorLabel()
        {
            _errorLabel = new Label
            {
                Text = _errorMessage,
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = _textColor,
                BackColor = Color.Transparent,
                Location = new Point(LABEL_MARGIN, LABEL_MARGIN),
                Size = new Size(FORM_WIDTH - (LABEL_MARGIN * 2), FORM_HEIGHT - 100),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                UseMnemonic = false // Prevent & from being treated as mnemonic
            };

            Controls.Add(_errorLabel);
        }

        private void CreateCloseButton()
        {
            int buttonX = (FORM_WIDTH - BUTTON_WIDTH) / 2;
            int buttonY = FORM_HEIGHT - BUTTON_HEIGHT - BUTTON_MARGIN_BOTTOM;

            _closeButton = new Button
            {
                Text = "OK",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = _buttonColor,
                ForeColor = _textColor,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(buttonX, buttonY),
                Size = new Size(BUTTON_WIDTH, BUTTON_HEIGHT),
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false,
                DialogResult = DialogResult.OK
            };

            _closeButton.FlatAppearance.BorderSize = 0;
            Controls.Add(_closeButton);

            // Set as default and cancel button
            AcceptButton = _closeButton;
            CancelButton = _closeButton;
        }

        private void SetupEventHandlers()
        {
            _closeButton.Click += CloseButton_Click;
            _closeButton.MouseEnter += CloseButton_MouseEnter;
            _closeButton.MouseLeave += CloseButton_MouseLeave;

            // Handle keyboard shortcuts
            KeyPreview = true;
            KeyDown += ErrorForm_KeyDown;
        }
        #endregion

        #region Event Handlers
        private void CloseButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void CloseButton_MouseEnter(object sender, EventArgs e)
        {
            _closeButton.BackColor = ControlPaint.Light(_buttonColor, 0.2f);
        }

        private void CloseButton_MouseLeave(object sender, EventArgs e)
        {
            _closeButton.BackColor = _buttonColor;
        }

        private void ErrorForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Close on Escape or Enter
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter)
            {
                DialogResult = DialogResult.OK;
                Close();
                e.Handled = true;
            }
        }

        #endregion

    }
}