namespace ShaderCacheCleaner
{
    partial class ScheduleForm
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
            chkEnableSchedule = new CheckBox();
            lblFrequency = new Label();
            cmbFrequency = new ComboBox();
            lblTime = new Label();
            dtpTime = new DateTimePicker();
            btnApply = new Button();
            btnClose = new Button();
            lblStatus = new Label();
            groupBox1 = new GroupBox();
            lblInfo = new Label();
            groupBox1.SuspendLayout();
            SuspendLayout();
            //
            // chkEnableSchedule
            //
            chkEnableSchedule.AutoSize = true;
            chkEnableSchedule.Location = new Point(12, 12);
            chkEnableSchedule.Name = "chkEnableSchedule";
            chkEnableSchedule.Size = new Size(201, 19);
            chkEnableSchedule.TabIndex = 0;
            chkEnableSchedule.Text = "Enable Automatic Cache Cleaning";
            chkEnableSchedule.UseVisualStyleBackColor = true;
            chkEnableSchedule.CheckedChanged += ChkEnableSchedule_CheckedChanged;
            //
            // lblFrequency
            //
            lblFrequency.AutoSize = true;
            lblFrequency.Location = new Point(15, 30);
            lblFrequency.Name = "lblFrequency";
            lblFrequency.Size = new Size(65, 15);
            lblFrequency.TabIndex = 1;
            lblFrequency.Text = "Frequency:";
            //
            // cmbFrequency
            //
            cmbFrequency.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFrequency.FormattingEnabled = true;
            cmbFrequency.Items.AddRange(new object[] { "DAILY", "WEEKLY", "MONTHLY" });
            cmbFrequency.Location = new Point(15, 48);
            cmbFrequency.Name = "cmbFrequency";
            cmbFrequency.Size = new Size(200, 23);
            cmbFrequency.TabIndex = 2;
            //
            // lblTime
            //
            lblTime.AutoSize = true;
            lblTime.Location = new Point(15, 84);
            lblTime.Name = "lblTime";
            lblTime.Size = new Size(37, 15);
            lblTime.TabIndex = 3;
            lblTime.Text = "Time:";
            //
            // dtpTime
            //
            dtpTime.CustomFormat = "HH:mm";
            dtpTime.Format = DateTimePickerFormat.Custom;
            dtpTime.Location = new Point(15, 102);
            dtpTime.Name = "dtpTime";
            dtpTime.ShowUpDown = true;
            dtpTime.Size = new Size(200, 23);
            dtpTime.TabIndex = 4;
            //
            // btnApply
            //
            btnApply.Location = new Point(15, 141);
            btnApply.Name = "btnApply";
            btnApply.Size = new Size(95, 30);
            btnApply.TabIndex = 5;
            btnApply.Text = "Apply";
            btnApply.UseVisualStyleBackColor = true;
            btnApply.Click += BtnApply_Click;
            //
            // btnClose
            //
            btnClose.Location = new Point(320, 310);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(95, 30);
            btnClose.TabIndex = 6;
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += BtnClose_Click;
            //
            // lblStatus
            //
            lblStatus.Location = new Point(12, 278);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(403, 23);
            lblStatus.TabIndex = 7;
            lblStatus.Text = "Ready";
            //
            // groupBox1
            //
            groupBox1.Controls.Add(lblFrequency);
            groupBox1.Controls.Add(cmbFrequency);
            groupBox1.Controls.Add(btnApply);
            groupBox1.Controls.Add(lblTime);
            groupBox1.Controls.Add(dtpTime);
            groupBox1.Location = new Point(12, 37);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(403, 185);
            groupBox1.TabIndex = 8;
            groupBox1.TabStop = false;
            groupBox1.Text = "Schedule Settings";
            //
            // lblInfo
            //
            lblInfo.Location = new Point(12, 235);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new Size(403, 40);
            lblInfo.TabIndex = 9;
            lblInfo.Text = "Note: Creating scheduled tasks requires administrator privileges. The scheduler will automatically clean all existing caches at the specified time.";
            //
            // ScheduleForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(427, 352);
            Controls.Add(lblInfo);
            Controls.Add(lblStatus);
            Controls.Add(btnClose);
            Controls.Add(chkEnableSchedule);
            Controls.Add(groupBox1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ScheduleForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Schedule Automatic Cleaning";
            Load += ScheduleForm_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private CheckBox chkEnableSchedule;
        private Label lblFrequency;
        private ComboBox cmbFrequency;
        private Label lblTime;
        private DateTimePicker dtpTime;
        private Button btnApply;
        private Button btnClose;
        private Label lblStatus;
        private GroupBox groupBox1;
        private Label lblInfo;
    }
}
