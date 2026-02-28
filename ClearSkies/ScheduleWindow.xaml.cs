using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClearSkies;

public partial class ScheduleWindow : Window
{
    private const string TASK_NAME = "ClearSkiesCleanup";

    public ScheduleWindow()
    {
        InitializeComponent();
        PopulateDayOfMonth();
    }

    private void PopulateDayOfMonth()
    {
        for (int i = 1; i <= 31; i++)
        {
            var item = new ComboBoxItem { Content = i.ToString() };
            if (i == 1) item.IsSelected = true;
            cmbDayOfMonth.Items.Add(item);
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        LoadCurrentSchedule();
    }

    private void LoadCurrentSchedule()
    {
        var taskExists = CheckTaskExists();

        if (taskExists)
        {
            chkEnableSchedule.IsChecked = true;
            lblStatus.Text = "Scheduled task is active";
            lblStatus.Foreground = (SolidColorBrush)FindResource("AccentBrush");
        }
        else
        {
            chkEnableSchedule.IsChecked = false;
            lblStatus.Text = "No scheduled task found";
            lblStatus.Foreground = (SolidColorBrush)FindResource("SubtextBrush");
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
        var enabled = chkEnableSchedule.IsChecked == true;
        pnlSettings.IsEnabled = enabled;
    }

    private void CmbFrequency_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (pnlDayOfWeek == null || pnlDayOfMonth == null) return;

        var selected = (cmbFrequency.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

        pnlDayOfWeek.Visibility = selected == "Weekly" ? Visibility.Visible : Visibility.Collapsed;
        pnlDayOfMonth.Visibility = selected == "Monthly" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ChkEnableSchedule_Changed(object sender, RoutedEventArgs e)
    {
        UpdateControlStates();

        if (chkEnableSchedule.IsChecked == false && CheckTaskExists())
        {
            var result = MessageBox.Show(
                "Do you want to remove the existing scheduled task?",
                "Remove Schedule",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                RemoveScheduledTask();
            }
        }
    }

    private void BtnApply_Click(object sender, RoutedEventArgs e)
    {
        if (!IsAdministrator())
        {
            MessageBox.Show(
                "Creating scheduled tasks requires administrator privileges.\n\n" +
                "Please run this application as Administrator.",
                "Administrator Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
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
            RemoveScheduledTask();

            var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
            var selectedItem = cmbFrequency.SelectedItem as ComboBoxItem;
            var frequency = selectedItem?.Content?.ToString() ?? "Daily";
            var schtasksFrequency = frequency.ToUpper();

            // Parse time from text boxes (12-hour format)
            if (!int.TryParse(txtHour.Text, out int hour) || hour < 1 || hour > 12)
                hour = 3;
            if (!int.TryParse(txtMinute.Text, out int minute) || minute < 0 || minute > 59)
                minute = 0;
            var isPm = (cmbAmPm.SelectedItem as ComboBoxItem)?.Content?.ToString() == "PM";
            // Convert to 24-hour for schtasks
            int hour24 = hour;
            if (isPm && hour != 12) hour24 += 12;
            if (!isPm && hour == 12) hour24 = 0;
            var time = $"{hour24:D2}:{minute:D2}";

            var arguments = $"/Create /TN \"{TASK_NAME}\" /TR \"\\\"{exePath}\\\" /clean\" " +
                           $"/SC {schtasksFrequency} /ST {time} /F";

            // Add day-of-week for weekly
            if (frequency == "Weekly")
            {
                var dayItem = cmbDayOfWeek.SelectedItem as ComboBoxItem;
                var dayName = dayItem?.Content?.ToString() ?? "Monday";
                // schtasks uses short day names: MON, TUE, WED, THU, FRI, SAT, SUN
                var dayShort = dayName.ToUpper()[..3];
                arguments += $" /D {dayShort}";
            }

            // Add day-of-month for monthly
            if (frequency == "Monthly")
            {
                var dayItem = cmbDayOfMonth.SelectedItem as ComboBoxItem;
                var day = dayItem?.Content?.ToString() ?? "1";
                arguments += $" /D {day}";
            }

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
                lblStatus.Foreground = (SolidColorBrush)FindResource("AccentBrush");

                var summary = frequency switch
                {
                    "Weekly" => $"every {(cmbDayOfWeek.SelectedItem as ComboBoxItem)?.Content}",
                    "Monthly" => $"on day {(cmbDayOfMonth.SelectedItem as ComboBoxItem)?.Content} of each month",
                    _ => "daily"
                };

                var amPm = isPm ? "PM" : "AM";
                MessageBox.Show(
                    $"Scheduled task created successfully!\n\n" +
                    $"The cache cleaner will run {summary} at {hour}:{minute:D2} {amPm}.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                lblStatus.Text = "Failed to create scheduled task";
                lblStatus.Foreground = (SolidColorBrush)FindResource("DangerBrush");
                MessageBox.Show(
                    $"Failed to create scheduled task.\n\n{error}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = "Error creating scheduled task";
            lblStatus.Foreground = (SolidColorBrush)FindResource("DangerBrush");
            MessageBox.Show(
                $"Error: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
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
                lblStatus.Foreground = (SolidColorBrush)FindResource("SubtextBrush");
            }
        }
        catch
        {
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        DragMove();
    }
}
