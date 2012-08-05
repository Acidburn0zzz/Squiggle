﻿using System.Globalization;
using System.Reflection;
using System.Windows;
using Squiggle.UI.Helpers;
using Squiggle.UI.StickyWindow;
using Squiggle.Utilities;
using Squiggle.Utilities.Application;
using Squiggle.UI.Components;

namespace Squiggle.UI.Windows
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : StickyWindowBase
    {
        public About()
        {
            InitializeComponent();            

            txtVersion.Text = "Version " + AppInfo.Version.ToString();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Link_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Shell.OpenUrl(e.Uri.ToString());
        }
    }
}
