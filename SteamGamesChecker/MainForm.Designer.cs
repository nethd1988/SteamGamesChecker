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
            this.lblAppID = new System.Windows.Forms.Label();
            this.txtAppID = new System.Windows.Forms.TextBox();
            this.btnCheckGame = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblGameName = new System.Windows.Forms.Label();
            this.lblLastUpdate = new System.Windows.Forms.Label();
            this.lblDaysAgo = new System.Windows.Forms.Label();
            this.lblDeveloper = new System.Windows.Forms.Label();
            this.lblPublisher = new System.Windows.Forms.Label();
            this.lblReleaseDate = new System.Windows.Forms.Label();
            this.lvGameHistory = new System.Windows.Forms.ListView();
            this.chGameName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chAppID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chLastUpdate = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chUpdateAgo = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lblHistory = new System.Windows.Forms.Label();
            this.cbMethod = new System.Windows.Forms.ComboBox();
            this.lblMethod = new System.Windows.Forms.Label();
            this.btnAddToList = new System.Windows.Forms.Button();
            this.lbSavedIDs = new System.Windows.Forms.ListBox();
            this.lblSavedIDs = new System.Windows.Forms.Label();
            this.btnRemoveID = new System.Windows.Forms.Button();
            this.btnScanAll = new System.Windows.Forms.Button();
            this.gbAutoScan = new System.Windows.Forms.GroupBox();
            this.rbDisabled = new System.Windows.Forms.RadioButton();
            this.rb24h = new System.Windows.Forms.RadioButton();
            this.rb12h = new System.Windows.Forms.RadioButton();
            this.rb6h = new System.Windows.Forms.RadioButton();
            this.rb1h = new System.Windows.Forms.RadioButton();
            this.rb30m = new System.Windows.Forms.RadioButton();
            this.lblNextScan = new System.Windows.Forms.Label();
            this.btnSortByUpdateTime = new System.Windows.Forms.Button();
            this.lblSortStatus = new System.Windows.Forms.Label();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripProgressBar1 = new System.Windows.Forms.ToolStripProgressBar();
            this.gbAutoScan.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblAppID
            // 
            this.lblAppID.AutoSize = true;
            this.lblAppID.Location = new System.Drawing.Point(12, 15);
            this.lblAppID.Name = "lblAppID";
            this.lblAppID.Size = new System.Drawing.Size(79, 13);
            this.lblAppID.TabIndex = 0;
            this.lblAppID.Text = "Nhập ID Game:";
            // 
            // txtAppID
            // 
            this.txtAppID.Location = new System.Drawing.Point(97, 12);
            this.txtAppID.Name = "txtAppID";
            this.txtAppID.Size = new System.Drawing.Size(100, 20);
            this.txtAppID.TabIndex = 1;
            // 
            // btnCheckGame
            // 
            this.btnCheckGame.Location = new System.Drawing.Point(377, 10);
            this.btnCheckGame.Name = "btnCheckGame";
            this.btnCheckGame.Size = new System.Drawing.Size(75, 23);
            this.btnCheckGame.TabIndex = 2;
            this.btnCheckGame.Text = "Kiểm tra";
            this.btnCheckGame.UseVisualStyleBackColor = true;
            this.btnCheckGame.Click += new System.EventHandler(this.btnCheckGame_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(12, 45);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(91, 13);
            this.lblStatus.TabIndex = 3;
            this.lblStatus.Text = "Trạng thái: Sẵn sàng";
            this.lblStatus.ForeColor = System.Drawing.Color.Green;
            // 
            // lblGameName
            // 
            this.lblGameName.AutoSize = true;
            this.lblGameName.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblGameName.Location = new System.Drawing.Point(12, 70);
            this.lblGameName.Name = "lblGameName";
            this.lblGameName.Size = new System.Drawing.Size(79, 16);
            this.lblGameName.TabIndex = 4;
            this.lblGameName.Text = "Tên game:";
            // 
            // lblLastUpdate
            // 
            this.lblLastUpdate.AutoSize = true;
            this.lblLastUpdate.Location = new System.Drawing.Point(12, 95);
            this.lblLastUpdate.Name = "lblLastUpdate";
            this.lblLastUpdate.Size = new System.Drawing.Size(122, 13);
            this.lblLastUpdate.TabIndex = 5;
            this.lblLastUpdate.Text = "Thời gian cập nhật gần nhất:";
            // 
            // lblDaysAgo
            // 
            this.lblDaysAgo.AutoSize = true;
            this.lblDaysAgo.Location = new System.Drawing.Point(12, 115);
            this.lblDaysAgo.Name = "lblDaysAgo";
            this.lblDaysAgo.Size = new System.Drawing.Size(0, 13);
            this.lblDaysAgo.TabIndex = 6;
            // 
            // lblDeveloper
            // 
            this.lblDeveloper.AutoSize = true;
            this.lblDeveloper.Location = new System.Drawing.Point(12, 135);
            this.lblDeveloper.Name = "lblDeveloper";
            this.lblDeveloper.Size = new System.Drawing.Size(67, 13);
            this.lblDeveloper.TabIndex = 7;
            this.lblDeveloper.Text = "Nhà phát triển:";
            // 
            // lblPublisher
            // 
            this.lblPublisher.AutoSize = true;
            this.lblPublisher.Location = new System.Drawing.Point(12, 155);
            this.lblPublisher.Name = "lblPublisher";
            this.lblPublisher.Size = new System.Drawing.Size(70, 13);
            this.lblPublisher.TabIndex = 8;
            this.lblPublisher.Text = "Nhà phát hành:";
            // 
            // lblReleaseDate
            // 
            this.lblReleaseDate.AutoSize = true;
            this.lblReleaseDate.Location = new System.Drawing.Point(12, 175);
            this.lblReleaseDate.Name = "lblReleaseDate";
            this.lblReleaseDate.Size = new System.Drawing.Size(75, 13);
            this.lblReleaseDate.TabIndex = 9;
            this.lblReleaseDate.Text = "Ngày phát hành:";
            // 
            // lvGameHistory
            // 
            this.lvGameHistory.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chGameName,
            this.chAppID,
            this.chLastUpdate,
            this.chUpdateAgo});
            this.lvGameHistory.FullRowSelect = true;
            this.lvGameHistory.HideSelection = false;
            this.lvGameHistory.Location = new System.Drawing.Point(12, 220);
            this.lvGameHistory.Name = "lvGameHistory";
            this.lvGameHistory.Size = new System.Drawing.Size(600, 150);
            this.lvGameHistory.TabIndex = 10;
            this.lvGameHistory.UseCompatibleStateImageBehavior = false;
            this.lvGameHistory.View = System.Windows.Forms.View.Details;
            this.lvGameHistory.SelectedIndexChanged += new System.EventHandler(this.lvGameHistory_SelectedIndexChanged);
            this.lvGameHistory.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.lvGameHistory_ColumnClick);
            // 
            // chGameName
            // 
            this.chGameName.Text = "Tên Game";
            this.chGameName.Width = 200;
            // 
            // chAppID
            // 
            this.chAppID.Text = "ID";
            this.chAppID.Width = 60;
            // 
            // chLastUpdate
            // 
            this.chLastUpdate.Text = "Cập nhật gần nhất (Giờ Việt Nam GMT+7)";
            this.chLastUpdate.Width = 250;
            // 
            // chUpdateAgo
            // 
            this.chUpdateAgo.Text = "Số ngày";
            this.chUpdateAgo.Width = 70;
            // 
            // lblHistory
            // 
            this.lblHistory.AutoSize = true;
            this.lblHistory.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblHistory.Location = new System.Drawing.Point(12, 200);
            this.lblHistory.Name = "lblHistory";
            this.lblHistory.Size = new System.Drawing.Size(88, 13);
            this.lblHistory.TabIndex = 11;
            this.lblHistory.Text = "Lịch sử tra cứu:";
            // 
            // cbMethod
            // 
            this.cbMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbMethod.FormattingEnabled = true;
            this.cbMethod.Items.AddRange(new object[] {
            "Steam API",
            "Selenium",
            "Steam CDN"});
            this.cbMethod.Location = new System.Drawing.Point(298, 11);
            this.cbMethod.Name = "cbMethod";
            this.cbMethod.Size = new System.Drawing.Size(73, 21);
            this.cbMethod.TabIndex = 12;
            // 
            // lblMethod
            // 
            this.lblMethod.AutoSize = true;
            this.lblMethod.Location = new System.Drawing.Point(215, 14);
            this.lblMethod.Name = "lblMethod";
            this.lblMethod.Size = new System.Drawing.Size(77, 13);
            this.lblMethod.TabIndex = 13;
            this.lblMethod.Text = "Phương pháp:";
            // 
            // btnAddToList
            // 
            this.btnAddToList.Location = new System.Drawing.Point(458, 10);
            this.btnAddToList.Name = "btnAddToList";
            this.btnAddToList.Size = new System.Drawing.Size(75, 23);
            this.btnAddToList.TabIndex = 14;
            this.btnAddToList.Text = "Thêm vào DS";
            this.btnAddToList.UseVisualStyleBackColor = true;
            this.btnAddToList.Click += new System.EventHandler(this.btnAddToList_Click);
            // 
            // lbSavedIDs
            // 
            this.lbSavedIDs.FormattingEnabled = true;
            this.lbSavedIDs.Location = new System.Drawing.Point(618, 30);
            this.lbSavedIDs.Name = "lbSavedIDs";
            this.lbSavedIDs.Size = new System.Drawing.Size(233, 290);
            this.lbSavedIDs.TabIndex = 15;
            this.lbSavedIDs.SelectedIndexChanged += new System.EventHandler(this.lbSavedIDs_SelectedIndexChanged);
            // 
            // lblSavedIDs
            // 
            this.lblSavedIDs.AutoSize = true;
            this.lblSavedIDs.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSavedIDs.Location = new System.Drawing.Point(618, 11);
            this.lblSavedIDs.Name = "lblSavedIDs";
            this.lblSavedIDs.Size = new System.Drawing.Size(122, 13);
            this.lblSavedIDs.TabIndex = 16;
            this.lblSavedIDs.Text = "Danh sách ID Game:";
            // 
            // btnRemoveID
            // 
            this.btnRemoveID.Location = new System.Drawing.Point(618, 330);
            this.btnRemoveID.Name = "btnRemoveID";
            this.btnRemoveID.Size = new System.Drawing.Size(75, 23);
            this.btnRemoveID.TabIndex = 17;
            this.btnRemoveID.Text = "Xóa ID";
            this.btnRemoveID.UseVisualStyleBackColor = true;
            this.btnRemoveID.Click += new System.EventHandler(this.btnRemoveID_Click);
            // 
            // btnScanAll
            // 
            this.btnScanAll.Location = new System.Drawing.Point(776, 330);
            this.btnScanAll.Name = "btnScanAll";
            this.btnScanAll.Size = new System.Drawing.Size(75, 23);
            this.btnScanAll.TabIndex = 18;
            this.btnScanAll.Text = "Quét tất cả";
            this.btnScanAll.UseVisualStyleBackColor = true;
            this.btnScanAll.Click += new System.EventHandler(this.btnScanAll_Click);
            // 
            // gbAutoScan
            // 
            this.gbAutoScan.Controls.Add(this.rbDisabled);
            this.gbAutoScan.Controls.Add(this.rb24h);
            this.gbAutoScan.Controls.Add(this.rb12h);
            this.gbAutoScan.Controls.Add(this.rb6h);
            this.gbAutoScan.Controls.Add(this.rb1h);
            this.gbAutoScan.Controls.Add(this.rb30m);
            this.gbAutoScan.Location = new System.Drawing.Point(618, 359);
            this.gbAutoScan.Name = "gbAutoScan";
            this.gbAutoScan.Size = new System.Drawing.Size(233, 72);
            this.gbAutoScan.TabIndex = 19;
            this.gbAutoScan.TabStop = false;
            this.gbAutoScan.Text = "Quét tự động (khoảng thời gian)";
            // 
            // rbDisabled
            // 
            this.rbDisabled.AutoSize = true;
            this.rbDisabled.Checked = true;
            this.rbDisabled.Location = new System.Drawing.Point(15, 19);
            this.rbDisabled.Name = "rbDisabled";
            this.rbDisabled.Size = new System.Drawing.Size(45, 17);
            this.rbDisabled.TabIndex = 0;
            this.rbDisabled.TabStop = true;
            this.rbDisabled.Text = "Tắt";
            this.rbDisabled.UseVisualStyleBackColor = true;
            this.rbDisabled.CheckedChanged += new System.EventHandler(this.rbScanInterval_CheckedChanged);
            // 
            // rb24h
            // 
            this.rb24h.AutoSize = true;
            this.rb24h.Location = new System.Drawing.Point(166, 44);
            this.rb24h.Name = "rb24h";
            this.rb24h.Size = new System.Drawing.Size(52, 17);
            this.rb24h.TabIndex = 5;
            this.rb24h.Text = "24 giờ";
            this.rb24h.UseVisualStyleBackColor = true;
            this.rb24h.CheckedChanged += new System.EventHandler(this.rbScanInterval_CheckedChanged);
            // 
            // rb12h
            // 
            this.rb12h.AutoSize = true;
            this.rb12h.Location = new System.Drawing.Point(107, 44);
            this.rb12h.Name = "rb12h";
            this.rb12h.Size = new System.Drawing.Size(52, 17);
            this.rb12h.TabIndex = 4;
            this.rb12h.Text = "12 giờ";
            this.rb12h.UseVisualStyleBackColor = true;
            this.rb12h.CheckedChanged += new System.EventHandler(this.rbScanInterval_CheckedChanged);
            // 
            // rb6h
            // 
            this.rb6h.AutoSize = true;
            this.rb6h.Location = new System.Drawing.Point(15, 44);
            this.rb6h.Name = "rb6h";
            this.rb6h.Size = new System.Drawing.Size(46, 17);
            this.rb6h.TabIndex = 3;
            this.rb6h.Text = "6 giờ";
            this.rb6h.UseVisualStyleBackColor = true;
            this.rb6h.CheckedChanged += new System.EventHandler(this.rbScanInterval_CheckedChanged);
            // 
            // rb1h
            // 
            this.rb1h.AutoSize = true;
            this.rb1h.Location = new System.Drawing.Point(166, 19);
            this.rb1h.Name = "rb1h";
            this.rb1h.Size = new System.Drawing.Size(46, 17);
            this.rb1h.TabIndex = 2;
            this.rb1h.Text = "1 giờ";
            this.rb1h.UseVisualStyleBackColor = true;
            this.rb1h.CheckedChanged += new System.EventHandler(this.rbScanInterval_CheckedChanged);
            // 
            // rb30m
            // 
            this.rb30m.AutoSize = true;
            this.rb30m.Location = new System.Drawing.Point(107, 19);
            this.rb30m.Name = "rb30m";
            this.rb30m.Size = new System.Drawing.Size(58, 17);
            this.rb30m.TabIndex = 1;
            this.rb30m.Text = "30 phút";
            this.rb30m.UseVisualStyleBackColor = true;
            this.rb30m.CheckedChanged += new System.EventHandler(this.rbScanInterval_CheckedChanged);
            // 
            // lblNextScan
            // 
            this.lblNextScan.AutoSize = true;
            this.lblNextScan.Location = new System.Drawing.Point(615, 441);
            this.lblNextScan.Name = "lblNextScan";
            this.lblNextScan.Size = new System.Drawing.Size(130, 13);
            this.lblNextScan.TabIndex = 20;
            this.lblNextScan.Text = "Quét tự động: Đã tắt";
            // 
            // btnSortByUpdateTime
            // 
            this.btnSortByUpdateTime.Location = new System.Drawing.Point(537, 195);
            this.btnSortByUpdateTime.Name = "btnSortByUpdateTime";
            this.btnSortByUpdateTime.Size = new System.Drawing.Size(75, 23);
            this.btnSortByUpdateTime.TabIndex = 21;
            this.btnSortByUpdateTime.Text = "Sắp xếp";
            this.btnSortByUpdateTime.UseVisualStyleBackColor = true;
            this.btnSortByUpdateTime.Click += new System.EventHandler(this.btnSortByUpdateTime_Click);
            // 
            // lblSortStatus
            // 
            this.lblSortStatus.AutoSize = true;
            this.lblSortStatus.Location = new System.Drawing.Point(343, 200);
            this.lblSortStatus.Name = "lblSortStatus";
            this.lblSortStatus.Size = new System.Drawing.Size(188, 13);
            this.lblSortStatus.TabIndex = 22;
            this.lblSortStatus.Text = "(Nhấn vào tiêu đề cột để sắp xếp)";
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripProgressBar1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 463);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(864, 22);
            this.statusStrip1.TabIndex = 23;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(111, 17);
            this.toolStripStatusLabel1.Text = "Steam Games Checker";
            // 
            // toolStripProgressBar1
            // 
            this.toolStripProgressBar1.Name = "toolStripProgressBar1";
            this.toolStripProgressBar1.Size = new System.Drawing.Size(200, 16);
            this.toolStripProgressBar1.Visible = false;
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(864, 485);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.lblSortStatus);
            this.Controls.Add(this.btnSortByUpdateTime);
            this.Controls.Add(this.lblNextScan);
            this.Controls.Add(this.gbAutoScan);
            this.Controls.Add(this.btnScanAll);
            this.Controls.Add(this.btnRemoveID);
            this.Controls.Add(this.lblSavedIDs);
            this.Controls.Add(this.lbSavedIDs);
            this.Controls.Add(this.btnAddToList);
            this.Controls.Add(this.lblMethod);
            this.Controls.Add(this.cbMethod);
            this.Controls.Add(this.lblHistory);
            this.Controls.Add(this.lvGameHistory);
            this.Controls.Add(this.lblReleaseDate);
            this.Controls.Add(this.lblPublisher);
            this.Controls.Add(this.lblDeveloper);
            this.Controls.Add(this.lblDaysAgo);
            this.Controls.Add(this.lblLastUpdate);
            this.Controls.Add(this.lblGameName);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnCheckGame);
            this.Controls.Add(this.txtAppID);
            this.Controls.Add(this.lblAppID);
            this.Name = "MainForm";
            this.Text = "Kiểm tra thông tin game Steam";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.gbAutoScan.ResumeLayout(false);
            this.gbAutoScan.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblAppID;
        private System.Windows.Forms.TextBox txtAppID;
        private System.Windows.Forms.Button btnCheckGame;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblGameName;
        private System.Windows.Forms.Label lblLastUpdate;
        private System.Windows.Forms.Label lblDaysAgo;
        private System.Windows.Forms.Label lblDeveloper;
        private System.Windows.Forms.Label lblPublisher;
        private System.Windows.Forms.Label lblReleaseDate;
        private System.Windows.Forms.ListView lvGameHistory;
        private System.Windows.Forms.ColumnHeader chGameName;
        private System.Windows.Forms.ColumnHeader chAppID;
        private System.Windows.Forms.ColumnHeader chLastUpdate;
        private System.Windows.Forms.ColumnHeader chUpdateAgo;
        private System.Windows.Forms.Label lblHistory;
        private System.Windows.Forms.ComboBox cbMethod;
        private System.Windows.Forms.Label lblMethod;
        private System.Windows.Forms.Button btnAddToList;
        private System.Windows.Forms.ListBox lbSavedIDs;
        private System.Windows.Forms.Label lblSavedIDs;
        private System.Windows.Forms.Button btnRemoveID;
        private System.Windows.Forms.Button btnScanAll;
        private System.Windows.Forms.GroupBox gbAutoScan;
        private System.Windows.Forms.RadioButton rbDisabled;
        private System.Windows.Forms.RadioButton rb30m;
        private System.Windows.Forms.RadioButton rb1h;
        private System.Windows.Forms.RadioButton rb6h;
        private System.Windows.Forms.RadioButton rb12h;
        private System.Windows.Forms.RadioButton rb24h;
        private System.Windows.Forms.Label lblNextScan;
        private System.Windows.Forms.Button btnSortByUpdateTime;
        private System.Windows.Forms.Label lblSortStatus;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar1;
    }
}