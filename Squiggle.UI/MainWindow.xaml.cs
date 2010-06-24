﻿using System;
using System.Collections.Generic;
using System.Windows;
using Squiggle.Chat;
using System.Net;
using System.Linq;
using System.Windows.Threading;
using Squiggle.UI.Settings;
using Messenger;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Windows.Input;
using System.Threading;

namespace Squiggle.UI
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        WindowState lastState;
        ClientViewModel clientViewModel;
        UserActivityMonitor activityMonitor;
        UserStatus lastStatus;
        Dictionary<Buddy, ChatWindow> chatWindows;
        ClientViewModel dummyViewModel;
        NetworkSignout autoSignout;
        ManualResetEvent clientAvailable = new ManualResetEvent(true);

        public static MainWindow Instance { get; private set; }
        public IChatClient ChatClient { get; private set; }

        bool exiting;

        public MainWindow()
        {
           Instance = this;
           InitializeComponent();

           chatWindows = new Dictionary<Buddy, ChatWindow>();

           chatControl.SignIn.CredentialsVerfied += new EventHandler<Squiggle.UI.Controls.LogInEventArgs>(OnCredentialsVerfied);
           chatControl.ContactList.ChatStart += new EventHandler<Squiggle.UI.Controls.ChatStartEventArgs>(OnStartChat);
           chatControl.ContactList.SignOut += new EventHandler(ContactList_SignOut);
           dummyViewModel = new ClientViewModel(new DummyChatClient());
           autoSignout = new NetworkSignout(u=>SignIn(u, false), ()=>SignOut(false));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = dummyViewModel;
            this.StateChanged += new EventHandler(Window_StateChanged);

            var settings = SettingsProvider.Current.Settings;

            chatControl.SignIn.chkAutoSignIn.IsChecked = settings.PersonalSettings.AutoSignMeIn;
            chatControl.SignIn.chkRememberName.IsChecked = settings.PersonalSettings.RememberMe;
            chatControl.SignIn.txtdisplayName.Text = settings.PersonalSettings.DisplayName;

            string name = settings.PersonalSettings.DisplayName;
            if (!String.IsNullOrEmpty(name) && settings.PersonalSettings.AutoSignMeIn)
                SignIn(name, true);
            else
            {
                chatControl.SignIn.txtdisplayName.Text = name;
                chatControl.SignIn.txtdisplayName.SelectAll();
                if (!String.IsNullOrEmpty(name))
                    chatControl.SignIn.chkRememberName.IsChecked = true;
            }
            if (App.RunInBackground)
                this.Hide();
        }

        void OnCredentialsVerfied(object sender, Squiggle.UI.Controls.LogInEventArgs e)
        {
            SignIn(e.UserName, true);
        }

        void chatClient_ChatStarted(object sender, ChatStartedEventArgs e)
        {
            CreateChatWindow(e.Buddy, e.Chat, false);
        }       

        void chatClient_BuddyOnline(object sender, BuddyOnlineEventArgs e)
        {
            if (!e.Discovered && SettingsProvider.Current.Settings.GeneralSettings.ShowPopups)
                TrayPopup.Show("Buddy Online", e.Buddy.DisplayName + " is online", _=> StartChat(e.Buddy));
        }

        void OnStartChat(object sender, Squiggle.UI.Controls.ChatStartEventArgs e)
        {
            StartChat(e.User, e.SendFile, e.File);
        }

        ChatWindow StartChat(Buddy buddy, bool sendFile, string filePath)
        {
            ChatWindow window = StartChat(buddy);
            if (sendFile)
                if (String.IsNullOrEmpty(filePath))
                    window.SendFile();
                else
                    window.SendFile(filePath);

            return window;
        }

        ChatWindow StartChat(Buddy buddy)
        {
            return CreateChatWindow(buddy, buddy.StartChat(), true);
        }

        private void trayIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            ToggleMainWindow();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState != System.Windows.WindowState.Minimized)
                lastState = this.WindowState;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            foreach (var window in chatWindows.Values.ToList())
                window.Close();
        }

        private void StatusMenu_Click(object sender, RoutedEventArgs e)
        {
            var status = (UserStatus)((System.Windows.Controls.MenuItem)e.OriginalSource).DataContext;
            clientViewModel.LoggedInUser.Status = status;
        }

        private void OpenMenu_Click(object sender, RoutedEventArgs e)
        {
            RestoreWindow();
        }

        private void QuiteMenu_Click(object sender, RoutedEventArgs e)
        {
            exiting = true;
            Close();
        }

        void ContactList_SignOut(object sender, EventArgs e)
        {
            SignOut(true);
        }

        private void SignOutMenu_Click(object sender, RoutedEventArgs e)
        {
            SignOut(true);
        }   

        void SignIn(string displayName, bool byUser)
        {
            Dispatcher.Invoke(() =>
            {
                clientAvailable.WaitOne();

                if (ChatClient != null && ChatClient.LoggedIn)
                    return;

                try
                {
                    ChatClient = CreateClient(displayName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                CreateMonitor();
                clientViewModel = new ClientViewModel(ChatClient);
                this.DataContext = clientViewModel;
                chatControl.ChatContext = clientViewModel;

                VisualStateManager.GoToState(chatControl, "OnlineState", true);
                if (byUser)
                    autoSignout.OnSignIn(displayName);
            });
        }
        
        void SignOut(bool byUser)
        {
            Dispatcher.Invoke(() =>
            {
                clientAvailable.Reset();
                if (ChatClient == null || !ChatClient.LoggedIn)
                    return;

                DestroyMonitor();
                ThreadPool.QueueUserWorkItem(_=>
                {
                    ChatClient.Logout();
                    clientAvailable.Set();
                });
                chatControl.ContactList.ChatContext = null;
                clientViewModel = null;
                this.DataContext = dummyViewModel;
                VisualStateManager.GoToState(chatControl, "OfflineState", true);
                chatControl.SignIn.txtdisplayName.Text = SettingsProvider.Current.Settings.PersonalSettings.DisplayName;
                if (byUser)
                    autoSignout.OnSignOut();
            });
        }

        void ToggleMainWindow()
        {
            Dispatcher.Invoke(() =>
            {
                if (this.Visibility == Visibility.Visible)
                    this.Hide();
                else
                    RestoreWindow();
            });
        }

        void CreateMonitor()
        {
            TimeSpan timeout = SettingsProvider.Current.Settings.PersonalSettings.IdleTimeout.Minutes();
            if (activityMonitor != null)
                activityMonitor.Dispose();
            activityMonitor = new UserActivityMonitor(timeout);
            activityMonitor.Idle += (sender, e) =>
            {
                if (ChatClient.LoggedIn)
                {
                    lastStatus = ChatClient.CurrentUser.Status;
                    ChatClient.CurrentUser.Status = UserStatus.Idle;
                }
            };
            activityMonitor.Active += (sender, e) =>
            {
                if (ChatClient.LoggedIn)
                    ChatClient.CurrentUser.Status = lastStatus;
            };
            activityMonitor.Start();
        }

        private void DestroyMonitor()
        {
            if (activityMonitor != null)
            {
                activityMonitor.Dispose();
                activityMonitor = null;
            }
        }        

        IChatClient CreateClient(string displayName)
        {
            var settings = SettingsProvider.Current.Settings;

            int chatPort = settings.ConnectionSettings.ChatPort;
            if (String.IsNullOrEmpty(settings.ConnectionSettings.BindToIP))
                throw new OperationCanceledException("You are not on a network. Please make sure your network card is enabled.");

            IPAddress localIP = IPAddress.Parse(settings.ConnectionSettings.BindToIP);
            TimeSpan keepAliveTimeout = settings.ConnectionSettings.KeepAliveTime.Seconds();
            IPAddress presenceAddress = IPAddress.Parse(settings.ConnectionSettings.PresenceAddress);
            int presencePort = settings.ConnectionSettings.PresencePort;

            var chatEndPoint = new IPEndPoint(localIP, chatPort);
            if (!NetworkUtility.IsEndPointFree(chatEndPoint))
                chatEndPoint.Port = NetworkUtility.GetFreePort();

            var presenceEndPoint = new IPEndPoint(presenceAddress, presencePort);

            ChatClient client = new ChatClient(chatEndPoint, presenceEndPoint, keepAliveTimeout);
            
            var properties = new Dictionary<string,string>();
            properties["MachineName"] = Environment.MachineName;
            client.Login(displayName, settings.PersonalSettings.DisplayMessage, properties);
            client.ChatStarted += new EventHandler<ChatStartedEventArgs>(chatClient_ChatStarted);
            client.BuddyUpdated += new EventHandler<BuddyEventArgs>(client_BuddyUpdated);
            client.BuddyOnline += new EventHandler<BuddyOnlineEventArgs>(chatClient_BuddyOnline);
            client.BuddyOffline += new EventHandler<BuddyEventArgs>(client_BuddyOffline);
            return client;
        }

        void client_BuddyOffline(object sender, BuddyEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                chatControl.ContactList.Refresh();
            });
        }

        void client_BuddyUpdated(object sender, BuddyEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                chatControl.ContactList.Refresh();
            });
        }

        public void RestoreWindow()
        {
            this.Show();
            this.Activate();
            this.WindowState = lastState;
        }

        ChatWindow CreateChatWindow(Buddy buddy, IChat session, bool focused)
        {
            ChatWindow window;

            if (!chatWindows.TryGetValue(buddy, out window))
            {
                window = new ChatWindow(buddy, session);
                window.Closed += (sender, e) => chatWindows.Remove(buddy);
                chatWindows.Add(buddy, window);
            }
            else
                window.ChatSession = session;
            window.Title = buddy.DisplayName;
            
            window.WindowState = focused ? WindowState.Normal : WindowState.Minimized;
            window.Show();
            if (focused)
                window.Activate();

            return window;
        }        

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!exiting && SettingsProvider.Current.Settings.GeneralSettings.HideToSystemTray)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void CloseMenu_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OpenReceivedFilesMenu_Click(object sender, RoutedEventArgs e)
        {
            SquiggleUtility.OpenDownloadsFolder();
        }

        private void SettingsMenu_Click(object sender, RoutedEventArgs e)
        {
            SquiggleUtility.ShowSettingsDialog(this);
        }

        private void AboutMenu_Click(object sender, RoutedEventArgs e)
        {
            SquiggleUtility.ShowAboutDialog();
        }

        private void SendFileMenu_Click(object sender, RoutedEventArgs e)
        {
            IEnumerable<Buddy> selectedBuddies = SquiggleUtility.ShowSendInstantMessageDialog(clientViewModel);
            foreach (Buddy buddy in selectedBuddies)
                StartChat(buddy, true, null);
        }

        private void SendMessageMenu_Click(object sender, RoutedEventArgs e)
        {
            IEnumerable<Buddy> selectedBuddies = SquiggleUtility.ShowSendInstantMessageDialog(clientViewModel);
            foreach (Buddy buddy in selectedBuddies)
                StartChat(buddy);
        }
    }
}
