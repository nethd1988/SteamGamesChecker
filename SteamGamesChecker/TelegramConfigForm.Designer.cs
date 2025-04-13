namespace SteamGamesChecker
{
    partial class TelegramConfigForm
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
            this.lblBotToken = new System.Windows.Forms.Label();
            this.txtBotToken = new System.Windows.Forms.TextBox();
            this.btnSaveToken = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnRemoveChatId = new System.Windows.Forms.Button();
            this.lstChatIds = new System.Windows.Forms.ListBox();
            this.btnAddChatId = new System.Windows.Forms.Button();
            this.txtChatId = new System.Windows.Forms.TextBox();
            this.lblChatId = new System.Windows.Forms.Label();
            this.btnTestBot = new System.Windows.Forms.Button();
            this.lblTestStatus = new System.Windows.Forms.Label();
            this.chkEnable = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.numNotificationDays = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.btnHelp = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numNotificationDays)).BeginInit();
            this.SuspendLayout();
            // 
            // lblBotToken
            // 
            this.lblBotToken.AutoSize = true;
            this.lblBotToken.Location = new System.Drawing.Point(12, 15);
            this.lblBotToken.Name = "lblBotToken";
            this.lblBotToken.Size = new System.Drawing.Size(59, 13);
            this.lblBotToken.TabIndex = 0;
            this.lblBotToken.Text = "Token Bot:";
            // 
            // txtBotToken
            // 
            this.txtBotToken.Location = new System.Drawing.Point(77, 12);
            this.txtBotToken.Name = "txtBotToken";
            this.txtBotToken.Size = new System.Drawing.Size(296, 20);
            this.txtBotToken.TabIndex = 1;
            // 
            // btnSaveToken
            // 
            this.btnSaveToken.Location = new System.Drawing.Point(379, 10);
            this.btnSaveToken.Name = "btnSaveToken";
            this.btnSaveToken.Size = new System.Drawing.Size(48, 23);
            this.btnSaveToken.TabIndex = 2;
            this.btnSaveToken.Text = "Lưu";
            this.btnSaveToken.UseVisualStyleBackColor = true;
            this.btnSaveToken.Click += new System.EventHandler(this.btnSaveToken_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnRemoveChatId);
            this.groupBox1.Controls.Add(this.lstChatIds);
            this.groupBox1.Controls.Add(this.btnAddChatId);
            this.groupBox1.Controls.Add(this.txtChatId);
            this.groupBox1.Controls.Add(this.lblChatId);
            this.groupBox1.Location = new System.Drawing.Point(12, 83);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(415, 150);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Chat IDs";
            // 
            // lblChatId
            // 
            this.lblChatId.AutoSize = true;
            this.lblChatId.Location = new System.Drawing.Point(17, 22);
            this.lblChatId.Name = "lblChatId";
            this.lblChatId.Size = new System.Drawing.Size(46, 13);
            this.lblChatId.TabIndex = 0;
            this.lblChatId.Text = "Chat ID:";
            // 
            // txtChatId
            // 
            this.txtChatId.Location = new System.Drawing.Point(69, 19);
            this.txtChatId.Name = "txtChatId";
            this.txtChatId.Size = new System.Drawing.Size(246, 20);
            this.txtChatId.TabIndex = 1;
            // 
            // btnAddChatId
            // 
            this.btnAddChatId.Location = new System.Drawing.Point(321, 17);
            this.btnAddChatId.Name = "btnAddChatId";
            this.btnAddChatId.Size = new System.Drawing.Size(75, 23);
            this.btnAddChatId.TabIndex = 2;
            this.btnAddChatId.Text = "Thêm";
            this.btnAddChatId.UseVisualStyleBackColor = true;
            this.btnAddChatId.Click += new System.EventHandler(this.btnAddChatId_Click);
            // 
            // lstChatIds
            // 
            this.lstChatIds.FormattingEnabled = true;
            this.lstChatIds.Location = new System.Drawing.Point(20, 45);
            this.lstChatIds.Name = "lstChatIds";
            this.lstChatIds.Size = new System.Drawing.Size(295, 95);
            this.lstChatIds.TabIndex = 3;
            // 
            // btnRemoveChatId
            // 
            this.btnRemoveChatId.Location = new System.Drawing.Point(321, 45);
            this.btnRemoveChatId.Name = "btnRemoveChatId";
            this.btnRemoveChatId.Size = new System.Drawing.Size(75, 23);
            this.btnRemoveChatId.TabIndex = 4;
            this.btnRemoveChatId.Text = "Xóa";
            this.btnRemoveChatId.UseVisualStyleBackColor = true;
            this.btnRemoveChatId.Click += new System.EventHandler(this.btnRemoveChatId_Click);
            // 
            // btnTestBot
            // 
            this.btnTestBot.Location = new System.Drawing.Point(15, 239);
            this.btnTestBot.Name = "btnTestBot";
            this.btnTestBot.Size = new System.Drawing.Size(105, 23);
            this.btnTestBot.TabIndex = 4;
            this.btnTestBot.Text = "Kiểm tra Bot";
            this.btnTestBot.UseVisualStyleBackColor = true;
            this.btnTestBot.Click += new System.EventHandler(this.btnTestBot_Click);
            // 
            // lblTestStatus
            // 
            this.lblTestStatus.AutoSize = true;
            this.lblTestStatus.Location = new System.Drawing.Point(126, 244);
            this.lblTestStatus.Name = "lblTestStatus";
            this.lblTestStatus.Size = new System.Drawing.Size(0, 13);
            this.lblTestStatus.TabIndex = 5;
            // 
            // chkEnable
            // 
            this.chkEnable.AutoSize = true;
            this.chkEnable.Location = new System.Drawing.Point(15, 46);
            this.chkEnable.Name = "chkEnable";
            this.chkEnable.Size = new System.Drawing.Size(182, 17);
            this.chkEnable.TabIndex = 6;
            this.chkEnable.Text = "Bật thông báo qua Telegram Bot";
            this.chkEnable.UseVisualStyleBackColor = true;
            this.chkEnable.CheckedChanged += new System.EventHandler(this.chkEnable_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(231, 47);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(88, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Thông báo trong:";
            // 
            // numNotificationDays
            // 
            this.numNotificationDays.Location = new System.Drawing.Point(325, 45);
            this.numNotificationDays.Maximum = new decimal(new int[] {
            365,
            0,
            0,
            0});
            this.numNotificationDays.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numNotificationDays.Name = "numNotificationDays";
            this.numNotificationDays.Size = new System.Drawing.Size(48, 20);
            this.numNotificationDays.TabIndex = 8;
            this.numNotificationDays.Value = new decimal(new int[] {
            7,
            0,
            0,
            0});
            this.numNotificationDays.ValueChanged += new System.EventHandler(this.numNotificationDays_ValueChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(379, 47);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(29, 13);
            this.label2.TabIndex = 9;
            this.label2.Text = "ngày";
            // 
            // btnHelp
            // 
            this.btnHelp.Location = new System.Drawing.Point(271, 239);
            this.btnHelp.Name = "btnHelp";
            this.btnHelp.Size = new System.Drawing.Size(75, 23);
            this.btnHelp.TabIndex = 10;
            this.btnHelp.Text = "Hướng dẫn";
            this.btnHelp.UseVisualStyleBackColor = true;
            this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(352, 239);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 11;
            this.btnClose.Text = "Đóng";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // TelegramConfigForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(439, 274);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnHelp);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.numNotificationDays);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.chkEnable);
            this.Controls.Add(this.lblTestStatus);
            this.Controls.Add(this.btnTestBot);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnSaveToken);
            this.Controls.Add(this.txtBotToken);
            this.Controls.Add(this.lblBotToken);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TelegramConfigForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Cấu hình Telegram Bot";
            this.Load += new System.EventHandler(this.TelegramConfigForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numNotificationDays)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblBotToken;
        private System.Windows.Forms.TextBox txtBotToken;
        private System.Windows.Forms.Button btnSaveToken;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnRemoveChatId;
        private System.Windows.Forms.ListBox lstChatIds;
        private System.Windows.Forms.Button btnAddChatId;
        private System.Windows.Forms.TextBox txtChatId;
        private System.Windows.Forms.Label lblChatId;
        private System.Windows.Forms.Button btnTestBot;
        private System.Windows.Forms.Label lblTestStatus;
        private System.Windows.Forms.CheckBox chkEnable;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numNotificationDays;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnHelp;
        private System.Windows.Forms.Button btnClose;
    }
}