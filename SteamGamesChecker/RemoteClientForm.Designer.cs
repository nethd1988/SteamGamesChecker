namespace SteamGamesChecker
{
    partial class RemoteClientForm
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
            this.lstClients = new System.Windows.Forms.ListBox();
            this.lblClients = new System.Windows.Forms.Label();
            this.groupBoxClients = new System.Windows.Forms.GroupBox();
            this.btnDeleteClient = new System.Windows.Forms.Button();
            this.btnEditClient = new System.Windows.Forms.Button();
            this.btnAddClient = new System.Windows.Forms.Button();
            this.groupBoxCommand = new System.Windows.Forms.GroupBox();
            this.lblReason = new System.Windows.Forms.Label();
            this.txtReason = new System.Windows.Forms.TextBox();
            this.lblAppId = new System.Windows.Forms.Label();
            this.txtAppId = new System.Windows.Forms.TextBox();
            this.btnSendCommand = new System.Windows.Forms.Button();
            this.btnTestConnection = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnClose = new System.Windows.Forms.Button();
            this.groupBoxClients.SuspendLayout();
            this.groupBoxCommand.SuspendLayout();
            this.SuspendLayout();
            // 
            // lstClients
            // 
            this.lstClients.FormattingEnabled = true;
            this.lstClients.Location = new System.Drawing.Point(15, 45);
            this.lstClients.Name = "lstClients";
            this.lstClients.Size = new System.Drawing.Size(358, 134);
            this.lstClients.TabIndex = 0;
            this.lstClients.SelectedIndexChanged += new System.EventHandler(this.lstClients_SelectedIndexChanged);
            // 
            // lblClients
            // 
            this.lblClients.AutoSize = true;
            this.lblClients.Location = new System.Drawing.Point(12, 29);
            this.lblClients.Name = "lblClients";
            this.lblClients.Size = new System.Drawing.Size(141, 13);
            this.lblClients.TabIndex = 1;
            this.lblClients.Text = "Danh sách client quản lý:";
            // 
            // groupBoxClients
            // 
            this.groupBoxClients.Controls.Add(this.btnDeleteClient);
            this.groupBoxClients.Controls.Add(this.btnEditClient);
            this.groupBoxClients.Controls.Add(this.btnAddClient);
            this.groupBoxClients.Controls.Add(this.lstClients);
            this.groupBoxClients.Controls.Add(this.lblClients);
            this.groupBoxClients.Location = new System.Drawing.Point(12, 12);
            this.groupBoxClients.Name = "groupBoxClients";
            this.groupBoxClients.Size = new System.Drawing.Size(460, 200);
            this.groupBoxClients.TabIndex = 2;
            this.groupBoxClients.TabStop = false;
            this.groupBoxClients.Text = "Quản lý client";
            // 
            // btnDeleteClient
            // 
            this.btnDeleteClient.Enabled = false;
            this.btnDeleteClient.Location = new System.Drawing.Point(379, 116);
            this.btnDeleteClient.Name = "btnDeleteClient";
            this.btnDeleteClient.Size = new System.Drawing.Size(75, 23);
            this.btnDeleteClient.TabIndex = 4;
            this.btnDeleteClient.Text = "Xóa";
            this.btnDeleteClient.UseVisualStyleBackColor = true;
            this.btnDeleteClient.Click += new System.EventHandler(this.btnDeleteClient_Click);
            // 
            // btnEditClient
            // 
            this.btnEditClient.Enabled = false;
            this.btnEditClient.Location = new System.Drawing.Point(379, 80);
            this.btnEditClient.Name = "btnEditClient";
            this.btnEditClient.Size = new System.Drawing.Size(75, 23);
            this.btnEditClient.TabIndex = 3;
            this.btnEditClient.Text = "Sửa";
            this.btnEditClient.UseVisualStyleBackColor = true;
            this.btnEditClient.Click += new System.EventHandler(this.btnEditClient_Click);
            // 
            // btnAddClient
            // 
            this.btnAddClient.Location = new System.Drawing.Point(379, 45);
            this.btnAddClient.Name = "btnAddClient";
            this.btnAddClient.Size = new System.Drawing.Size(75, 23);
            this.btnAddClient.TabIndex = 2;
            this.btnAddClient.Text = "Thêm";
            this.btnAddClient.UseVisualStyleBackColor = true;
            this.btnAddClient.Click += new System.EventHandler(this.btnAddClient_Click);
            // 
            // groupBoxCommand
            // 
            this.groupBoxCommand.Controls.Add(this.lblReason);
            this.groupBoxCommand.Controls.Add(this.txtReason);
            this.groupBoxCommand.Controls.Add(this.lblAppId);
            this.groupBoxCommand.Controls.Add(this.txtAppId);
            this.groupBoxCommand.Controls.Add(this.btnSendCommand);
            this.groupBoxCommand.Controls.Add(this.btnTestConnection);
            this.groupBoxCommand.Location = new System.Drawing.Point(12, 218);
            this.groupBoxCommand.Name = "groupBoxCommand";
            this.groupBoxCommand.Size = new System.Drawing.Size(460, 163);
            this.groupBoxCommand.TabIndex = 3;
            this.groupBoxCommand.TabStop = false;
            this.groupBoxCommand.Text = "Gửi lệnh điều khiển";
            // 
            // lblReason
            // 
            this.lblReason.AutoSize = true;
            this.lblReason.Location = new System.Drawing.Point(15, 84);
            this.lblReason.Name = "lblReason";
            this.lblReason.Size = new System.Drawing.Size(45, 13);
            this.lblReason.TabIndex = 5;
            this.lblReason.Text = "Lý do:";
            // 
            // txtReason
            // 
            this.txtReason.Location = new System.Drawing.Point(84, 81);
            this.txtReason.Multiline = true;
            this.txtReason.Name = "txtReason";
            this.txtReason.Size = new System.Drawing.Size(289, 64);
            this.txtReason.TabIndex = 4;
            this.txtReason.Text = "Yêu cầu cập nhật từ SteamGamesChecker";
            // 
            // lblAppId
            // 
            this.lblAppId.AutoSize = true;
            this.lblAppId.Location = new System.Drawing.Point(15, 51);
            this.lblAppId.Name = "lblAppId";
            this.lblAppId.Size = new System.Drawing.Size(49, 13);
            this.lblAppId.TabIndex = 3;
            this.lblAppId.Text = "App ID:";
            // 
            // txtAppId
            // 
            this.txtAppId.Location = new System.Drawing.Point(84, 48);
            this.txtAppId.Name = "txtAppId";
            this.txtAppId.Size = new System.Drawing.Size(100, 20);
            this.txtAppId.TabIndex = 2;
            this.txtAppId.TextChanged += new System.EventHandler(this.txtAppId_TextChanged);
            // 
            // btnSendCommand
            // 
            this.btnSendCommand.Enabled = false;
            this.btnSendCommand.Location = new System.Drawing.Point(379, 79);
            this.btnSendCommand.Name = "btnSendCommand";
            this.btnSendCommand.Size = new System.Drawing.Size(75, 66);
            this.btnSendCommand.TabIndex = 1;
            this.btnSendCommand.Text = "Gửi Lệnh Cập Nhật";
            this.btnSendCommand.UseVisualStyleBackColor = true;
            this.btnSendCommand.Click += new System.EventHandler(this.btnSendCommand_Click);
            // 
            // btnTestConnection
            // 
            this.btnTestConnection.Enabled = false;
            this.btnTestConnection.Location = new System.Drawing.Point(190, 46);
            this.btnTestConnection.Name = "btnTestConnection";
            this.btnTestConnection.Size = new System.Drawing.Size(183, 23);
            this.btnTestConnection.TabIndex = 0;
            this.btnTestConnection.Text = "Kiểm tra kết nối";
            this.btnTestConnection.UseVisualStyleBackColor = true;
            this.btnTestConnection.Click += new System.EventHandler(this.btnTestConnection_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(12, 394);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(67, 13);
            this.lblStatus.TabIndex = 4;
            this.lblStatus.Text = "Trạng thái:";
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(391, 389);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 5;
            this.btnClose.Text = "Đóng";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // RemoteClientForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 426);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.groupBoxCommand);
            this.Controls.Add(this.groupBoxClients);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RemoteClientForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Quản Lý Client Từ Xa";
            this.Load += new System.EventHandler(this.RemoteClientForm_Load);
            this.groupBoxClients.ResumeLayout(false);
            this.groupBoxClients.PerformLayout();
            this.groupBoxCommand.ResumeLayout(false);
            this.groupBoxCommand.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ListBox lstClients;
        private System.Windows.Forms.Label lblClients;
        private System.Windows.Forms.GroupBox groupBoxClients;
        private System.Windows.Forms.Button btnDeleteClient;
        private System.Windows.Forms.Button btnEditClient;
        private System.Windows.Forms.Button btnAddClient;
        private System.Windows.Forms.GroupBox groupBoxCommand;
        private System.Windows.Forms.Button btnSendCommand;
        private System.Windows.Forms.Button btnTestConnection;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label lblAppId;
        private System.Windows.Forms.TextBox txtAppId;
        private System.Windows.Forms.Label lblReason;
        private System.Windows.Forms.TextBox txtReason;
    }
}