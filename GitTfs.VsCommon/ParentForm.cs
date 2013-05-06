﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Sep.Git.Tfs.VsCommon
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
