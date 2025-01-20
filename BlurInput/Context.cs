using PluginBlurInput;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using System.Windows.Forms;
using System;

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

   public Button CreateStyledButton(string text, Point location, string iconUnicode, Color buttonColor, Color textColor)
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
    
