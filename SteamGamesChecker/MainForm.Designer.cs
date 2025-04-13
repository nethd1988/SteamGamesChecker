namespace SteamGamesChecker
{
    partial class MainForm
    {
        // ... code không thay đổi ...

        private void InitializeComponent()
        {
            // ... code không thay đổi ...

            // 
            // menuStrip1
            // 
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.telegramMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutMenuItem = new System.Windows.Forms.ToolStripMenuItem();

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
            this.btnConfigTelegram = new System.Windows.Forms.Button();
            this.btnConfigTelegram.Location = new System.Drawing.Point(618, 407);
            this.btnConfigTelegram.Name = "btnConfigTelegram";
            this.btnConfigTelegram.Size = new System.Drawing.Size(130, 23);
            this.btnConfigTelegram.TabIndex = 25;
            this.btnConfigTelegram.Text = "Cấu hình Telegram";
            this.btnConfigTelegram.UseVisualStyleBackColor = true;
            this.btnConfigTelegram.Click += new System.EventHandler(this.btnConfigTelegram_Click);

            // ... code tiếp theo ...

            // Thêm controls vào form
            this.Controls.Add(this.btnConfigTelegram);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;

            // ... code tiếp theo ...

            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
        }

        // Thêm biến thành viên cho menu và button mới
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem telegramMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutMenuItem;
        private System.Windows.Forms.Button btnConfigTelegram;

        // ... code tiếp theo ...
    }
}