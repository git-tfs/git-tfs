using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Sep.Git.Tfs.Vs2010
{
    public class ParentForm : Form
    {
        public ParentForm()
        {
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false;
            base.OnLoad(e);
            WindowState = FormWindowState.Normal;
            CenterToParent();
        }
    }
}
