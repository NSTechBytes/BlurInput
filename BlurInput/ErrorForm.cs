using System.Drawing;
using System.Windows.Forms;

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


        closeButton.FlatAppearance.BorderSize = 0;
        closeButton.Click += (sender, e) => { this.Close(); };
        closeButton.MouseEnter += (sender, e) => closeButton.BackColor = ControlPaint.Dark(buttonColor, 0.2f);
        closeButton.MouseLeave += (sender, e) => closeButton.BackColor = buttonColor;


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