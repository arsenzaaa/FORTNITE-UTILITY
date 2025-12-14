using System;
using System.Drawing;
using System.Windows.Forms;

namespace FortniteUtility;

internal enum DialogStyle
{
    Ok,
    YesNo
}

internal static class Dialogs
{
    public static DialogResult Info(IWin32Window? owner, string title, string message, string buttonText = "OK")
    {
        using var dlg = new DarkDialog(title, message, DialogStyle.Ok, buttonText, null, Theme.Accent);
        return dlg.ShowDialog(owner);
    }

    public static DialogResult Error(IWin32Window? owner, string title, string message, string buttonText = "OK")
    {
        using var dlg = new DarkDialog(title, message, DialogStyle.Ok, buttonText, null, Theme.Danger);
        return dlg.ShowDialog(owner);
    }

    public static DialogResult Confirm(IWin32Window? owner, string title, string message, string yesText = "Yes", string noText = "No")
    {
        using var dlg = new DarkDialog(title, message, DialogStyle.YesNo, yesText, noText, Theme.Accent);
        return dlg.ShowDialog(owner);
    }
}

internal sealed class DarkDialog : Form
{
    private readonly DialogStyle _style;
    private readonly string _primaryText;
    private readonly string? _secondaryText;
    private readonly Color _accent;
    private readonly Bitmap _iconBitmap;

    public DarkDialog(string title, string message, DialogStyle style, string primaryText, string? secondaryText, Color accent)
    {
        _style = style;
        _primaryText = primaryText;
        _secondaryText = secondaryText;
        _accent = accent;
        _iconBitmap = style == DialogStyle.YesNo
            ? SystemIcons.Question.ToBitmap()
            : accent == Theme.Danger ? SystemIcons.Warning.ToBitmap() : SystemIcons.Information.ToBitmap();

        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = Theme.BodyFont;
        BackColor = Theme.Surface;
        ForeColor = Theme.TextPrimary;

        BuildLayout(message);
    }

    private void BuildLayout(string message)
    {
        const int padding = 22;
        const int buttonHeight = 34;
        var iconBox = new PictureBox
        {
            Image = _iconBitmap,
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(32, 32),
            Location = new Point(padding, padding)
        };

        var textLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(520, 0),
            Location = new Point(iconBox.Right + 12, padding),
            Text = message,
            Font = Theme.Small,
            ForeColor = Theme.TextPrimary
        };

        Controls.Add(iconBox);
        Controls.Add(textLabel);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Padding = new Padding(padding, 12, padding, padding),
            BackColor = Theme.Surface,
            AutoSize = true,
            WrapContents = false
        };

        if (_style == DialogStyle.Ok)
        {
            var ok = CreateButton(_primaryText, _accent);
            ok.DialogResult = DialogResult.OK;
            AcceptButton = ok;
            buttons.Controls.Add(ok);
        }
        else
        {
            var yes = CreateButton(_primaryText, _accent);
            yes.DialogResult = DialogResult.Yes;
            var no = CreateButton(_secondaryText ?? "No", Theme.SurfaceAlt, Theme.TextPrimary);
            no.DialogResult = DialogResult.No;
            AcceptButton = yes;
            CancelButton = no;
            buttons.Controls.Add(yes);
            buttons.Controls.Add(no);
        }

        buttons.Height = buttonHeight + padding;
        Controls.Add(buttons);

        var textHeight = textLabel.Height;
        int baseHeight = padding + Math.Max(iconBox.Height, textHeight) + buttons.Height + padding;
        int width = Math.Max(360, textLabel.Right + padding);
        ClientSize = new Size(width, baseHeight);
    }

    private Button CreateButton(string text, Color accentColor, Color? foreColor = null)
    {
        bool filled = accentColor == Theme.Accent || accentColor == Theme.Danger;
        var baseBack = filled ? Theme.Accent : Theme.Background;
        var borderColor = filled ? Theme.Background : Theme.Accent;
        var textColor = foreColor ?? (filled ? Theme.Background : Theme.TextPrimary);

        var btn = new Button
        {
            Text = text,
            Height = 34,
            Width = 120,
            FlatStyle = FlatStyle.Flat,
            Font = Theme.BodyBold,
            BackColor = baseBack,
            ForeColor = textColor,
            Margin = new Padding(8, 0, 0, 0)
        };

        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = borderColor;
        btn.FlatAppearance.MouseOverBackColor = baseBack;
        btn.FlatAppearance.MouseDownBackColor = baseBack;
        return btn;
    }
}
