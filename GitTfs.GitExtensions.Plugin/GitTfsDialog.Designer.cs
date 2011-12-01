namespace GitTfs.GitExtensions.Plugin
{
    partial class GitTfsDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GitTfsDialog));
            this.PushButton = new System.Windows.Forms.Button();
            this.Container = new System.Windows.Forms.SplitContainer();
            this.TopContainer = new System.Windows.Forms.SplitContainer();
            this.TfsRemoteComboBox = new System.Windows.Forms.ComboBox();
            this.TfsRemoteLabel = new System.Windows.Forms.Label();
            this.PullGroupBox = new System.Windows.Forms.GroupBox();
            this.MergeOptionsGroupBox = new System.Windows.Forms.GroupBox();
            this.FetchRadioButton = new System.Windows.Forms.RadioButton();
            this.RebaseRadioButton = new System.Windows.Forms.RadioButton();
            this.PullRadioButton = new System.Windows.Forms.RadioButton();
            this.PullButton = new System.Windows.Forms.Button();
            this.PushGroupBox = new System.Windows.Forms.GroupBox();
            this.PushOptionsGroupBox = new System.Windows.Forms.GroupBox();
            this.RCheckinRadioButton = new System.Windows.Forms.RadioButton();
            this.ShelveRadioButton = new System.Windows.Forms.RadioButton();
            this.CheckinRadioButton = new System.Windows.Forms.RadioButton();
            this.Container.Panel1.SuspendLayout();
            this.Container.Panel2.SuspendLayout();
            this.Container.SuspendLayout();
            this.TopContainer.Panel1.SuspendLayout();
            this.TopContainer.Panel2.SuspendLayout();
            this.TopContainer.SuspendLayout();
            this.PullGroupBox.SuspendLayout();
            this.MergeOptionsGroupBox.SuspendLayout();
            this.PushGroupBox.SuspendLayout();
            this.PushOptionsGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // PushButton
            // 
            this.PushButton.Enabled = false;
            this.PushButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PushButton.Image = ((System.Drawing.Image)(resources.GetObject("PushButton.Image")));
            this.PushButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.PushButton.Location = new System.Drawing.Point(6, 121);
            this.PushButton.Name = "PushButton";
            this.PushButton.Size = new System.Drawing.Size(75, 23);
            this.PushButton.TabIndex = 1;
            this.PushButton.Text = "Push";
            this.PushButton.UseVisualStyleBackColor = true;
            this.PushButton.Click += new System.EventHandler(this.PushButtonClick);
            // 
            // Container
            // 
            this.Container.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Container.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.Container.Location = new System.Drawing.Point(0, 0);
            this.Container.Name = "Container";
            this.Container.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // Container.Panel1
            // 
            this.Container.Panel1.Controls.Add(this.TopContainer);
            // 
            // Container.Panel2
            // 
            this.Container.Panel2.Controls.Add(this.PushGroupBox);
            this.Container.Size = new System.Drawing.Size(269, 346);
            this.Container.SplitterDistance = 186;
            this.Container.TabIndex = 2;
            // 
            // TopContainer
            // 
            this.TopContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TopContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.TopContainer.Location = new System.Drawing.Point(0, 0);
            this.TopContainer.Name = "TopContainer";
            this.TopContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // TopContainer.Panel1
            // 
            this.TopContainer.Panel1.Controls.Add(this.TfsRemoteComboBox);
            this.TopContainer.Panel1.Controls.Add(this.TfsRemoteLabel);
            // 
            // TopContainer.Panel2
            // 
            this.TopContainer.Panel2.Controls.Add(this.PullGroupBox);
            this.TopContainer.Size = new System.Drawing.Size(269, 186);
            this.TopContainer.SplitterDistance = 34;
            this.TopContainer.TabIndex = 5;
            // 
            // TfsRemoteComboBox
            // 
            this.TfsRemoteComboBox.FormattingEnabled = true;
            this.TfsRemoteComboBox.Location = new System.Drawing.Point(79, 6);
            this.TfsRemoteComboBox.Name = "TfsRemoteComboBox";
            this.TfsRemoteComboBox.Size = new System.Drawing.Size(167, 21);
            this.TfsRemoteComboBox.Sorted = true;
            this.TfsRemoteComboBox.TabIndex = 1;
            this.TfsRemoteComboBox.SelectedIndexChanged += new System.EventHandler(this.TfsRemoteComboBoxSelectedIndexChanged);
            // 
            // TfsRemoteLabel
            // 
            this.TfsRemoteLabel.AutoSize = true;
            this.TfsRemoteLabel.Location = new System.Drawing.Point(3, 9);
            this.TfsRemoteLabel.Name = "TfsRemoteLabel";
            this.TfsRemoteLabel.Size = new System.Drawing.Size(70, 13);
            this.TfsRemoteLabel.TabIndex = 0;
            this.TfsRemoteLabel.Text = "TFS Remote:";
            // 
            // PullGroupBox
            // 
            this.PullGroupBox.Controls.Add(this.MergeOptionsGroupBox);
            this.PullGroupBox.Controls.Add(this.PullButton);
            this.PullGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PullGroupBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PullGroupBox.Location = new System.Drawing.Point(0, 0);
            this.PullGroupBox.Name = "PullGroupBox";
            this.PullGroupBox.Size = new System.Drawing.Size(269, 148);
            this.PullGroupBox.TabIndex = 4;
            this.PullGroupBox.TabStop = false;
            this.PullGroupBox.Text = "Pull";
            // 
            // MergeOptionsGroupBox
            // 
            this.MergeOptionsGroupBox.Controls.Add(this.FetchRadioButton);
            this.MergeOptionsGroupBox.Controls.Add(this.RebaseRadioButton);
            this.MergeOptionsGroupBox.Controls.Add(this.PullRadioButton);
            this.MergeOptionsGroupBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MergeOptionsGroupBox.Location = new System.Drawing.Point(6, 19);
            this.MergeOptionsGroupBox.Name = "MergeOptionsGroupBox";
            this.MergeOptionsGroupBox.Size = new System.Drawing.Size(255, 93);
            this.MergeOptionsGroupBox.TabIndex = 2;
            this.MergeOptionsGroupBox.TabStop = false;
            this.MergeOptionsGroupBox.Text = "Merge Options";
            // 
            // FetchRadioButton
            // 
            this.FetchRadioButton.AutoSize = true;
            this.FetchRadioButton.Location = new System.Drawing.Point(7, 66);
            this.FetchRadioButton.Name = "FetchRadioButton";
            this.FetchRadioButton.Size = new System.Drawing.Size(235, 17);
            this.FetchRadioButton.TabIndex = 2;
            this.FetchRadioButton.Text = "Do not merge, only fetch remote TFS branch";
            this.FetchRadioButton.UseVisualStyleBackColor = true;
            this.FetchRadioButton.CheckedChanged += new System.EventHandler(this.MergeOptionCheckedChanged);
            // 
            // RebaseRadioButton
            // 
            this.RebaseRadioButton.AutoSize = true;
            this.RebaseRadioButton.Location = new System.Drawing.Point(7, 44);
            this.RebaseRadioButton.Name = "RebaseRadioButton";
            this.RebaseRadioButton.Size = new System.Drawing.Size(240, 17);
            this.RebaseRadioButton.TabIndex = 1;
            this.RebaseRadioButton.Text = "Rebase remote TFS branch to current branch";
            this.RebaseRadioButton.UseVisualStyleBackColor = true;
            this.RebaseRadioButton.CheckedChanged += new System.EventHandler(this.MergeOptionCheckedChanged);
            // 
            // PullRadioButton
            // 
            this.PullRadioButton.AutoSize = true;
            this.PullRadioButton.Location = new System.Drawing.Point(7, 20);
            this.PullRadioButton.Name = "PullRadioButton";
            this.PullRadioButton.Size = new System.Drawing.Size(233, 17);
            this.PullRadioButton.TabIndex = 0;
            this.PullRadioButton.Text = "Merge remote TFS branch to current branch";
            this.PullRadioButton.UseVisualStyleBackColor = true;
            this.PullRadioButton.CheckedChanged += new System.EventHandler(this.MergeOptionCheckedChanged);
            // 
            // PullButton
            // 
            this.PullButton.Enabled = false;
            this.PullButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PullButton.Image = ((System.Drawing.Image)(resources.GetObject("PullButton.Image")));
            this.PullButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.PullButton.Location = new System.Drawing.Point(6, 118);
            this.PullButton.Name = "PullButton";
            this.PullButton.Size = new System.Drawing.Size(75, 23);
            this.PullButton.TabIndex = 0;
            this.PullButton.Text = "Pull";
            this.PullButton.UseVisualStyleBackColor = true;
            this.PullButton.Click += new System.EventHandler(this.PullButtonClick);
            // 
            // PushGroupBox
            // 
            this.PushGroupBox.Controls.Add(this.PushButton);
            this.PushGroupBox.Controls.Add(this.PushOptionsGroupBox);
            this.PushGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PushGroupBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PushGroupBox.Location = new System.Drawing.Point(0, 0);
            this.PushGroupBox.Name = "PushGroupBox";
            this.PushGroupBox.Size = new System.Drawing.Size(269, 156);
            this.PushGroupBox.TabIndex = 2;
            this.PushGroupBox.TabStop = false;
            this.PushGroupBox.Text = "Push";
            // 
            // PushOptionsGroupBox
            // 
            this.PushOptionsGroupBox.Controls.Add(this.RCheckinRadioButton);
            this.PushOptionsGroupBox.Controls.Add(this.ShelveRadioButton);
            this.PushOptionsGroupBox.Controls.Add(this.CheckinRadioButton);
            this.PushOptionsGroupBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PushOptionsGroupBox.Location = new System.Drawing.Point(6, 19);
            this.PushOptionsGroupBox.Name = "PushOptionsGroupBox";
            this.PushOptionsGroupBox.Size = new System.Drawing.Size(255, 96);
            this.PushOptionsGroupBox.TabIndex = 0;
            this.PushOptionsGroupBox.TabStop = false;
            this.PushOptionsGroupBox.Text = "Options";
            // 
            // RCheckinRadioButton
            // 
            this.RCheckinRadioButton.AutoSize = true;
            this.RCheckinRadioButton.Location = new System.Drawing.Point(7, 18);
            this.RCheckinRadioButton.Name = "RCheckinRadioButton";
            this.RCheckinRadioButton.Size = new System.Drawing.Size(201, 17);
            this.RCheckinRadioButton.TabIndex = 3;
            this.RCheckinRadioButton.TabStop = true;
            this.RCheckinRadioButton.Text = "Recursively Checkin changes to TFS";
            this.RCheckinRadioButton.UseVisualStyleBackColor = true;
            // 
            // ShelveRadioButton
            // 
            this.ShelveRadioButton.AutoSize = true;
            this.ShelveRadioButton.Location = new System.Drawing.Point(6, 64);
            this.ShelveRadioButton.Name = "ShelveRadioButton";
            this.ShelveRadioButton.Size = new System.Drawing.Size(137, 17);
            this.ShelveRadioButton.TabIndex = 2;
            this.ShelveRadioButton.TabStop = true;
            this.ShelveRadioButton.Text = "Shelve changes to TFS";
            this.ShelveRadioButton.UseVisualStyleBackColor = true;
            this.ShelveRadioButton.CheckedChanged += new System.EventHandler(this.PushOptionCheckedChanged);
            // 
            // CheckinRadioButton
            // 
            this.CheckinRadioButton.AutoSize = true;
            this.CheckinRadioButton.Location = new System.Drawing.Point(7, 41);
            this.CheckinRadioButton.Name = "CheckinRadioButton";
            this.CheckinRadioButton.Size = new System.Drawing.Size(143, 17);
            this.CheckinRadioButton.TabIndex = 0;
            this.CheckinRadioButton.TabStop = true;
            this.CheckinRadioButton.Text = "Checkin changes to TFS";
            this.CheckinRadioButton.UseVisualStyleBackColor = true;
            this.CheckinRadioButton.CheckedChanged += new System.EventHandler(this.PushOptionCheckedChanged);
            // 
            // GitTfsDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(269, 346);
            this.Controls.Add(this.Container);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GitTfsDialog";
            this.Text = "git-tfs";
            this.Container.Panel1.ResumeLayout(false);
            this.Container.Panel2.ResumeLayout(false);
            this.Container.ResumeLayout(false);
            this.TopContainer.Panel1.ResumeLayout(false);
            this.TopContainer.Panel1.PerformLayout();
            this.TopContainer.Panel2.ResumeLayout(false);
            this.TopContainer.ResumeLayout(false);
            this.PullGroupBox.ResumeLayout(false);
            this.MergeOptionsGroupBox.ResumeLayout(false);
            this.MergeOptionsGroupBox.PerformLayout();
            this.PushGroupBox.ResumeLayout(false);
            this.PushOptionsGroupBox.ResumeLayout(false);
            this.PushOptionsGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button PushButton;
        private System.Windows.Forms.SplitContainer Container;
        private System.Windows.Forms.GroupBox PushGroupBox;
        private System.Windows.Forms.GroupBox PushOptionsGroupBox;
        private System.Windows.Forms.RadioButton ShelveRadioButton;
        private System.Windows.Forms.RadioButton CheckinRadioButton;
        private System.Windows.Forms.SplitContainer TopContainer;
        private System.Windows.Forms.ComboBox TfsRemoteComboBox;
        private System.Windows.Forms.Label TfsRemoteLabel;
        private System.Windows.Forms.GroupBox PullGroupBox;
        private System.Windows.Forms.GroupBox MergeOptionsGroupBox;
        private System.Windows.Forms.RadioButton FetchRadioButton;
        private System.Windows.Forms.RadioButton RebaseRadioButton;
        private System.Windows.Forms.RadioButton PullRadioButton;
        private System.Windows.Forms.Button PullButton;
        private System.Windows.Forms.RadioButton RCheckinRadioButton;
    }
}