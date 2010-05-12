﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Squiggle.Chat;

namespace Messenger
{
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : Window
    {
        IChatSession chatSession;
        public ChatWindow()
        {
            InitializeComponent();

            
        }

        void chatSession_MessageReceived(object sender, Squiggle.Chat.Services.Chat.Host.MessageReceivedEventArgs e)
        {
            WriteMessage(e.User, e.Message);
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            WriteMessage("Me", txtMessage.Text);
        }

        private void WriteMessage(string user, string message)
        {
            var title = new Bold(new Run(user+": "));
            var text = new Run(message);
            sentMessages.Inlines.Add(title);
            sentMessages.Inlines.Add(text);
            sentMessages.Inlines.Add(new Run("\r\n"));
            scrollViewer.ScrollToBottom();
            txtMessage.Text = String.Empty;
            txtMessage.Focus();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            chatSession = this.DataContext as IChatSession;
            chatSession.MessageReceived += new EventHandler<Squiggle.Chat.Services.Chat.Host.MessageReceivedEventArgs>(chatSession_MessageReceived);
        }
    }
}
