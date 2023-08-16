using SplatNet.Stream.Api.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SplatNet.Stream.Windows.Controls
{
    public sealed class CheckableTreeItem : INotifyPropertyChanged
    {
		public event PropertyChangedEventHandler PropertyChanged;

		public ObservableCollection<CheckableTreeItem> Items { get; set; } = new();

		public Match Match { get; set; }

		public string Name
		{
			get => this.name;
			set
			{
				if (this.name != value)
				{
					this.name = value;
					this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Name)));
				}
			}
		}

		public bool? IsChecked 
		{
			get => this.isChecked;
			set => this.SetIsChecked(value, true, true);
		}

		private string name;
		private CheckableTreeItem parent;
		private bool? isChecked = false;

		public CheckableTreeItem(Match match)
		{
			this.Match = match;

			this.Name = $"{match.Map}/{match.Mode}";
			this.Initialize();
		}

		public CheckableTreeItem(DateTime dateCategory)
		{
			this.Name = $"{dateCategory:d}";
			this.Initialize();
		}

		private void Initialize()
		{
			this.Items.CollectionChanged += this.Items_CollectionChanged;
		}

		private void SetIsChecked(bool? value, bool updateChildren, bool updateParent)
		{
			if (value == this.isChecked) return;

			this.isChecked = value;

			if (updateChildren && this.isChecked.HasValue)
			{
				foreach (var item in this.Items)
				{
					item.SetIsChecked(this.isChecked, true, false);
				}
			}

			if (updateParent && this.parent != null)
			{
				this.parent.VerifyCheckedState();
			}

			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsChecked)));
		}

		private void VerifyCheckedState()
		{
			bool? state = null;

			for (int i = 0; i < this.Items.Count; i++)
			{
				bool? current = this.Items[i].IsChecked;
				if (i == 0)
				{
					state = current;
				}
				else if (state != current)
				{
					state = null;
					break;
				}
			}

			this.SetIsChecked(state, false, true);
		}

		private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
			{
				foreach (CheckableTreeItem item in e.OldItems)
				{
					item.parent = null;
				}
			}

			if (e.NewItems != null)
			{
				foreach (CheckableTreeItem item in e.NewItems)
				{
					item.parent = this;
				}
			}
		}
	}
}
