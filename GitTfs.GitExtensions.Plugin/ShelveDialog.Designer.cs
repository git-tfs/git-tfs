namespace GitTfs.GitExtensions.Plugin
{
    partial class ShelveDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ShelveDialog));
            this.NameTextBox = new System.Windows.Forms.TextBox();
            this.NameLabel = new System.Windows.Forms.Label();
            this.OverwriteCheckBox = new System.Windows.Forms.CheckBox();
            this.ShelveButton = new System.Windows.Forms.Button();
            this.CancelButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // NameTextBox
            // 
            this.NameTextBox.Location = new System.Drawing.Point(106, 6);
            this.NameTextBox.Name = "NameTextBox";
            this.NameTextBox.Size = new System.Drawing.Size(150, 20);
            this.NameTextBox.TabIndex = 0;
            this.NameTextBox.TextChanged += new System.EventHandler(this.NameTextBoxTextChanged);
            // 
            // NameLabel
            // 
            this.NameLabel.AutoSize = true;
            this.NameLabel.Location = new System.Drawing.Point(12, 9);
            this.NameLabel.Name = "NameLabel";
            this.NameLabel.Size = new System.Drawing.Size(88, 13);
            this.NameLabel.TabIndex = 1;
            this.NameLabel.Text = "Shelveset Name:";
            // 
            // OverwriteCheckBox
            // 
            this.OverwriteCheckBox.AutoSize = true;
            this.OverwriteCheckBox.Location = new System.Drawing.Point(15, 32);
            this.OverwriteCheckBox.Name = "OverwriteCheckBox";
            this.OverwriteCheckBox.Size = new System.Drawing.Size(157, 17);
            this.OverwriteCheckBox.TabIndex = 2;
            this.OverwriteCheckBox.Text = "Overwrite existing shelveset";
            this.OverwriteCheckBox.UseVisualStyleBackColor = true;
            // 
            // ShelveButton
            // 
            this.ShelveButton.Image = ((System.Drawing.Image)(resources.GetObject("ShelveButton.Image")));
            this.ShelveButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.ShelveButton.Location = new System.Drawing.Point(15, 65);
            this.ShelveButton.Name = "ShelveButton";
            this.ShelveButton.Size = new System.Drawing.Size(75, 23);
            this.ShelveButton.TabIndex = 3;
            this.ShelveButton.Text = "Shelve";
            this.ShelveButton.UseVisualStyleBackColor = true;
            this.ShelveButton.Click += new System.EventHandler(this.ShelveButtonClick);
            // 
            // CancelButton
            // 
            this.CancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelButton.Location = new System.Drawing.Point(106, 65);
            this.CancelButton.Name = "CancelButton";
            this.CancelButton.Size = new System.Drawing.Size(75, 23);
            this.CancelButton.TabIndex = 4;
            this.CancelButton.Text = "Cancel";
            this.CancelButton.UseVisualStyleBackColor = true;
            // 
            // ShelveDialog
            // 
            this.AcceptButton = this.ShelveButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(269, 104);
            this.Controls.Add(this.CancelButton);
            this.Controls.Add(this.ShelveButton);
            this.Controls.Add(this.OverwriteCheckBox);
            this.Controls.Add(this.NameLabel);
            this.Controls.Add(this.NameTextBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ShelveDialog";
            this.Text = "Git-Tfs Shelve";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox NameTextBox;
        private System.Windows.Forms.Label NameLabel;
        private System.Windows.Forms.CheckBox OverwriteCheckBox;
        private System.Windows.Forms.Button ShelveButton;
        private System.Windows.Forms.Button CancelButton;
    }
}