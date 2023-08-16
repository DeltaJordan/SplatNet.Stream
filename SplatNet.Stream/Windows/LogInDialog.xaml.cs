using SplatNet.Stream.Api.Core;
using SplatNet.Stream.Extensions;
using SplatNet.Stream.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;

namespace SplatNet.Stream.Windows
{
	public partial class LogInDialog : Window
	{
		public ApiCredentials Credentials { get; set; }

		private readonly string authCodeVerifier;

		public LogInDialog()
		{
			this.InitializeComponent();

			byte[] randomVerifier = new byte[32];
			Random.Shared.NextBytes(randomVerifier);
			string authCodeVerifier = Convert.ToBase64String(randomVerifier).TrimEnd('=').Replace('+', '-').Replace('/', '_');

			string loginUrl = SplatApiAuth.GetLogInUrl(SplatApiShared.A_VERSION, authCodeVerifier);

			this.linkLogin.NavigateUri = new Uri(loginUrl);
			this.authCodeVerifier = authCodeVerifier;
		}

		private void LinkLogin_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
			e.Handled = true;
		}

		private void BtnLogin_Click(object sender, RoutedEventArgs e)
		{
			string auth = this.tbxAuth.Text;
			this.btnLogin.Visibility = Visibility.Hidden;
			SplatApiAuth.LogInAsync(auth, this.authCodeVerifier).ThenDispatch(x =>
			{
				this.Credentials = new ApiCredentials("splatnet");
				this.Credentials.SessionToken = x;

				SplatApiAuth.GetGTokenAsync(SplatApiShared.F_GEN_URL, x, SplatApiShared.A_VERSION).ThenDispatch(x =>
				{
					this.Credentials.GToken = x.gToken;
					this.Credentials.Country = x.country;
					this.Credentials.Language = x.language;
					SplatApiAuth.GetBulletAsync(x.gToken, SplatApiShared.USER_AGENT, x.language, x.country).ThenDispatch(x =>
					{
						this.Credentials.BulletToken = x;
						this.Credentials.Save();
						this.DialogResult = true;
						this.Close();
					}, this.HandleLogInError);
				}, this.HandleLogInError);
			}, this.HandleLogInError);
		}
		
		private void HandleLogInError(Exception ex)
		{
			MessageBox.Show($"Failed to log in. Try logging out and back in.\nEXCEPTION: {ex}");
			this.DialogResult = false;
			this.Close();
		}
	}
}
