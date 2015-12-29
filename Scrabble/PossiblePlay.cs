using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scrabble
{
	public class PossiblePlay
	{
		public int TotalValue;
		public string Word;
		public ExistingPlay Interaction;
		public List<int> XValues = new List<int>();
		public List<int> YValues = new List<int>();
		public List<string> WordsCreated;
		public List<TileControl> TilesToPlay;
	}
}
