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
            this.txtAppID = new System.Windows.Forms.TextBox();
            this.lblAppID = new System.Windows.Forms.Label();
            this.btnCheckUpdate = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.cbMethod = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.lbSavedIDs = new System.Windows.Forms.ListBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.btnScanAll = new System.Windows.Forms.Button();
            this.btnAutoScan = new System.Windows.Forms.Button();
            this.txtScanInterval = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripProgressBar1 = new System.Windows.Forms.ToolStripProgressBar();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.telegramMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.btnConfigTelegram = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageGameList = new System.Windows.Forms.TabPage();
            this.lvGameHistory = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tabPageScanHistory = new System.Windows.Forms.TabPage();
            this.lvScanHistory = new System.Windows.Forms.ListView();
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader8 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader9 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnClearHistory = new System.Windows.Forms.Button();
            this.statusStrip1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPageGameList.SuspendLayout();
            this.tabPageScanHistory.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtAppID
            // 
            this.txtAppID.Location = new System.Drawing.Point(71, 39);
            this.txtAppID.Name = "txtAppID";
            this.txtAppID.Size = new System.Drawing.Size(100, 20);
            this.txtAppID.TabIndex = 0;
            // 
            // lblAppID
            // 
            this.lblAppID.AutoSize = true;
            this.lblAppID.Location = new System.Drawing.Point(12, 42);
            this.lblAppID.Name = "lblAppID";
            this.lblAppID.Size = new System.Drawing.Size(43, 13);
            this.lblAppID.TabIndex = 1;
            this.lblAppID.Text = "App ID:";
            // 
            // btnCheckUpdate
            // 
            this.btnCheckUpdate.Location = new System.Drawing.Point(423, 39);
            this.btnCheckUpdate.Name = "btnCheckUpdate";
            this.btnCheckUpdate.Size = new System.Drawing.Size(122, 23);
            this.btnCheckUpdate.TabIndex = 2;
            this.btnCheckUpdate.Text = "Kiểm Tra Cập Nhật";
            this.btnCheckUpdate.UseVisualStyleBackColor = true;
            this.btnCheckUpdate.Click += new System.EventHandler(this.btnCheckUpdate_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(12, 73);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(126, 13);
            this.lblStatus.TabIndex = 4;
            this.lblStatus.Text = "Trạng thái: Chưa kiểm tra";
            // 
            // cbMethod
            // 
            this.cbMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbMethod.FormattingEnabled = true;
            this.cbMethod.Location = new System.Drawing.Point(254, 38);
            this.cbMethod.Name = "cbMethod";
            this.cbMethod.Size = new System.Drawing.Size(121, 21);
            this.cbMethod.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(177, 42);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Phương thức:";
            // 
            // lbSavedIDs
            // 
            this.lbSavedIDs.FormattingEnabled = true;
            this.lbSavedIDs.Location = new System.Drawing.Point(12, 292);
            this.lbSavedIDs.Name = "lbSavedIDs";
            this.lbSavedIDs.Size = new System.Drawing.Size(300, 199);
            this.lbSavedIDs.TabIndex = 7;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(318, 292);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 8;
            this.btnSave.Text = "Lưu ID";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnRemove
            // 
            this.btnRemove.Location = new System.Drawing.Point(318, 321);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(75, 23);
            this.btnRemove.TabIndex = 9;
            this.btnRemove.Text = "Xóa ID";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // btnScanAll
            // 
            this.btnScanAll.Location = new System.Drawing.Point(399, 292);
            this.btnScanAll.Name = "btnScanAll";
            this.btnScanAll.Size = new System.Drawing.Size(94, 23);
            this.btnScanAll.TabIndex = 10;
            this.btnScanAll.Text = "Quét Tất Cả";
            this.btnScanAll.UseVisualStyleBackColor = true;
            this.btnScanAll.Click += new System.EventHandler(this.btnScanAll_Click);
            // 
            // btnAutoScan
            // 
            this.btnAutoScan.Location = new System.Drawing.Point(399, 321);
            this.btnAutoScan.Name = "btnAutoScan";
            this.btnAutoScan.Size = new System.Drawing.Size(94, 23);
            this.btnAutoScan.TabIndex = 11;
            this.btnAutoScan.Text = "Tự Động Quét";
            this.btnAutoScan.UseVisualStyleBackColor = true;
            this.btnAutoScan.Click += new System.EventHandler(this.btnAutoScan_Click);
            // 
            // txtScanInterval
            // 
            this.txtScanInterval.Location = new System.Drawing.Point(399, 364);
            this.txtScanInterval.Name = "txtScanInterval";
            this.txtScanInterval.Size = new System.Drawing.Size(52, 20);
            this.txtScanInterval.TabIndex = 12;
            this.txtScanInterval.Text = "15";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(336, 367);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(52, 13);
            this.label2.TabIndex = 13;
            this.label2.Text = "Quét mỗi:";
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripProgressBar1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 578);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(800, 22);
            this.statusStrip1.TabIndex = 14;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripProgressBar1
            // 
            this.toolStripProgressBar1.Name = "toolStripProgressBar1";
            this.toolStripProgressBar1.Size = new System.Drawing.Size(100, 16);
            this.toolStripProgressBar1.Visible = false;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.toolsToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(800, 24);
            this.menuStrip1.TabIndex = 15;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // exitMenuItem
            // 
            this.exitMenuItem.Name = "exitMenuItem";
            this.exitMenuItem.Size = new System.Drawing.Size(104, 22);
            this.exitMenuItem.Text = "Thoát";
            this.exitMenuItem.Click += new System.EventHandler(this.exitMenuItem_Click);
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.telegramMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(64, 20);
            this.toolsToolStripMenuItem.Text = "Công cụ";
            // 
            // telegramMenuItem
            // 
            this.telegramMenuItem.Name = "telegramMenuItem";
            this.telegramMenuItem.Size = new System.Drawing.Size(143, 22);
            this.telegramMenuItem.Text = "Telegram Bot";
            this.telegramMenuItem.Click += new System.EventHandler(this.telegramMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(62, 20);
            this.helpToolStripMenuItem.Text = "Trợ giúp";
            // 
            // aboutMenuItem
            // 
            this.aboutMenuItem.Name = "aboutMenuItem";
            this.aboutMenuItem.Size = new System.Drawing.Size(125, 22);
            this.aboutMenuItem.Text = "Giới thiệu";
            this.aboutMenuItem.Click += new System.EventHandler(this.aboutMenuItem_Click);
            // 
            // btnConfigTelegram
            // 
            this.btnConfigTelegram.Location = new System.Drawing.Point(12, 500);
            this.btnConfigTelegram.Name = "btnConfigTelegram";
            this.btnConfigTelegram.Size = new System.Drawing.Size(140, 23);
            this.btnConfigTelegram.TabIndex = 16;
            this.btnConfigTelegram.Text = "Cấu hình Telegram Bot";
            this.btnConfigTelegram.UseVisualStyleBackColor = true;
            this.btnConfigTelegram.Click += new System.EventHandler(this.btnConfigTelegram_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(457, 367);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(28, 13);
            this.label3.TabIndex = 17;
            this.label3.Text = "phút";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPageGameList);
            this.tabControl1.Controls.Add(this.tabPageScanHistory);
            this.tabControl1.Location = new System.Drawing.Point(12, 89);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(776, 197);
            this.tabControl1.TabIndex = 18;
            // 
            // tabPageGameList
            // 
            this.tabPageGameList.Controls.Add(this.lvGameHistory);
            this.tabPageGameList.Location = new System.Drawing.Point(4, 22);
            this.tabPageGameList.Name = "tabPageGameList";
            this.tabPageGameList.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageGameList.Size = new System.Drawing.Size(768, 171);
            this.tabPageGameList.TabIndex = 0;
            this.tabPageGameList.Text = "Danh sách game";
            this.tabPageGameList.UseVisualStyleBackColor = true;
            // 
            // lvGameHistory
            // 
            this.lvGameHistory.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4});
            this.lvGameHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvGameHistory.FullRowSelect = true;
            this.lvGameHistory.GridLines = true;
            this.lvGameHistory.HideSelection = false;
            this.lvGameHistory.Location = new System.Drawing.Point(3, 3);
            this.lvGameHistory.Name = "lvGameHistory";
            this.lvGameHistory.Size = new System.Drawing.Size(762, 165);
            this.lvGameHistory.TabIndex = 0;
            this.lvGameHistory.UseCompatibleStateImageBehavior = false;
            this.lvGameHistory.View = System.Windows.Forms.View.Details;
            this.lvGameHistory.SelectedIndexChanged += new System.EventHandler(this.lvGameHistory_SelectedIndexChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Tên Game";
            this.columnHeader1.Width = 250;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "ID";
            this.columnHeader2.Width = 100;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Lần Cập Nhật Cuối";
            this.columnHeader3.Width = 250;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Ngày";
            this.columnHeader4.Width = 150;
            // 
            // tabPageScanHistory
            // 
            this.tabPageScanHistory.Controls.Add(this.lvScanHistory);
            this.tabPageScanHistory.Location = new System.Drawing.Point(4, 22);
            this.tabPageScanHistory.Name = "tabPageScanHistory";
            this.tabPageScanHistory.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageScanHistory.Size = new System.Drawing.Size(768, 171);
            this.tabPageScanHistory.TabIndex = 1;
            this.tabPageScanHistory.Text = "Lịch sử quét";
            this.tabPageScanHistory.UseVisualStyleBackColor = true;
            // 
            // lvScanHistory
            // 
            this.lvScanHistory.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader5,
            this.columnHeader6,
            this.columnHeader7,
            this.columnHeader8,
            this.columnHeader9});
            this.lvScanHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvScanHistory.FullRowSelect = true;
            this.lvScanHistory.GridLines = true;
            this.lvScanHistory.HideSelection = false;
            this.lvScanHistory.Location = new System.Drawing.Point(3, 3);
            this.lvScanHistory.Name = "lvScanHistory";
            this.lvScanHistory.Size = new System.Drawing.Size(762, 165);
            this.lvScanHistory.TabIndex = 0;
            this.lvScanHistory.UseCompatibleStateImageBehavior = false;
            this.lvScanHistory.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "Thời gian quét";
            this.columnHeader5.Width = 150;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "Tổng số";
            this.columnHeader6.Width = 80;
            // 
            // columnHeader7
            // 
            this.columnHeader7.Text = "Thành công";
            this.columnHeader7.Width = 100;
            // 
            // columnHeader8
            // 
            this.columnHeader8.Text = "Thất bại";
            this.columnHeader8.Width = 80;
            // 
            // columnHeader9
            // 
            this.columnHeader9.Text = "Cập nhật mới";
            this.columnHeader9.Width = 200;
            // 
            // btnClearHistory
            // 
            this.btnClearHistory.Location = new System.Drawing.Point(693, 500);
            this.btnClearHistory.Name = "btnClearHistory";
            this.btnClearHistory.Size = new System.Drawing.Size(95, 23);
            this.btnClearHistory.TabIndex = 19;
            this.btnClearHistory.Text = "Xóa lịch sử quét";
            this.btnClearHistory.UseVisualStyleBackColor = true;
            this.btnClearHistory.Click += new System.EventHandler(this.btnClearHistory_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this.btnClearHistory);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btnConfigTelegram);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtScanInterval);
            this.Controls.Add(this.btnAutoScan);
            this.Controls.Add(this.btnScanAll);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.lbSavedIDs);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cbMethod);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnCheckUpdate);
            this.Controls.Add(this.lblAppID);
            this.Controls.Add(this.txtAppID);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Steam Games Checker";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPageGameList.ResumeLayout(false);
            this.tabPageScanHistory.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtAppID;
        private System.Windows.Forms.Label lblAppID;
        private System.Windows.Forms.Button btnCheckUpdate;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.ComboBox cbMethod;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox lbSavedIDs;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnScanAll;
        private System.Windows.Forms.Button btnAutoScan;
        private System.Windows.Forms.TextBox txtScanInterval;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem telegramMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutMenuItem;
        private System.Windows.Forms.Button btnConfigTelegram;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageGameList;
        private System.Windows.Forms.ListView lvGameHistory;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.TabPage tabPageScanHistory;
        private System.Windows.Forms.ListView lvScanHistory;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.ColumnHeader columnHeader7;
        private System.Windows.Forms.ColumnHeader columnHeader8;
        private System.Windows.Forms.ColumnHeader columnHeader9;
        private System.Windows.Forms.Button btnClearHistory;
    }
}