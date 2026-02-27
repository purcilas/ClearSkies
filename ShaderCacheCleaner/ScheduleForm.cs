using System.Diagnostics;

namespace ShaderCacheCleaner
{
    public partial class ScheduleForm : Form
    {
        private const string TASK_NAME = "ShaderCacheCleanup";

        public ScheduleForm()
        {
            InitializeComponent();
        }

        private void ScheduleForm_Load(object sender, EventArgs e)
        {
            LoadCurrentSchedule();
        }

        private void LoadCurrentSchedule()
        {
            // Check if task exists
            var taskExists = CheckTaskExists();

            if (taskExists)
            {
                chkEnableSchedule.Checked = true;
                lblStatus.Text = "Scheduled task is active";
                lblStatus.ForeColor = Color.Green;
            }
            else
            {
                chkEnableSchedule.Checked = false;
                lblStatus.Text = "No scheduled task found";
                lblStatus.ForeColor = Color.Gray;
            }

            UpdateControlStates();
        }

        private bool CheckTaskExists()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/Query /TN \"{TASK_NAME}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };

                using var process = Process.Start(psi);
                process?.WaitForExit();
                return process?.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private void UpdateControlStates()
        {
            var enabled = chkEnableSchedule.Checked;
            cmbFrequency.Enabled = enabled;
            dtpTime.Enabled = enabled;
            btnApply.Enabled = enabled;
        }

        private void ChkEnableSchedule_CheckedChanged(object sender, EventArgs e)
        {
            UpdateControlStates();

            if (!chkEnableSchedule.Checked && CheckTaskExists())
            {
                var result = MessageBox.Show(
                    "Do you want to remove the existing scheduled task?",
                    "Remove Schedule",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    RemoveScheduledTask();
                }
            }
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            if (!IsAdministrator())
            {
                MessageBox.Show(
                    "Creating scheduled tasks requires administrator privileges.\n\n" +
                    "Please run this application as Administrator.",
                    "Administrator Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            CreateScheduledTask();
        }

        private bool IsAdministrator()
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }

        private void CreateScheduledTask()
        {
            try
            {
                // Remove existing task if it exists
                RemoveScheduledTask();

                var exePath = Application.ExecutablePath;
                var frequency = cmbFrequency.SelectedItem?.ToString() ?? "DAILY";
                var time = dtpTime.Value.ToString("HH:mm");

                // Create the task using schtasks
                var arguments = $"/Create /TN \"{TASK_NAME}\" /TR \"\\\"{exePath}\\\" /clean\" " +
                               $"/SC {frequency} /ST {time} /F";

                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    Verb = "runas"
                };

                using var process = Process.Start(psi);
                var output = process?.StandardOutput.ReadToEnd();
                var error = process?.StandardError.ReadToEnd();
                process?.WaitForExit();

                if (process?.ExitCode == 0)
                {
                    lblStatus.Text = "Scheduled task created successfully";
                    lblStatus.ForeColor = Color.Green;
                    MessageBox.Show(
                        $"Scheduled task created successfully!\n\n" +
                        $"The cache cleaner will run {frequency.ToLower()} at {time}.",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    lblStatus.Text = "Failed to create scheduled task";
                    lblStatus.ForeColor = Color.Red;
                    MessageBox.Show(
                        $"Failed to create scheduled task.\n\n{error}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error creating scheduled task";
                lblStatus.ForeColor = Color.Red;
                MessageBox.Show(
                    $"Error: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void RemoveScheduledTask()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/Delete /TN \"{TASK_NAME}\" /F",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                process?.WaitForExit();

                if (process?.ExitCode == 0)
                {
                    lblStatus.Text = "Scheduled task removed";
                    lblStatus.ForeColor = Color.Gray;
                }
            }
            catch
            {
                // Silently fail if task doesn't exist
            }
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
