﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using Squiggle.Chat;
using Squiggle.UI.Settings;
using System.Reflection;
using Squiggle.UI.ViewModel;

namespace Squiggle.UI.Helpers
{
    class SquiggleUtility
    {
        public static string GetInstallDirectory()
        {
            string location = Assembly.GetExecutingAssembly().Location;
            location = Path.GetDirectoryName(location);
            return location;
        }

        public static IEnumerable<UserStatus> GetChangableStatuses()
        {
            var statuses = from status in Enum.GetValues(typeof(UserStatus)).Cast<UserStatus>()
                           where status != UserStatus.Idle
                           select status;
            return statuses;
        }

        public static void OpenDownloadsFolder()
        {
            string downloadsFolder = SettingsProvider.Current.Settings.GeneralSettings.DownloadsFolder;
            try
            {
                if (Shell.CreateDirectoryIfNotExists(downloadsFolder))
                    Process.Start(downloadsFolder);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        public static void ShowFontDialog()
        {
            using (var dialog = new System.Windows.Forms.FontDialog())
            {
                var settings = SettingsProvider.Current.Settings.PersonalSettings;
                dialog.Font = new System.Drawing.Font(settings.FontName, settings.FontSize, settings.FontStyle);
                dialog.ShowColor = true;
                dialog.Color = settings.FontColor;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    float fontSize = dialog.Font.Size;
                    System.Drawing.Color fontColor = dialog.Color;

                    settings.FontColor = fontColor;
                    settings.FontName = dialog.Font.Name;
                    settings.FontSize = Convert.ToInt32(fontSize);
                    settings.FontStyle = dialog.Font.Style;
                    SettingsProvider.Current.Save();
                }
            }
        }

        public static void ShowSettingsDialog(Window owner)
        {
            Buddy user = null;
            if (MainWindow.Instance.chatControl.ContactList.ChatContext != null && MainWindow.Instance.chatControl.ContactList.ChatContext.IsLoggedIn)
                user = MainWindow.Instance.chatControl.ContactList.ChatContext.LoggedInUser;
            var settings = new SettingsWindow(user);
            settings.Owner = owner;
            if (settings.ShowDialog() == true)
            {
                if (String.IsNullOrEmpty(MainWindow.Instance.chatControl.SignIn.DisplayName) &&
                    MainWindow.Instance.chatControl.SignIn.Visibility == Visibility.Visible)
                {
                    MainWindow.Instance.chatControl.SignIn.DisplayName = SettingsProvider.Current.Settings.PersonalSettings.DisplayName;
                }
            }
        }

        public static Buddy SelectContact(string title, Window owner, Predicate<Buddy> exclusionFilter = null)
        {
            return SelectContacts(title, owner, exclusionFilter, false).FirstOrDefault();
        }

        public static IEnumerable<Buddy> SelectContacts(string title, Window owner, Predicate<Buddy> exclusionFilter = null, bool multiple = true)
        {
            var clientViewModel = (ClientViewModel)MainWindow.Instance.DataContext;
            var selectContactDialog = new ContactsSelectWindow(clientViewModel, false);
            selectContactDialog.ExcludeCriterea = exclusionFilter;
            selectContactDialog.AllowMultiSelect = multiple;
            selectContactDialog.Owner = owner;
            selectContactDialog.Title = title;
            if (selectContactDialog.ShowDialog() == true)
                return selectContactDialog.SelectedContacts;

            return Enumerable.Empty<Buddy>();
        }


        public static void ShowAboutDialog(Window owner)
        {
            var about = new About();
            about.Owner = owner;
            about.ShowDialog();
        }

        public static void ShakeWindow(ChatWindow window)
        {            
            if (window.WindowState != System.Windows.WindowState.Minimized)
            {
                var rand = new Random();
                double top = window.Top;
                double left = window.Left;
                const int power = 10;

                for (int i = 0; i < 30; i++)
                {
                    window.Top = top + rand.Next(-power, power);
                    window.Left = left + rand.Next(-power, power);
                    Thread.Sleep(10);                    
                }

                window.Top = top;
                window.Left = left;
            }
        }

        public static FontSetting GetFontSettings()
        {
            var settings = SettingsProvider.Current.Settings.PersonalSettings;
            var fontSettings = new FontSetting(settings.FontColor, settings.FontName, settings.FontSize, settings.FontStyle);

            return fontSettings;
        }
    }
}
