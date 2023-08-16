using CsvHelper;
using Microsoft.Win32;
using SplatNet.Stream.Api.Core;
using SplatNet.Stream.Api.Models;
using SplatNet.Stream.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;

namespace SplatNet.Stream.Windows
{
    /// <summary>
    /// Interaction logic for ExportWindow.xaml
    /// </summary>
    public partial class ExportWindow : Window
    {
        public ExportWindow()
        {
			this.InitializeComponent();

			this.LoadMatches();
		}

		private void LoadMatches()
		{
			List<DateTime> days = SplatApiShared.OldBattles.Select(x => x.Time.Date).Distinct().OrderByDescending(x => x).ToList();
			foreach (DateTime day in days)
			{
				CheckableTreeItem item = new(day);
				this.trvExport.Items.Add(item);
				foreach (Match match in SplatApiShared.OldBattles.OrderByDescending(x => x.Time).Where(x => x.Time.Date == day))
				{
					item.Items.Add(new CheckableTreeItem(match));
				}
			}
		}

		private void Export_Clicked(object sender, RoutedEventArgs e)
		{
			SaveFileDialog ofd = new();
			ofd.Filter = "CSV File|*.csv";
			ofd.FileName = "export.csv";
			ofd.ShowDialog();

			if (string.IsNullOrWhiteSpace(ofd.FileName))
			{
				MessageBox.Show("Invalid path.");
				return;
			}

			List<Match> selectedMatches = new();
			foreach(var parent in this.trvExport.Items)
			{
				if (parent is not CheckableTreeItem checkableParent) continue;
				foreach (var child in checkableParent.Items)
				{
					if (child.IsChecked == true)
					{
						selectedMatches.Add(child.Match);
					}
				}
			}

			string result;
			using(StringWriter stringWriter = new())
			using(CsvWriter csvWriter = new(stringWriter, CultureInfo.CurrentCulture))
			{
				csvWriter.WriteField("");
				csvWriter.WriteField("Player");
				csvWriter.WriteField("Weapon");
				csvWriter.WriteField("Kills");
				csvWriter.WriteField("Assists");
				csvWriter.WriteField("Deaths");
				csvWriter.WriteField("+/-");
				csvWriter.WriteField("Paint");
				csvWriter.WriteField("Specials");
				csvWriter.WriteField("Match Time");
				csvWriter.NextRecord();

				foreach(var match in selectedMatches) 
				{
					csvWriter.WriteField("Our Team");
					csvWriter.WriteField($"{match.Map}/{match.Mode}");
					csvWriter.WriteField("");
					csvWriter.WriteField(match.MyTeam.TotalKills);
					csvWriter.WriteField(match.MyTeam.TotalAssists);
					csvWriter.WriteField(match.MyTeam.TotalDeaths);
					csvWriter.WriteField(match.MyTeam.TotalKD);
					csvWriter.WriteField(match.MyTeam.TotalPaint);
					csvWriter.WriteField(match.MyTeam.TotalSpecials);
					csvWriter.WriteField($"|{match.Duration:mm\\:ss}|");
					csvWriter.NextRecord();

					int i = 0;
					foreach (var player in match.MyTeam.Players)
					{
						if (i == 0)
						{
							csvWriter.WriteField(match.MyTeam.Judgement);
						}
						else if (i == 1)
						{
							csvWriter.WriteField(match.MyTeam.Score);
						}
						else
						{
							csvWriter.WriteField("");
						}

						csvWriter.WriteField(player.Name);
						csvWriter.WriteField(player.WeaponName);
						csvWriter.WriteField(player.Kills);
						csvWriter.WriteField(player.Assists);
						csvWriter.WriteField(player.Deaths);
						csvWriter.WriteField(player.KD);
						csvWriter.WriteField(player.Paint);
						csvWriter.WriteField(player.Specials);
						csvWriter.NextRecord();

						i++;
					}

					csvWriter.WriteField("Enemy Team");
					csvWriter.WriteField("");
					csvWriter.WriteField("");
					csvWriter.WriteField(match.EnemyTeam.TotalKills);
					csvWriter.WriteField(match.EnemyTeam.TotalAssists);
					csvWriter.WriteField(match.EnemyTeam.TotalDeaths);
					csvWriter.WriteField(match.EnemyTeam.TotalKD);
					csvWriter.WriteField(match.EnemyTeam.TotalPaint);
					csvWriter.WriteField(match.EnemyTeam.TotalSpecials);
					csvWriter.NextRecord();

					i = 0;
					foreach (var player in match.EnemyTeam.Players)
					{
						if (i == 0)
						{
							csvWriter.WriteField(match.EnemyTeam.Judgement);
						}
						else if (i == 1)
						{
							csvWriter.WriteField(match.EnemyTeam.Score);
						}
						else
						{
							csvWriter.WriteField("");
						}

						csvWriter.WriteField(player.Name);
						csvWriter.WriteField(player.WeaponName);
						csvWriter.WriteField(player.Kills);
						csvWriter.WriteField(player.Assists);
						csvWriter.WriteField(player.Deaths);
						csvWriter.WriteField(player.KD);
						csvWriter.WriteField(player.Paint);
						csvWriter.WriteField(player.Specials);
						csvWriter.NextRecord();

						i++;
					}
				}

				result = stringWriter.ToString();
			}

			File.WriteAllText(ofd.FileName, result);
		}
	}
}
