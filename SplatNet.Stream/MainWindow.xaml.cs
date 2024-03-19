using SplatNet.Stream.Api.Core;
using SplatNet.Stream.Api.Models;
using SplatNet.Stream.Extensions;
using SplatNet.Stream.IO;
using SplatNet.Stream.Security;
using SplatNet.Stream.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace SplatNet.Stream
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		public event EventHandler<Exception> MatchUpdated;

		public Match Match {
			get => this.m_match;
			set
			{
				if (this.m_match.Id != value.Id)
				{
					this.m_match = value;
					this.RaisePropertyChanged(nameof(this.Match));
				}
			}
		}

		private Match m_match = new();

		public MainWindow()
		{
			this.InitializeComponent();
			this.DataContext = this;
			this.UseLayoutRounding = true;

			FetchOldIds();
			this.GetLatestBattle();
			new Dashboard(this).Show();
		}

		public void UpdateBattle()
		{
			this.GetLatestBattle();
		}

		private void GetLatestBattle()
		{
			CheckLoginAsync().ThenDispatch(isAuthed =>
			{
				if (isAuthed) 
				{
					SplatApiFetch.FetchJsonAsync().ThenDispatch(match =>
					{
						this.Match = match;
						this.MatchUpdated?.Invoke(this, null);
					}, ex =>
					{
						this.MatchUpdated?.Invoke(this, ex);
					});
				}
				else
				{
					Authenticate();
					this.GetLatestBattle();
				}
			});
		}

		

		private static async Task<bool> CheckLoginAsync()
		{
			SplatApiShared.Credentials = new ApiCredentials("splatnet");
			if (!SplatApiShared.Credentials.IsValid())
			{
				return false;
			}

			return await SplatApiFetch.PrefetchAsync().SafeAsync();
		}

		private static void Authenticate()
		{
			LogInDialog dialog = null;
			while (dialog?.DialogResult != true)
			{
				dialog = new LogInDialog();
				dialog.ShowDialog();
				if (dialog.DialogResult != true)
				{
					MessageBox.Show("You must log in to use this application.");
				}
			}

			SplatApiShared.Credentials = dialog.Credentials;
		}

		private static void FetchOldIds()
		{
			string cache = Directory.CreateDirectory(Path.Combine(IOUtil.AppDirectory, "cache")).FullName;
			SplatApiShared.OldBattles.AddRange(Directory.EnumerateFiles(cache).Select(x => Match.Load(File.ReadAllText(x))));
		}

		private void RaisePropertyChanged(string propertyName)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
