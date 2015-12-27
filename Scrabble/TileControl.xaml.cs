using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Scrabble
{
	public sealed partial class TileControl : UserControl
	{
		public int LetterValue { get; set; }
		public int GridX { get; set; }
		public int GridY { get; set; }
		public string Letter { get; set; }
		public eTileState TileStatus { get; set; }

		public TileControl(string letter, int letterValue)
		{
			this.InitializeComponent();

			Letter = letter;
			LetterValue = letterValue;
			GridX = -1;
			GridY = -1;
			TileStatus = eTileState.InBag;

			string letterFile = string.Format("ms-appx:///Assets/{0}.png", Letter);
			LetterImage.Source = new BitmapImage() { UriSource = new Uri(letterFile, UriKind.Absolute) };
		}
	}
}
