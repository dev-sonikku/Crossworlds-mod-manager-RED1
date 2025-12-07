namespace CrossworldsModManager
{
    partial class SettingsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.txtGameDir = new System.Windows.Forms.TextBox();
            this.btnBrowseGameDir = new System.Windows.Forms.Button();
            this.btnBrowseModsDir = new System.Windows.Forms.Button();
            this.txtModsDir = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.chkSortEnabled = new System.Windows.Forms.CheckBox();
            this.chkAutoClean = new System.Windows.Forms.CheckBox();
            this.chkCheckForGames = new System.Windows.Forms.CheckBox();
            this.chkAutoCloseLog = new System.Windows.Forms.CheckBox();
            this.chkDeveloperMode = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(84, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Game Directory:";
            this.label1.ForeColor = System.Drawing.Color.Gainsboro;
            // 
            // txtGameDir
            // 
            this.txtGameDir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtGameDir.Location = new System.Drawing.Point(102, 12);
            this.txtGameDir.Name = "txtGameDir";
            this.txtGameDir.Size = new System.Drawing.Size(389, 20);
            this.txtGameDir.TabIndex = 1;
            this.txtGameDir.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.txtGameDir.ForeColor = System.Drawing.Color.White;
            // 
            // btnBrowseGameDir
            // 
            this.btnBrowseGameDir.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseGameDir.Location = new System.Drawing.Point(497, 10);
            this.btnBrowseGameDir.Name = "btnBrowseGameDir";
            this.btnBrowseGameDir.Size = new System.Drawing.Size(75, 23);
            this.btnBrowseGameDir.TabIndex = 2;
            this.btnBrowseGameDir.Text = "Browse...";
            this.btnBrowseGameDir.UseVisualStyleBackColor = false;
            this.btnBrowseGameDir.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(63)))), ((int)(((byte)(63)))), ((int)(((byte)(70)))));
            this.btnBrowseGameDir.ForeColor = System.Drawing.Color.White;
            this.btnBrowseGameDir.Click += new System.EventHandler(this.btnBrowseGameDir_Click);
            // 
            // btnBrowseModsDir
            // 
            this.btnBrowseModsDir.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseModsDir.Location = new System.Drawing.Point(497, 39);
            this.btnBrowseModsDir.Name = "btnBrowseModsDir";
            this.btnBrowseModsDir.Size = new System.Drawing.Size(75, 23);
            this.btnBrowseModsDir.TabIndex = 5;
            this.btnBrowseModsDir.Text = "Browse...";
            this.btnBrowseModsDir.UseVisualStyleBackColor = false;
            this.btnBrowseModsDir.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(63)))), ((int)(((byte)(63)))), ((int)(((byte)(70)))));
            this.btnBrowseModsDir.ForeColor = System.Drawing.Color.White;
            this.btnBrowseModsDir.Click += new System.EventHandler(this.btnBrowseModsDir_Click);
            // 
            // txtModsDir
            // 
            this.txtModsDir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtModsDir.Location = new System.Drawing.Point(102, 41);
            this.txtModsDir.Name = "txtModsDir";
            this.txtModsDir.Size = new System.Drawing.Size(389, 20);
            this.txtModsDir.TabIndex = 4;
            this.txtModsDir.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.txtModsDir.ForeColor = System.Drawing.Color.White;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 44);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(82, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Mods Directory:";
            this.label2.ForeColor = System.Drawing.Color.Gainsboro;
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.Location = new System.Drawing.Point(416, 79);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 6;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(497, 79);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 7;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(63)))), ((int)(((byte)(63)))), ((int)(((byte)(70)))));
            this.btnCancel.ForeColor = System.Drawing.Color.White;
            // 
            // chkSortEnabled
            // 
            this.chkSortEnabled.AutoSize = true;
            this.chkSortEnabled.ForeColor = System.Drawing.Color.Gainsboro;
            this.chkSortEnabled.Location = new System.Drawing.Point(102, 67);
            this.chkSortEnabled.Name = "chkSortEnabled";
            this.chkSortEnabled.Size = new System.Drawing.Size(215, 17);
            this.chkSortEnabled.TabIndex = 8;
            this.chkSortEnabled.Text = "Sort enabled mods to the top of the list";
            this.chkSortEnabled.UseVisualStyleBackColor = true;
            // 
            // chkAutoClean
            // 
            this.chkAutoClean.AutoSize = true;
            this.chkAutoClean.ForeColor = System.Drawing.Color.Gainsboro;
            this.chkAutoClean.Location = new System.Drawing.Point(102, 90);
            this.chkAutoClean.Name = "chkAutoClean";
            this.chkAutoClean.Size = new System.Drawing.Size(212, 17);
            this.chkAutoClean.TabIndex = 9;
            this.chkAutoClean.Text = "Automatically clean up temporary files";
            this.chkAutoClean.UseVisualStyleBackColor = true;
            // 
            // chkCheckForGames
            // 
            this.chkCheckForGames.AutoSize = true;
            this.chkCheckForGames.ForeColor = System.Drawing.Color.Gainsboro;
            this.chkCheckForGames.Location = new System.Drawing.Point(102, 113);
            this.chkCheckForGames.Name = "chkCheckForGames";
            this.chkCheckForGames.Size = new System.Drawing.Size(226, 17);
            this.chkCheckForGames.TabIndex = 10;
            this.chkCheckForGames.Text = "Check for game installations on startup";
            this.chkCheckForGames.UseVisualStyleBackColor = true;
            // 
            // chkAutoCloseLog
            // 
            this.chkAutoCloseLog.AutoSize = true;
            this.chkAutoCloseLog.ForeColor = System.Drawing.Color.Gainsboro;
            this.chkAutoCloseLog.Location = new System.Drawing.Point(102, 136);
            this.chkAutoCloseLog.Name = "chkAutoCloseLog";
            this.chkAutoCloseLog.Size = new System.Drawing.Size(221, 17);
            this.chkAutoCloseLog.TabIndex = 11;
            this.chkAutoCloseLog.Text = "Auto-close debug log on successful save";
            this.chkAutoCloseLog.UseVisualStyleBackColor = true;
            // 
            // chkDeveloperMode
            // 
            this.chkDeveloperMode.AutoSize = true;
            this.chkDeveloperMode.ForeColor = System.Drawing.Color.Gainsboro;
            this.chkDeveloperMode.Location = new System.Drawing.Point(102, 159);
            this.chkDeveloperMode.Name = "chkDeveloperMode";
            this.chkDeveloperMode.Size = new System.Drawing.Size(138, 17);
            this.chkDeveloperMode.TabIndex = 12;
            this.chkDeveloperMode.Text = "Enable Developer Mode";
            this.chkDeveloperMode.UseVisualStyleBackColor = true;
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 191);
            this.Controls.Add(this.chkDeveloperMode);
            this.Controls.Add(this.chkAutoCloseLog);
            this.Controls.Add(this.chkCheckForGames);
            this.Controls.Add(this.chkAutoClean);
            this.Controls.Add(this.chkSortEnabled);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnBrowseModsDir);
            this.Controls.Add(this.txtModsDir);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnBrowseGameDir);
            this.Controls.Add(this.txtGameDir);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Settings";
            this.Load += new System.EventHandler(this.SettingsForm_Load);
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.ForeColor = System.Drawing.Color.White;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtGameDir;
        private System.Windows.Forms.Button btnBrowseGameDir;
        private System.Windows.Forms.Button btnBrowseModsDir;
        private System.Windows.Forms.TextBox txtModsDir;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.CheckBox chkSortEnabled;
        private System.Windows.Forms.CheckBox chkAutoClean;
        private System.Windows.Forms.CheckBox chkCheckForGames;
        private System.Windows.Forms.CheckBox chkAutoCloseLog;
        private System.Windows.Forms.CheckBox chkDeveloperMode;
    }
}