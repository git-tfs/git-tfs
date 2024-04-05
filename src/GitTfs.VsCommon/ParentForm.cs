#if NETFRAMEWORK
using System.Drawing;
using System.Windows.Forms;

namespace GitTfs.VsCommon
{
    public class ParentForm : Form
    {
        public ParentForm()
        {
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = true;
            ShowIcon = false;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            WindowState = FormWindowState.Normal;
            CenterToParent();
            Text = "Checkin tool (git-tfs)";
            Size = new Size(0, 0);
        }
    }
}
#endif
