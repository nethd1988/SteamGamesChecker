namespace SteamGamesChecker
{
    partial class MainForm
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
            this.lvGameHistory = new System.Windows.Forms.ListView();
            this.chGameName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chAppID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chLastUpdate = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chDaysAgo = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnCheckUpdate = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtAppID = new System.Windows.Forms.TextBox();
            this.cbMethod = new System.Windows.Forms.ComboBox();
            this.btnScanAll = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripProgressBar1 = new System.Windows.Forms.ToolStripProgressBar();
            this.btnSave = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.lbSavedIDs = new System.Windows.Forms.ListBox();
            this.btnRemove = new System.Windows.Forms.Button();
            this.btnAutoScan = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.txtScanInterval = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.telegramMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.btnConfigTelegram = new System.Windows.Forms.Button();
            this.statusStrip1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lvGameHistory
            // 
            this.lvGameHistory.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chGameName,
            this.chAppID,
            this.chLastUpdate,
            this.chDaysAgo});
            this.lvGameHistory.HideSelection = false;
            this.lvGameHistory.Location = new System.Drawing.Point(12, 130);
            this.lvGameHistory.Name = "lvGameHistory";
            this.lvGameHistory.Size = new System.Drawing.Size(840, 259);
            this.lvGameHistory.TabIndex = 0;
            this.lvGameHistory.UseCompatibleStateImageBehavior = false;
            this.lvGameHistory.View = System.Windows.Forms.View.Details;
            // 
            // chGameName
            // 
            this.chGameName.Text = "Tên Game";
            this.chGameName.Width = 250;
            // 
            // chAppID
            // 
            this.chAppID.Text = "App ID";
            this.chAppID.Width = 80;
            // 
            // chLastUpdate
            // 
            this.chLastUpdate.Text = "Lần Cập Nhật Cuối";
            this.chLastUpdate.Width = 350;
            // 
            // chDaysAgo
            // 
            this.chDaysAgo.Text = "Ngày Trước";
            this.chDaysAgo.Width = 80;
            // 
            // btnCheckUpdate
            // 
            this.btnCheckUpdate.Location = new System.Drawing.Point(296, 44);
            this.btnCheckUpdate.Name = "btnCheckUpdate";
            this.btnCheckUpdate.Size = new System.Drawing.Size(75, 23);
            this.btnCheckUpdate.TabIndex = 1;
            this.btnCheckUpdate.Text = "Kiểm Tra";
            this.btnCheckUpdate.UseVisualStyleBackColor = true;
            this.btnCheckUpdate.Click += new System.EventHandler(this.btnCheckUpdate_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 49);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(45, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "App ID:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(135, 49);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(58, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Phương pháp:";
            // 
            // txtAppID
            // 
            this.txtAppID.Location = new System.Drawing.Point(63, 46);
            this.txtAppID.Name = "txtAppID";
            this.txtAppID.Size = new System.Drawing.Size(66, 20);
            this.txtAppID.TabIndex = 4;
            // 
            // cbMethod
            // 
            this.cbMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbMethod.FormattingEnabled = true;
            this.cbMethod.Items.AddRange(new object[] {
            "Steam API",
            "SteamDB"});
            this.cbMethod.Location = new System.Drawing.Point(199, 46);
            this.cbMethod.Name = "cbMethod";
            this.cbMethod.Size = new System.Drawing.Size(91, 21);
            this.cbMethod.TabIndex = 5;
            // 
            // btnScanAll
            // 
            this.btnScanAll.Location = new System.Drawing.Point(722, 71);
            this.btnScanAll.Name = "btnScanAll";
            this.btnScanAll.Size = new System.Drawing.Size(130, 23);
            this.btnScanAll.TabIndex = 6;
            this.btnScanAll.Text = "Quét Tất Cả";
            this.btnScanAll.UseVisualStyleBackColor = true;
            this.btnScanAll.Click += new System.EventHandler(this.btnScanAll_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus,
            this.toolStripProgressBar1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 439);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(864, 22);
            this.statusStrip1.TabIndex = 7;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(107, 17);
            this.lblStatus.Text = "Trạng thái: Sẵn sàng";
            // 
            // toolStripProgressBar1
            // 
            this.toolStripProgressBar1.Name = "toolStripProgressBar1";
            this.toolStripProgressBar1.Size = new System.Drawing.Size(100, 16);
            this.toolStripProgressBar1.Visible = false;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(377, 44);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 8;
            this.btnSave.Text = "Lưu";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(472, 29);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(98, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "Game đã lưu";
            // 
            // lbSavedIDs
            // 
            this.lbSavedIDs.FormattingEnabled = true;
            this.lbSavedIDs.Location = new System.Drawing.Point(475, 45);
            this.lbSavedIDs.Name = "lbSavedIDs";
            this.lbSavedIDs.Size = new System.Drawing.Size(241, 69);
            this.lbSavedIDs.TabIndex = 10;
            // 
            // btnRemove
            // 
            this.btnRemove.Location = new System.Drawing.Point(722, 42);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(130, 23);
            this.btnRemove.TabIndex = 11;
            this.btnRemove.Text = "Xóa Game";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // btnAutoScan
            // 
            this.btnAutoScan.Location = new System.Drawing.Point(296, 73);
            this.btnAutoScan.Name = "btnAutoScan";
            this.btnAutoScan.Size = new System.Drawing.Size(156, 23);
            this.btnAutoScan.TabIndex = 12;
            this.btnAutoScan.Text = "Tự Động Quét";
            this.btnAutoScan.UseVisualStyleBackColor = true;
            this.btnAutoScan.Click += new System.EventHandler(this.btnAutoScan_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 76);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(86, 13);
            this.label4.TabIndex = 13;
            this.label4.Text = "Thời gian quét (phút):";
            // 
            // txtScanInterval
            // 
            this.txtScanInterval.Location = new System.Drawing.Point(199, 73);
            this.txtScanInterval.Name = "txtScanInterval";
            this.txtScanInterval.Size = new System.Drawing.Size(91, 20);
            this.txtScanInterval.TabIndex = 14;
            this.txtScanInterval.Text = "60";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 111);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(96, 13);
            this.label5.TabIndex = 15;
            this.label5.Text = "Lịch sử cập nhật:";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileMenuItem,
            this.toolsMenuItem,
            this.helpMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(864, 24);
            this.menuStrip1.TabIndex = 24;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileMenuItem
            // 
            this.fileMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitMenuItem});
            this.fileMenuItem.Name = "fileMenuItem";
            this.fileMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileMenuItem.Text = "File";
            // 
            // exitMenuItem
            // 
            this.exitMenuItem.Name = "exitMenuItem";
            this.exitMenuItem.Size = new System.Drawing.Size(180, 22);
            this.exitMenuItem.Text = "Thoát";
            this.exitMenuItem.Click += new System.EventHandler(this.exitMenuItem_Click);
            // 
            // toolsMenuItem
            // 
            this.toolsMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.telegramMenuItem});
            this.toolsMenuItem.Name = "toolsMenuItem";
            this.toolsMenuItem.Size = new System.Drawing.Size(70, 20);
            this.toolsMenuItem.Text = "Công cụ";
            // 
            // telegramMenuItem
            // 
            this.telegramMenuItem.Name = "telegramMenuItem";
            this.telegramMenuItem.Size = new System.Drawing.Size(180, 22);
            this.telegramMenuItem.Text = "Telegram Bot";
            this.telegramMenuItem.Click += new System.EventHandler(this.telegramMenuItem_Click);
            // 
            // helpMenuItem
            // 
            this.helpMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutMenuItem});
            this.helpMenuItem.Name = "helpMenuItem";
            this.helpMenuItem.Size = new System.Drawing.Size(62, 20);
            this.helpMenuItem.Text = "Trợ giúp";
            // 
            // aboutMenuItem
            // 
            this.aboutMenuItem.Name = "aboutMenuItem";
            this.aboutMenuItem.Size = new System.Drawing.Size(180, 22);
            this.aboutMenuItem.Text = "Giới thiệu";
            this.aboutMenuItem.Click += new System.EventHandler(this.aboutMenuItem_Click);
            // 
            // btnConfigTelegram
            // 
            this.btnConfigTelegram.Location = new System.Drawing.Point(618, 407);
            this.btnConfigTelegram.Name = "btnConfigTelegram";
            this.btnConfigTelegram.Size = new System.Drawing.Size(130, 23);
            this.btnConfigTelegram.TabIndex = 25;
            this.btnConfigTelegram.Text = "Cấu hình Telegram";
            this.btnConfigTelegram.UseVisualStyleBackColor = true;
            this.btnConfigTelegram.Click += new System.EventHandler(this.btnConfigTelegram_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(864, 461);
            this.Controls.Add(this.btnConfigTelegram);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.txtScanInterval);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.btnAutoScan);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.lbSavedIDs);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.btnScanAll);
            this.Controls.Add(this.cbMethod);
            this.Controls.Add(this.txtAppID);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnCheckUpdate);
            this.Controls.Add(this.lvGameHistory);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Steam Games Checker";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView lvGameHistory;
        private System.Windows.Forms.ColumnHeader chGameName;
        private System.Windows.Forms.ColumnHeader chAppID;
        private System.Windows.Forms.ColumnHeader chLastUpdate;
        private System.Windows.Forms.ColumnHeader chDaysAgo;
        private System.Windows.Forms.Button btnCheckUpdate;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtAppID;
        private System.Windows.Forms.ComboBox cbMethod;
        private System.Windows.Forms.Button btnScanAll;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar1;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ListBox lbSavedIDs;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnAutoScan;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtScanInterval;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem telegramMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutMenuItem;
        private System.Windows.Forms.Button btnConfigTelegram;
    }
}