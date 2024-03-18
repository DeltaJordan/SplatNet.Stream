using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatNet.Stream.Api.Models
{
    public sealed class Player
    {
        public string Name { get; set; }
        public string Tag { get; set; }
        public bool IsAlias { get; set; }
		public string WeaponUrl { get; set; } = "https://placehold.co/32";
		public string WeaponName { get; set; }
		public int Kills { get; set; }
        public int Assists { get; set; }
        public int Deaths { get; set; }
        public int KD => this.Kills - this.Deaths;
		public string KillParticipation => this.Team == null ? "" : $"{(this.Kills + this.Assists) / (decimal)this.Team.TotalKills:P}";
        public int Paint { get; set; }
        public int Specials { get; set; }

		public Team Team { get; set; }
    }
}
