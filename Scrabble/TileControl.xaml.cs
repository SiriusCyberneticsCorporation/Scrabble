using System;
using Windows.Foundation;
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

			TileLetterTextBlock.Text = Letter;
			LetterValueTextBlock.Text = LetterValue.ToString();
		}

		private void TileRectangle_SizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
		{
			TileRectangle.RadiusX = TileRectangle.ActualWidth * 0.1;
			TileRectangle.RadiusY = TileRectangle.RadiusX;
		}
	}
}
