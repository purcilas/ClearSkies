namespace ShaderCacheCleaner;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        listViewCaches = new ListView();
        columnName = new ColumnHeader();
        columnPath = new ColumnHeader();
        columnSize = new ColumnHeader();
        btnScan = new Button();
        btnCleanSelected = new Button();
        btnSchedule = new Button();
        lblTotalSize = new Label();
        lblStatus = new Label();
        progressBar = new ProgressBar();
        btnSelectAll = new Button();
        btnDeselectAll = new Button();
        txtLog = new TextBox();
        lblLog = new Label();
        btnClearLog = new Button();
        btnConfigMsfs = new Button();
        SuspendLayout();
        //
        // listViewCaches
        //
        listViewCaches.CheckBoxes = true;
        listViewCaches.Columns.AddRange(new ColumnHeader[] { columnName, columnPath, columnSize });
        listViewCaches.FullRowSelect = true;
        listViewCaches.GridLines = true;
        listViewCaches.Location = new Point(12, 12);
        listViewCaches.Name = "listViewCaches";
        listViewCaches.Size = new Size(760, 300);
        listViewCaches.TabIndex = 0;
        listViewCaches.UseCompatibleStateImageBehavior = false;
        listViewCaches.View = View.Details;
        //
        // columnName
        //
        columnName.Text = "Cache Name";
        columnName.Width = 250;
        //
        // columnPath
        //
        columnPath.Text = "Location";
        columnPath.Width = 350;
        //
        // columnSize
        //
        columnSize.Text = "Size";
        columnSize.Width = 120;
        //
        // btnScan
        //
        btnScan.Location = new Point(12, 318);
        btnScan.Name = "btnScan";
        btnScan.Size = new Size(120, 35);
        btnScan.TabIndex = 1;
        btnScan.Text = "Scan Caches";
        btnScan.UseVisualStyleBackColor = true;
        btnScan.Click += BtnScan_Click;
        //
        // btnSelectAll
        //
        btnSelectAll.Location = new Point(138, 318);
        btnSelectAll.Name = "btnSelectAll";
        btnSelectAll.Size = new Size(58, 35);
        btnSelectAll.TabIndex = 7;
        btnSelectAll.Text = "All";
        btnSelectAll.UseVisualStyleBackColor = true;
        btnSelectAll.Click += BtnSelectAll_Click;
        //
        // btnDeselectAll
        //
        btnDeselectAll.Location = new Point(196, 318);
        btnDeselectAll.Name = "btnDeselectAll";
        btnDeselectAll.Size = new Size(58, 35);
        btnDeselectAll.TabIndex = 8;
        btnDeselectAll.Text = "None";
        btnDeselectAll.UseVisualStyleBackColor = true;
        btnDeselectAll.Click += BtnDeselectAll_Click;
        //
        // btnCleanSelected
        //
        btnCleanSelected.Location = new Point(260, 318);
        btnCleanSelected.Name = "btnCleanSelected";
        btnCleanSelected.Size = new Size(120, 35);
        btnCleanSelected.TabIndex = 2;
        btnCleanSelected.Text = "Clean Selected";
        btnCleanSelected.UseVisualStyleBackColor = true;
        btnCleanSelected.Click += BtnCleanSelected_Click;
        //
        // btnConfigMsfs
        //
        btnConfigMsfs.Location = new Point(504, 318);
        btnConfigMsfs.Name = "btnConfigMsfs";
        btnConfigMsfs.Size = new Size(142, 35);
        btnConfigMsfs.TabIndex = 12;
        btnConfigMsfs.Text = "MSFS Cache Folder";
        btnConfigMsfs.UseVisualStyleBackColor = true;
        btnConfigMsfs.Click += BtnConfigMsfs_Click;
        //
        // btnSchedule
        //
        btnSchedule.Location = new Point(652, 318);
        btnSchedule.Name = "btnSchedule";
        btnSchedule.Size = new Size(120, 35);
        btnSchedule.TabIndex = 3;
        btnSchedule.Text = "Schedule...";
        btnSchedule.UseVisualStyleBackColor = true;
        btnSchedule.Click += BtnSchedule_Click;
        //
        // lblTotalSize
        //
        lblTotalSize.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblTotalSize.Location = new Point(12, 365);
        lblTotalSize.Name = "lblTotalSize";
        lblTotalSize.Size = new Size(760, 23);
        lblTotalSize.TabIndex = 4;
        lblTotalSize.Text = "Total Cache Size: 0 B";
        //
        // lblStatus
        //
        lblStatus.Location = new Point(12, 395);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(760, 23);
        lblStatus.TabIndex = 5;
        lblStatus.Text = "Ready";
        //
        // progressBar
        //
        progressBar.Location = new Point(12, 421);
        progressBar.Name = "progressBar";
        progressBar.Size = new Size(760, 23);
        progressBar.TabIndex = 6;
        //
        // lblLog
        //
        lblLog.AutoSize = true;
        lblLog.Location = new Point(12, 454);
        lblLog.Name = "lblLog";
        lblLog.Size = new Size(82, 15);
        lblLog.TabIndex = 10;
        lblLog.Text = "Deletion Log:";
        //
        // btnClearLog
        //
        btnClearLog.Location = new Point(697, 449);
        btnClearLog.Name = "btnClearLog";
        btnClearLog.Size = new Size(75, 23);
        btnClearLog.TabIndex = 11;
        btnClearLog.Text = "Clear Log";
        btnClearLog.UseVisualStyleBackColor = true;
        btnClearLog.Click += BtnClearLog_Click;
        //
        // txtLog
        //
        txtLog.BackColor = SystemColors.Window;
        txtLog.Font = new Font("Consolas", 8F);
        txtLog.Location = new Point(12, 475);
        txtLog.Multiline = true;
        txtLog.Name = "txtLog";
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.Size = new Size(760, 150);
        txtLog.TabIndex = 9;
        //
        // Form1
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(784, 637);
        Controls.Add(btnConfigMsfs);
        Controls.Add(btnClearLog);
        Controls.Add(lblLog);
        Controls.Add(txtLog);
        Controls.Add(btnDeselectAll);
        Controls.Add(btnSelectAll);
        Controls.Add(progressBar);
        Controls.Add(lblStatus);
        Controls.Add(lblTotalSize);
        Controls.Add(btnSchedule);
        Controls.Add(btnCleanSelected);
        Controls.Add(btnScan);
        Controls.Add(listViewCaches);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        MaximizeBox = false;
        Name = "Form1";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "MSFS Shader Cache Cleaner";
        Load += Form1_Load;
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private ListView listViewCaches;
    private ColumnHeader columnName;
    private ColumnHeader columnPath;
    private ColumnHeader columnSize;
    private Button btnScan;
    private Button btnCleanSelected;
    private Button btnSchedule;
    private Label lblTotalSize;
    private Label lblStatus;
    private ProgressBar progressBar;
    private Button btnSelectAll;
    private Button btnDeselectAll;
    private TextBox txtLog;
    private Label lblLog;
    private Button btnClearLog;
    private Button btnConfigMsfs;
}
