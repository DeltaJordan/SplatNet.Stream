using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SplatNet.Stream.Api.Models
{
    public class Team
    {
        public Player[] Players { get => this.m_players; }
        public Brush InkColor { get; set; }

        public int TotalKills => this.m_players.Sum(p => p.Kills);
        public int TotalAssists => this.m_players.Sum(p => p.Assists);
        public int TotalDeaths => this.m_players.Sum(p => p.Deaths);
        public int TotalKD => this.m_players.Sum(p => p.KD);
        public int TotalPaint => this.m_players.Sum(p => p.Paint);
        public int TotalSpecials => this.m_players.Sum(p => p.Specials);

		public string Judgement { get; set; }
		public int Score { get; set; }

        private readonly Player[] m_players = new Player[4]
        {
            new Player() { Name = "Player" },
            new Player() { Name = "Player" },
            new Player() { Name = "Player" },
            new Player() { Name = "Player" }
        };
    }
}
