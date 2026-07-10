using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SharpDX.Windows;

namespace Loader
{
    public class AppForm : RenderForm
    {
        private readonly ContextMenuStrip contextMenu1;
        public Action FixImguiCapture;
        private readonly NotifyIcon notifyIcon;

        public AppForm()
        {
            SuspendLayout();
            contextMenu1 = new ContextMenuStrip();
            var menuItem1 = new ToolStripMenuItem();
            var menuItem2 = new ToolStripMenuItem();
            contextMenu1.Items.AddRange(new ToolStripItem[] {menuItem2, menuItem1});

            menuItem2.Text = "Bring to Front";

            menuItem1.Text = "E&xit";
            notifyIcon = new NotifyIcon();
            notifyIcon.ContextMenuStrip = contextMenu1;
            notifyIcon.Icon = Icon;
            menuItem1.Click += (sender, args) => { Close(); };

            menuItem2.Click += (sender, args) =>
            {
                BringToFront();
                FixImguiCapture?.Invoke();
            };

            Icon = Icon.ExtractAssociatedIcon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "textures\\icon.ico"));
            notifyIcon.Icon = Icon;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(0, 0);

            Text = "ExileApi";
            notifyIcon.Text = "ExileApi";
            notifyIcon.Visible = true;
            Size = new Size(1600, 900); //Screen.PrimaryScreen.Bounds.Size;
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            ShowInTaskbar = true;
            BackColor = Color.Black;

            ResumeLayout(false);
            BringToFront();
        }

        protected override void Dispose(bool disposing)
        {
            if (notifyIcon != null)
                notifyIcon.Icon = null;

            notifyIcon?.Dispose();
            contextMenu1?.Dispose();
            base.Dispose(disposing);
        }
    }
}
