using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SplatNet.Stream.Windows
{
	/// <summary>
	/// Interaction logic for Dashboard.xaml
	/// </summary>
	public partial class Dashboard : Window
	{
		public int Cooldown { get; set; }

		private MainWindow parent;
		private Timer timer;

		public Dashboard(MainWindow parent)
		{
			this.InitializeComponent();

			this.parent = parent;
			this.parent.MatchUpdated += this.Parent_MatchUpdated;

			this.timer = new(this.Cooldown_Updated, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
			this.Loaded += this.MainWindow_Loaded;
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			EventManager.RegisterClassHandler(typeof(Window), PreviewKeyUpEvent, new KeyEventHandler(this.OnGlobalKeyUp));
		}

		private void OnGlobalKeyUp(object sender, KeyEventArgs e)
		{
			if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.F12)
			{
				if (this.btnRefresh.IsEnabled)
				{
					this.Refresh_Click(this, null);
				}
			}
		}

		private void Cooldown_Updated(object _)
		{
			if (this.Cooldown <= 0)
				return;

			Application.Current.Dispatcher.Invoke(() =>
			{
				this.Cooldown--;
				if (this.Cooldown <= 0 )
				{
					this.Cooldown = 0;
					this.btnRefresh.IsEnabled = true;
					this.lblRefresh.Content = "Refresh";
				}
				else
				{
					this.lblRefresh.Content = $"Cooldown {this.Cooldown}s";
				}
			});
		}

		private void Parent_MatchUpdated(object sender, Exception ex)
		{
			if (ex == null)
			{
				this.icoStatus.Foreground = new SolidColorBrush(Colors.Green);
				this.lblStatus.Content = "Successfully retrieved the latest battle.";
			}
			else
			{
				this.icoStatus.Foreground = new SolidColorBrush(Colors.Red);
				this.lblStatus.Content = $"Exception detected! Send this to me Latios ;)\n{ex}";
			}

			if (this.Cooldown <= 0)
				this.btnRefresh.IsEnabled = true;
		}

		private void Refresh_Click(object sender, RoutedEventArgs e)
		{
			this.btnRefresh.IsEnabled = false;
			this.Cooldown = 60;
			this.icoStatus.Foreground = new SolidColorBrush(Colors.Yellow);
			this.lblStatus.Content = "Loading please wait...";
			this.parent.UpdateBattle();
		}

		private void Export_Click(object sender, RoutedEventArgs e)
		{
			new ExportWindow().Show();
		}
	}
}
