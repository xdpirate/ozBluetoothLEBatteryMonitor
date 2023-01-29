using BluetoothLEBatteryMonitor.Service;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Windows.Forms;
using Windows.UI.Xaml.Automation.Peers;

namespace BluetoothLEBatteryMonitor
{
    public partial class Settings : Form
    {
        private DeviceManager deviceManager = null;
        private Info infoForm = null;
        private bool UserClose = false;
        private bool UserShow = false;
        private bool lowBatteryNotificationDone = false;

        public Settings()
        {
            InitializeComponent();

                //First of all create entry for settings
            Registry.CurrentUser.CreateSubKey("SOFTWARE\\BluetoothLEBatteryMonitor");

                //Reload settings
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
            if (rk != null)
                checkBoxStartup.Checked = rk.GetValue("BluetoothLEBatteryMonitor") != null;

            rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\BluetoothLEBatteryMonitor", false);

            numericUpDownRefreshPeriod.Value = (int)rk.GetValue("IntervalMin", 5);
            checkBoxNotification.Checked = ((int)rk.GetValue("NotificationEnabled", 1)) != 0;
            checkBoxScanForEver.Checked = ((int)rk.GetValue("AutomaticDetectionEnabled", 0)) != 0;

                //Instantiate everything
            deviceManager = new DeviceManager(new DeviceNotification(this));
            deviceManager.scan(checkBoxScanForEver.Checked);

            infoForm = new Info(deviceManager);

            IconTimer.Interval = ((int)numericUpDownRefreshPeriod.Value) * 60 * 1000;
            IconTimer.Start();

            UpdateIcon();
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(UserShow ? value : false);
            UserShow = false;
        }

        private void DeviceListForm_Load(object sender, EventArgs e)
        {

        }

        private void DeviceListForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                if (!UserClose)
                {
                    e.Cancel = true;
                    Hide();
                }
            }
        }

        private void IconTimer_Tick(object sender, EventArgs e)
        {
            UpdateIcon();
        }

        public void UpdateIcon()
        {
            ConcurrentDictionary<string, DeviceBLE> deviceDict = deviceManager.getDeviceList();

                //Request to update battery level
            foreach (DeviceBLE device in deviceDict.Values)
                device.UpdateBatteryLevel();

            int theLowestBattery = 100;
            string theLowestBatteryName = "";
            string theBalloonText = "";

            foreach (DeviceBLE device in deviceDict.Values)
            {
                int         theBatteryLevel = device.GetBatteryLevel();
                string      theName = device.GetName();

                if ((theBatteryLevel >= 0) && (theBatteryLevel < theLowestBattery))
                {
                    theLowestBattery = theBatteryLevel;
                    theLowestBatteryName = theName;
                }

                theBalloonText += String.Format("{0}: {1}%\n", theName, theBatteryLevel);
            }

            if (theLowestBattery >= 90)
            {
                NotifyIcon.Icon = BluetoothLEBatteryMonitor.Properties.Resources.Icon_Battery_100;
            }
            else if (theLowestBattery >= 70)
            {
                NotifyIcon.Icon = BluetoothLEBatteryMonitor.Properties.Resources.Icon_Battery_80;
            }
            else if (theLowestBattery >= 50)
            {
                NotifyIcon.Icon = BluetoothLEBatteryMonitor.Properties.Resources.Icon_Battery_60;
            }
            else if (theLowestBattery >= 30)
            {
                NotifyIcon.Icon = BluetoothLEBatteryMonitor.Properties.Resources.Icon_Battery_40;
            }
            else if(theLowestBattery > 0)
            {
                NotifyIcon.Icon = BluetoothLEBatteryMonitor.Properties.Resources.Icon_Battery_20;
            }

            if (theLowestBattery <= 20)
            {
                if (!lowBatteryNotificationDone)
                    Notify(String.Format("Battery LOW on '{0}' ({1}%) !", theLowestBatteryName, theLowestBattery), ToolTipIcon.Warning);

                lowBatteryNotificationDone = true;
            }
            else
            {
                lowBatteryNotificationDone = false;
            }

            NotifyIcon.Text = theBalloonText.Substring(0, Math.Min(theBalloonText.Length, 64));
        }


        public void Notify(string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            if (checkBoxNotification.Checked)
                NotifyIcon.ShowBalloonTip(300, "BluetoothLE Battery Monitor", message, icon);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            infoForm.Close();

            deviceManager.stopScan();

                //Because of an issue, we have to show the setting form before closing it
            UserShow = true;
            Show();

            UserClose = true;
            Close();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UserShow = true;
            Show();
        }

        private void NotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            infoForm.Show();
        }

        private void numericUpDownRefreshPeriod_ValueChanged(object sender, EventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\BluetoothLEBatteryMonitor", true);
            rk.SetValue("IntervalMin", numericUpDownRefreshPeriod.Value, RegistryValueKind.DWord);

            IconTimer.Stop();
            IconTimer.Interval = (int)(numericUpDownRefreshPeriod.Value * 60 * 1000);
            IconTimer.Start();
        }
        private void checkBoxStartup_CheckedChanged(object sender, EventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (checkBoxStartup.Checked)
                rk.SetValue("BluetoothLEBatteryMonitor", Application.ExecutablePath);
            else
                rk.DeleteValue("BluetoothLEBatteryMonitor", false);
        }

        private void checkBoxScanForEver_CheckedChanged(object sender, EventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\BluetoothLEBatteryMonitor", true);
            rk.SetValue("AutomaticDetectionEnabled", checkBoxScanForEver.Checked ? 1 : 0);
        }

        private void checkBoxNotification_CheckedChanged(object sender, EventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\BluetoothLEBatteryMonitor", true);
            rk.SetValue("NotificationEnabled", checkBoxNotification.Checked ? 1 : 0);
        }
    }

    /* --------------------------------------------------------------------- */

    class DeviceNotification : IDeviceNotification
    {
        private Settings form;
        public DeviceNotification(Settings form)
        {
            this.form = form;
        }

        public void OnNewDevice(DeviceBLE aDevice)
        {
            //this.form.Notify("New device detected: " + aDevice.GetName() + " (Battery: " + aDevice.GetBatteryLevel() + "%)");
            this.form.UpdateIcon();
        }

    }
}
