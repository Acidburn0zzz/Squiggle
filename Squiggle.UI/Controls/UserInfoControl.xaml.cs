﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Squiggle.Chat;

namespace Squiggle.UI.Controls
{
    
    
    /// <summary>
    /// Interaction logic for BuddyList.xaml
    /// </summary>
    public partial class UserInfoControl : UserControl
    {
        public event EventHandler<ChatStartEventArgs> ChatStart = delegate { };

        public static DependencyProperty ChatContextProperty = DependencyProperty.Register("ChatContext", typeof(ClientViewModel), typeof(UserInfoControl), new PropertyMetadata(null));
        public ClientViewModel ChatContext
        {
            get { return GetValue(ChatContextProperty) as ClientViewModel; }
            set 
            {
                SetValue(ChatContextProperty, value);
                //displayMessageBox.SelfUser = value.LoggedInUser;
            }
        } 

        public UserInfoControl()
        {
            InitializeComponent();            
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Buddy buddy = ((Border)sender).Tag as Buddy;
            if(buddy.Status != UserStatus.Offline)
                ChatStart(this, new ChatStartEventArgs() { User=buddy });
        }

        private void ComboBoxItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ShowSettingsWindow();

            e.Handled = true;
        }

        private void ShowSettingsWindow()
        {
            SettingsWindow settings = new SettingsWindow(ChatContext.LoggedInUser);
            settings.ShowDialog();
        }

        private void ComboBoxItem_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ShowSettingsWindow();
                e.Handled = true;
            }
        }

    }

    public class ChatStartEventArgs : EventArgs
    {
        public Buddy User { get; set; }
    }
}
