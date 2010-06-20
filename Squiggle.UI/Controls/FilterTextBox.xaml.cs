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

namespace Squiggle.UI.Controls
{
    /// <summary>
    /// Interaction logic for FilterTextBox.xaml
    /// </summary>
    public partial class FilterTextBox : UserControl
    {
        public event EventHandler<BuddyFilterEventArs> FilterChanged = delegate { };

        bool waterMarked = true;

        public FilterTextBox()
        {
            InitializeComponent();
            ShowWaterMarked();
        }

        public bool IsFocusedOrNotEmpty
        {
            get { return (bool)GetValue(IsFocusedOrNotEmptyProperty); }
            set { SetValue(IsFocusedOrNotEmptyProperty, value); }
        }

        public static readonly DependencyProperty IsFocusedOrNotEmptyProperty =
            DependencyProperty.Register("IsFocusedOrNotEmpty", typeof(bool), typeof(FilterTextBox), new UIPropertyMetadata(false));

        private void btnFilter_Click(object sender, RoutedEventArgs e)
        {
            txtFilter.Text = String.Empty;
            FilterChanged(this, new BuddyFilterEventArs() { FilterBy = String.Empty });
        }

        private void txtFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateIsFocusedOrNotEmpty();
            if (!waterMarked)
                FilterChanged(this, new BuddyFilterEventArs() { FilterBy = txtFilter.Text });
        }

        void UpdateIsFocusedOrNotEmpty()
        {
            IsFocusedOrNotEmpty = txtFilter.IsFocused || !waterMarked;
        }

        private void txtFilter_GotFocus(object sender, RoutedEventArgs e)
        {
            UpdateIsFocusedOrNotEmpty();

            if (waterMarked)
            {
                txtFilter.Text = String.Empty;
                txtFilter.Foreground = new SolidColorBrush(Colors.Black);
            }
            waterMarked = false;
        }

        private void txtFilter_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateIsFocusedOrNotEmpty();

            if (txtFilter.Text == String.Empty)
                ShowWaterMarked();
        }

        private void ShowWaterMarked()
        {
            waterMarked = true;
            txtFilter.Text = "Search contacts...";
            txtFilter.Foreground = new SolidColorBrush(Colors.LightGray);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            txtFilter.Clear();
            ShowWaterMarked();
        }        
    }

    public class BuddyFilterEventArs : EventArgs
    {
        public string FilterBy { get; set; }
    }
}
