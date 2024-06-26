﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SplatNet.Stream.Windows.Controls
{
	/// <summary>
	/// Interaction logic for CustomWindowHeader.xaml
	/// </summary>
	public partial class CustomWindowHeader : UserControl
	{
		public bool ShouldCloseExit
		{
			get => (bool)this.GetValue(ShouldClosingExitProperty);
			set => this.SetValue(ShouldClosingExitProperty, value);
		}

		public static readonly DependencyProperty ShouldClosingExitProperty =
			DependencyProperty.Register
			(
				"ShouldCloseExit",
				typeof (bool),
				typeof (CustomWindowHeader),
				new PropertyMetadata (true)
			);

		public CustomWindowHeader()
		{
			this.InitializeComponent();
		}

		private void Minimize_Click(object sender, RoutedEventArgs e)
		{
			Window window = Window.GetWindow(this);
			if (window != null)
			{
				window.WindowState = WindowState.Minimized;
			}
		}

		private void Close_Click(object sender, RoutedEventArgs e)
		{
			if (this.ShouldCloseExit)
			{
				Environment.Exit(0);
			}
			else
			{
				Window window = Window.GetWindow(this);
				window?.Close();
			}
		}
	}
}
