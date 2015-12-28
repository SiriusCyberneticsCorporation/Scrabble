using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Scrabble
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class GamePage : Page
	{
		private bool m_restartLastGame = false;
		private bool m_firstWord = false;
		private bool m_computersWordFound = false;
		private bool[] m_panelSpaceFilled = new bool[7];
		private bool[,] m_boardSpaceFilled = new bool[15, 15];
		private double m_tileSize = 15;
		//private Gaddag m_gaddag = new Gaddag();
		private Random m_randomiser = null;
		private DateTime m_messageDisplayTime = DateTime.Now;
		private DateTime m_gameStartTime = DateTime.Now;
		private eTurnState m_turnState = eTurnState.Unknown;
		private TileControl m_previousImage = null;
		private List<string> m_allWordsPlayed = new List<string>();
		private IList<string> m_words = new List<string>();
		private DispatcherTimer m_gameTimer = new DispatcherTimer();
		private TileControl[,] m_boardTiles = new TileControl[15, 15];
		private List<TileControl> m_letterBag = new List<TileControl>();
		private List<TileControl> m_currentWordTiles = new List<TileControl>();
		private List<TileControl> m_playedTiles = new List<TileControl>();
		private List<TileControl> m_panelTiles = new List<TileControl>();
		private List<TileControl> m_computersTiles = new List<TileControl>();

		private Dictionary<string, List<string>> m_wordLookup = new Dictionary<string, List<string>>();

		#region Scrabble Configuration

		private class ScrabbleLetterConfig
		{
			public int NumberOf;
			public string Letter;
			public int LetterValue;
		}

		private static string BLANK = " ";

		private static List<ScrabbleLetterConfig> s_scrabbleLetters = new List<ScrabbleLetterConfig>()
		{
			new ScrabbleLetterConfig() {NumberOf = 9, Letter = "A", LetterValue = 1},
			new ScrabbleLetterConfig() {NumberOf = 2, Letter = "B", LetterValue = 3},
			new ScrabbleLetterConfig() {NumberOf = 2, Letter = "C", LetterValue = 3},
			new ScrabbleLetterConfig() {NumberOf = 4, Letter = "D", LetterValue = 2},
			new ScrabbleLetterConfig() {NumberOf = 12, Letter = "E", LetterValue = 1},
			new ScrabbleLetterConfig() {NumberOf = 2, Letter = "F", LetterValue = 4},
			new ScrabbleLetterConfig() {NumberOf = 3, Letter = "G", LetterValue = 2},
			new ScrabbleLetterConfig() {NumberOf = 2, Letter = "H", LetterValue = 4},
			new ScrabbleLetterConfig() {NumberOf = 9, Letter = "I", LetterValue = 1},
			new ScrabbleLetterConfig() {NumberOf = 1, Letter = "J", LetterValue = 8},
			new ScrabbleLetterConfig() {NumberOf = 1, Letter = "K", LetterValue = 5},
			new ScrabbleLetterConfig() {NumberOf = 4, Letter = "L", LetterValue = 1},
			new ScrabbleLetterConfig() {NumberOf = 2, Letter = "M", LetterValue = 3},
			new ScrabbleLetterConfig() {NumberOf = 6, Letter = "N", LetterValue = 1},
			new ScrabbleLetterConfig() {NumberOf = 8, Letter = "O", LetterValue = 1},
			new ScrabbleLetterConfig() {NumberOf = 2, Letter = "P", LetterValue = 3},
			new ScrabbleLetterConfig() {NumberOf = 1, Letter = "Q", LetterValue = 10},
			new ScrabbleLetterConfig() {NumberOf = 6, Letter = "R", LetterValue = 1},
			new ScrabbleLetterConfig() {NumberOf = 4, Letter = "S", LetterValue = 1},
			new ScrabbleLetterConfig() {NumberOf = 6, Letter = "T", LetterValue = 1},
			new ScrabbleLetterConfig() {NumberOf = 4, Letter = "U", LetterValue = 1},
			new ScrabbleLetterConfig() {NumberOf = 2, Letter = "V", LetterValue = 4},
			new ScrabbleLetterConfig() {NumberOf = 2, Letter = "W", LetterValue = 4},
			new ScrabbleLetterConfig() {NumberOf = 1, Letter = "X", LetterValue = 8},
			new ScrabbleLetterConfig() {NumberOf = 2, Letter = "Y", LetterValue = 4},
			new ScrabbleLetterConfig() {NumberOf = 1, Letter = "Z", LetterValue = 10}/*,
			new ScrabbleLetterConfig() {NumberOf = 2, Letter = BLANK, LetterValue = 0}*/
		};

		#endregion Scrabble Configuration
		
		public GamePage()
		{
			this.InitializeComponent();

			m_randomiser = new Random((int)DateTime.Now.TimeOfDay.TotalSeconds);

			m_gameTimer.Tick += GameTimer_Tick;
			m_gameTimer.Interval = new TimeSpan(0, 0, 1);

			ReadWords();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			bool.TryParse(e.Parameter as string, out m_restartLastGame);
		}

		private void GamePageRoot_Loaded(object sender, RoutedEventArgs e)
		{
			if (m_restartLastGame)
			{
			}
			else
			{
				StartNewGame();
			}
		}

		private void GamePageRoot_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			ResizeTiles();
		}

		private void ResizeTiles()
		{
			m_tileSize = ScrabbleBoard.ActualWidth / 15;
//			m_tileSize = ScrabbleBoard.ActualHeight / 16;

			foreach (TileControl tile in m_playedTiles)
			{
				PlaceTileOnBoard(tile, tile.GridX, tile.GridY);
			}
			foreach (TileControl tile in m_currentWordTiles)
			{
				PlaceTileOnBoard(tile, tile.GridX, tile.GridY);
			}

			foreach (TileControl tile in m_panelTiles)
			{
				PlaceTileOnPanel(tile, tile.GridX);
			}
		}

		#region Letter Dragging Functionality

		void DragLetter_ManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
		{
			if (m_turnState != eTurnState.PlayersTurn)
			{
				e.Handled = true;
			}
		}

		void DragLetter_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			TileControl draggedItem = (TileControl)sender;

			GeneralTransform imageTrasform = draggedItem.TransformToVisual(this);
			Point imagePosition = imageTrasform.TransformPoint(new Point());
			Point cursorPosition = imageTrasform.TransformPoint(e.Position);
			Rect imageRect = imageTrasform.TransformBounds(new Rect(0, 0, draggedItem.ActualWidth, draggedItem.ActualHeight));

			GeneralTransform boardTransform = ScrabbleBoard.TransformToVisual(this);
			Rect boardRect = boardTransform.TransformBounds(new Rect(0, 0, ScrabbleBoard.ActualWidth, ScrabbleBoard.ActualHeight));
			Rect letterPanelRect = boardTransform.TransformBounds(new Rect(m_tileSize * 4, ScrabbleBoard.ActualHeight - m_tileSize, m_tileSize * 7, m_tileSize));

			bool droppedOnBoard = boardRect.Contains(cursorPosition) || boardRect.Contains(imagePosition);
			bool droppedOnLetterPanel = letterPanelRect.Contains(cursorPosition) || letterPanelRect.Contains(imagePosition);

			if (droppedOnLetterPanel)
			{
				int gridX = 0;
				double desiredX = 0;
				double desiredY = 0;
				double placementX = 0;

				if (!e.IsInertial)
				{
					placementX = cursorPosition.X;
				}
				else
				{
					placementX = imagePosition.X + (m_tileSize / 2);
				}

				desiredX = placementX - ((placementX - letterPanelRect.X) % m_tileSize);
				desiredY = letterPanelRect.Y;
				gridX = Math.Max(0, Math.Min(6, (int)((1 + desiredX - letterPanelRect.X) / m_tileSize)));

				PlaceTileOnPanel(draggedItem, gridX);
			}
			else if (droppedOnBoard)
			{
				int gridX = 0;
				int gridY = 0;
				double desiredX = 0;
				double desiredY = 0;
				double placementX = 0;
				double placementY = 0;
				TranslateTransform tileRenderTransform = draggedItem.RenderTransform as TranslateTransform;

				if (!e.IsInertial)
				{
					placementX = cursorPosition.X;
					placementY = cursorPosition.Y;
				}
				else
				{
					placementX = imagePosition.X + (m_tileSize / 2);
					placementY = imagePosition.Y + (m_tileSize / 2);
				}

				desiredX = placementX - ((placementX - boardRect.X) % m_tileSize);
				desiredY = placementY - ((placementY - boardRect.Y) % m_tileSize);
				gridX = Math.Max(0, Math.Min(14, (int)((1 + desiredX - boardRect.X) / m_tileSize)));
				gridY = Math.Max(0, Math.Min(14, (int)((1 + desiredY - boardRect.Y) / m_tileSize)));

				foreach (TileControl tile in m_playedTiles)
				{
					if (tile != draggedItem && tile.GridX == gridX && tile.GridY == gridY)
					{
						FindNearestBoardSpace(placementX - boardRect.X, placementY - boardRect.Y, ref gridX, ref gridY);
						break;
					}
				}
				foreach (TileControl tile in m_currentWordTiles)
				{
					if (tile != draggedItem && tile.GridX == gridX && tile.GridY == gridY)
					{
						FindNearestBoardSpace(placementX - boardRect.X, placementY - boardRect.Y, ref gridX, ref gridY);
						break;
					}
				}

				PlaceTileOnBoard(draggedItem, gridX, gridY);
			}
			else
			{
				PlaceTileOnPanel(draggedItem, 0);
			}

			TileMoved();
		}

		private void PlaceTileOnPanel(TileControl draggedItem, int gridX)
		{
			TranslateTransform tileRenderTransform = draggedItem.RenderTransform as TranslateTransform;

			foreach (TileControl tile in m_panelTiles)
			{
				// If a tile is dropped on an existing tile...
				if (tile != draggedItem && tile.GridX == gridX)
				{
					// If moving tiles that are already on the panel then shuffle up or down as appropriate
					if (draggedItem.GridY == 16)
					{
						// If the tile is being moved right then shuffle all preceeding tiles right
						if (draggedItem.GridX < tile.GridX)
						{
							foreach (TileControl tileToMove in m_panelTiles)
							{
								if (tileToMove.GridX > draggedItem.GridX && tileToMove.GridX <= tile.GridX)
								{
									tileToMove.GridX--;
									TranslateTransform movingTileRenderTransform = tileToMove.RenderTransform as TranslateTransform;
									movingTileRenderTransform.X -= m_tileSize;
								}
							}
						}
						// If the tile is being moved left then shuffle all preceeding tiles right
						else if (draggedItem.GridX > tile.GridX)
						{
							foreach (TileControl tileToMove in m_panelTiles)
							{
								if (tileToMove.GridX < draggedItem.GridX && tileToMove.GridX >= tile.GridX)
								{
									tileToMove.GridX++;
									TranslateTransform movingTileRenderTransform = tileToMove.RenderTransform as TranslateTransform;
									movingTileRenderTransform.X += m_tileSize;
								}
							}
						}
					}
					else
					{
						int spacesToTheLeft = gridX;
						int spacesToTheRight = 6 - gridX;
						int gapToLeftAt = 0;
						int gapToRightAt = 6;
						bool[] existingTiles = new bool[7];

						foreach (TileControl tileToMove in m_panelTiles)
						{
							existingTiles[tileToMove.GridX] = true;
							if (tileToMove.GridX < gridX)
							{
								spacesToTheLeft--;
							}
							else if (tileToMove.GridX > gridX)
							{
								spacesToTheRight--;
							}
						}
						for (int i = gridX; i >= 0; i--)
						{
							if (!existingTiles[i])
							{
								gapToLeftAt = i;
								break;
							}
						}
						for (int i = gridX; i < 7; i++)
						{
							if (!existingTiles[i])
							{
								gapToRightAt = i;
								break;
							}
						}
						// If there is no space on the left or simple more space on the right then shuffle right.
						if (spacesToTheLeft == 0 || spacesToTheRight >= spacesToTheLeft)
						{
							foreach (TileControl tileToMove in m_panelTiles)
							{
								if (tileToMove.GridX >= gridX && tileToMove.GridX < gapToRightAt)
								{
									tileToMove.GridX++;
									TranslateTransform movingTileRenderTransform = tileToMove.RenderTransform as TranslateTransform;
									movingTileRenderTransform.X += m_tileSize;
								}
							}
						}
						// Otherwise shuffle left.
						else if (spacesToTheRight == 0 || spacesToTheRight < spacesToTheLeft)
						{
							foreach (TileControl tileToMove in m_panelTiles)
							{
								if (tileToMove.GridX <= gridX && tileToMove.GridX > gapToLeftAt)
								{
									tileToMove.GridX--;
									TranslateTransform movingTileRenderTransform = tileToMove.RenderTransform as TranslateTransform;
									movingTileRenderTransform.X -= m_tileSize;
								}
							}
						}
					}
					break;
				}
			}

			GeneralTransform boardTransform = ScrabbleBoard.TransformToVisual(this);
			Rect letterPanelRect = boardTransform.TransformBounds(new Rect(m_tileSize * 4, ScrabbleBoard.ActualHeight - m_tileSize, m_tileSize * 7, m_tileSize));
			Point positionOfBoard = boardTransform.TransformPoint(new Point(0, 0));

			tileRenderTransform.X = positionOfBoard.X + (m_tileSize * (gridX + 4));
			tileRenderTransform.Y = positionOfBoard.Y + (m_tileSize * 15) + 1;

			if (draggedItem.GridY != 16 && draggedItem.GridX >= 0 && draggedItem.GridY >= 0)
			{
				m_boardSpaceFilled[draggedItem.GridX, draggedItem.GridY] = false;
			}

			draggedItem.Width = m_tileSize;
			draggedItem.Height = m_tileSize;
			draggedItem.GridX = gridX;
			draggedItem.GridY = 16;
			draggedItem.Visibility = Windows.UI.Xaml.Visibility.Visible;

			if (draggedItem.TileStatus == eTileState.ComposingNewWord)
			{
				draggedItem.TileStatus = eTileState.OnPlayerPanel;
				m_currentWordTiles.Remove(draggedItem);
				m_panelTiles.Add(draggedItem);
			}
		}


		private void PlaceTileOnBoard(TileControl draggedItem, int gridX, int gridY)
		{
			if(ScrabbleBoard.ActualWidth < 10 || ScrabbleBoard.ActualHeight < 10)
			{
				return;
			}
			TranslateTransform tileRenderTransform = draggedItem.RenderTransform as TranslateTransform;

			GeneralTransform boardTransform = ScrabbleBoard.TransformToVisual(this);
			Rect boardRect = boardTransform.TransformBounds(new Rect(0, 0, ScrabbleBoard.ActualWidth, ScrabbleBoard.ActualHeight));
			Point positionOfBoard = boardTransform.TransformPoint(new Point(0, 0));

			tileRenderTransform.X = positionOfBoard.X + (m_tileSize * gridX);
			tileRenderTransform.Y = positionOfBoard.Y + (m_tileSize * gridY);

			if (draggedItem.GridY != 16 && draggedItem.GridX >= 0 && draggedItem.GridY >= 0)
			{
				m_boardSpaceFilled[draggedItem.GridX, draggedItem.GridY] = false;
			}

			m_boardSpaceFilled[gridX, gridY] = true;
			draggedItem.Width = m_tileSize;
			draggedItem.Height = m_tileSize;
			draggedItem.GridX = gridX;
			draggedItem.GridY = gridY;
			draggedItem.Visibility = Windows.UI.Xaml.Visibility.Visible;

			if (draggedItem.TileStatus == eTileState.OnPlayerPanel)
			{
				draggedItem.TileStatus = eTileState.ComposingNewWord;
				m_panelTiles.Remove(draggedItem);
				m_currentWordTiles.Add(draggedItem);
			}
		}

		private void FindNearestBoardSpace(double placementX, double placementY, ref int gridX, ref int gridY)
		{
			bool left = (placementX % m_tileSize) < (m_tileSize / 2);
			bool up = (placementY % m_tileSize) < (m_tileSize / 2);

			// Move up one if possible.
			if (up && gridY > 0 && !m_boardSpaceFilled[gridX, gridY - 1])
			{
				gridY--;
			}
			// Move down one if possible.
			else if (!up && gridY < 14 && !m_boardSpaceFilled[gridX, gridY + 1])
			{
				gridY++;
			}
			// Move left one if possible.
			else if (left && gridX > 0 && !m_boardSpaceFilled[gridX - 1, gridY])
			{
				gridX--;
			}
			// Move right one if possible.
			else if (!left && gridX < 14 && !m_boardSpaceFilled[gridX + 1, gridY])
			{
				gridX++;
			}
			// Move up and left if possible.
			else if (left && up && gridX > 0 && gridY > 0 && !m_boardSpaceFilled[gridX - 1, gridY - 1])
			{
				gridX--;
				gridY--;
			}
			// Move down and left if possible.
			else if (left && !up && gridX > 0 && gridY < 14 && !m_boardSpaceFilled[gridX - 1, gridY + 1])
			{
				gridX--;
				gridY++;
			}
			// Move up and right if possible.
			else if (!left && up && gridX < 14 && gridY > 0 && !m_boardSpaceFilled[gridX + 1, gridY - 1])
			{
				gridX++;
				gridY--;
			}
			// Move down and right if possible.
			else if (!left && !up && gridX < 14 && gridY < 14 && !m_boardSpaceFilled[gridX + 1, gridY + 1])
			{
				gridX++;
				gridY++;
			}
			// If no single space moves are possible then try to find any free space.
			else
			{
				bool spaceFound = false;
				if (up && gridY > 1)
				{
					for (int row = gridY; row >= 0; row--)
					{
						if (!m_boardSpaceFilled[gridX, row])
						{
							spaceFound = true;
							gridY = row;
							break;
						}
					}
				}
				if (!spaceFound && !up && gridY < 13)
				{
					for (int row = gridY; row < 14; row++)
					{
						if (!m_boardSpaceFilled[gridX, row])
						{
							spaceFound = true;
							gridY = row;
							break;
						}
					}
				}
				if (!spaceFound && left && gridX > 1)
				{
					for (int column = gridX; column >= 0; column--)
					{
						if (!m_boardSpaceFilled[column, gridY])
						{
							spaceFound = true;
							gridX = column;
							break;
						}
					}
				}
				if (!spaceFound && !left && gridX < 13)
				{
					for (int column = gridX; column < 14; column++)
					{
						if (!m_boardSpaceFilled[column, gridY])
						{
							spaceFound = true;
							gridX = column;
							break;
						}
					}
				}
			}
		}


		private void FindNearestPanelSpace(double placementX, ref int gridX, ref double desiredX)
		{
			bool left = (placementX % m_tileSize) < (m_tileSize / 2);

			// Move left one if possible.
			if (left && gridX > 0 && !m_panelSpaceFilled[gridX - 1])
			{
				gridX--;
				desiredX -= m_tileSize;
			}
			// Move right one if possible.
			else if (!left && gridX < 6 && !m_panelSpaceFilled[gridX + 1])
			{
				gridX++;
				desiredX += m_tileSize;
			}
			// If no single space moves are possible then try to find any free space.
			else
			{
				bool spaceFound = false;

				if (left && gridX > 1)
				{
					for (int column = gridX; column >= 0; column--)
					{
						if (!m_panelSpaceFilled[column])
						{
							spaceFound = true;
							desiredX -= m_tileSize * (gridX - column);
							gridX = column;
							break;
						}
					}
				}
				if (!spaceFound && !left && gridX < 5)
				{
					for (int column = gridX; column < 6; column++)
					{
						if (!m_panelSpaceFilled[column])
						{
							spaceFound = true;
							desiredX += m_tileSize * (column - gridX);
							gridX = column;
							break;
						}
					}
				}
			}
		}

		private void DragLetter_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			if (!e.IsInertial)
			{
				TileControl dragableItem = sender as TileControl;
				TranslateTransform tileRenderTransform = dragableItem.RenderTransform as TranslateTransform;

				tileRenderTransform.X += e.Delta.Translation.X;
				tileRenderTransform.Y += e.Delta.Translation.Y;

				if (m_previousImage != null)
				{
					Canvas.SetZIndex(m_previousImage, 0);
				}
				m_previousImage = dragableItem;
				Canvas.SetZIndex(dragableItem, 1);
			}
			else
			{
				e.Complete();
			}
		}

		#endregion Letter Dragging Functionality

		private void ScrabbleBoard_Loaded(object sender, RoutedEventArgs e)
		{

		}

		private void ScrabbleBoard_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			ResizeTiles();
		}

		private void ShuffleLettersButton_Click(object sender, RoutedEventArgs e)
		{
			List<int> positions = new List<int>() { 0, 1, 2, 3, 4, 5, 6 };
			GeneralTransform boardTransform = ScrabbleBoard.TransformToVisual(this);
			Point positionOfBoard = boardTransform.TransformPoint(new Point(0, 0));

			foreach (TileControl tile in m_panelTiles)
			{
				int selector = m_randomiser.Next(positions.Count);

				PlaceTileOnPanel(tile, positions[selector]);

				positions.RemoveAt(selector);
			}
		}

		private void SwapLettersButton_Click(object sender, RoutedEventArgs e)
		{
			for (int i = 0; i < m_panelTiles.Count; i++)
			{
				m_panelTiles[i].TileStatus = eTileState.InBag;
				m_letterBag.Add(m_panelTiles[i]);
			}

			m_panelTiles.Clear();
			for (int i = 0; i < 7; i++)
			{
				int nextLetter = m_randomiser.Next(m_letterBag.Count - 1);

				TileControl tile = m_letterBag[nextLetter];

				tile.TileStatus = eTileState.OnPlayerPanel;
				m_panelTiles.Add(tile);

				m_letterBag.RemoveAt(nextLetter);
			}
		}

		private void RecallLettersButton_Click(object sender, RoutedEventArgs e)
		{

		}

		private void PlayButton_Click(object sender, RoutedEventArgs e)
		{
			if (m_currentWordTiles.Count == 0)
			{
				MessageTextBox.Text = "You have not placed any letters!";
				MessageTextBox.Visibility = Windows.UI.Xaml.Visibility.Visible;
				m_messageDisplayTime = DateTime.Now;
			}
			else
			{
				bool allWordsOk = true;
				List<string> wordsPlayed = new List<string>(); // GetPlayedWords(true);

				if (m_currentWordTiles.Count > 1)
				{
					int lastX = -1;
					int lastY = -1;
					bool wordHorizontal = false;
					bool wordVertical = false;

					foreach (TileControl tile in m_currentWordTiles)
					{
						if (lastX >= 0 && tile.GridX != lastX)
						{
							wordHorizontal = true;
						}
						if (lastY >= 0 && tile.GridY != lastY)
						{
							wordVertical = true;
						}
						lastX = tile.GridX;
						lastY = tile.GridY;
					}

					if (wordHorizontal == wordVertical)
					{
						MessageTextBox.Text = "Invalid Tile placement!";
						MessageTextBox.Visibility = Windows.UI.Xaml.Visibility.Visible;
						m_messageDisplayTime = DateTime.Now;
					}
					else if (wordHorizontal)
					{
						wordsPlayed = GetPlayedWords(true, m_currentWordTiles);
					}
					else
					{
						wordsPlayed = GetPlayedWords(false, m_currentWordTiles);
					}
				}
				else
				{
					bool horizontal = true;
					TileControl tile = m_currentWordTiles[0];

					if ((tile.GridY - 1 > 0 && m_boardTiles[tile.GridX, tile.GridY - 1] != null) ||
						(tile.GridY + 1 < 15 && m_boardTiles[tile.GridX, tile.GridY + 1] != null))
					{
						horizontal = false;
					}						

					wordsPlayed = GetPlayedWords(horizontal, m_currentWordTiles);

					List<TileControl> playedTiles = new List<TileControl>();

					// Add the only tile played.
					playedTiles.Add(m_currentWordTiles[0]);

					GetSideWordsForWordPlayedHorizontal(playedTiles, ref wordsPlayed);
					GetSideWordsForWordPlayedVertical(playedTiles, ref wordsPlayed);
				}

				foreach (string playedWord in wordsPlayed)
				{
					if (playedWord.Length > 0)
					{
						if (!m_words.Contains(playedWord))
						{
							MessageTextBox.Text = "My dictionary does not contain the word '" + playedWord + "'.";
							MessageTextBox.Visibility = Windows.UI.Xaml.Visibility.Visible;
							m_messageDisplayTime = DateTime.Now;
							allWordsOk = false;
							break;
						}
					}
				}

				if (allWordsOk)
				{
					foreach (string word in wordsPlayed)
					{
						m_allWordsPlayed.Add(word);
						PlayersWords.Items.Add(word);
					}
					foreach (TileControl tile in m_currentWordTiles)
					{
						tile.TileStatus = eTileState.Played;
						m_playedTiles.Add(tile);
						m_boardTiles[tile.GridX, tile.GridY] = tile;
					}

					m_firstWord = false;
					m_currentWordTiles.Clear();
					m_turnState = eTurnState.ComputersTurn;

					for (int i = 7 - m_panelTiles.Count; i > 0; i--)
					{
						int nextLetter = m_randomiser.Next(m_letterBag.Count - 1);

						TileControl tile = m_letterBag[nextLetter];

						tile.TileStatus = eTileState.OnPlayerPanel;
						tile.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
						tile.ManipulationStarting += DragLetter_ManipulationStarting;
						tile.ManipulationDelta += DragLetter_ManipulationDelta;
						tile.ManipulationCompleted += DragLetter_ManipulationCompleted;

						PlaceTileOnPanel(tile, i / 2);

						m_panelTiles.Add(tile);

						m_letterBag.RemoveAt(nextLetter);
					}
				}
			}
		}

		private void PassButton_Click(object sender, RoutedEventArgs e)
		{

		}

		string SortLetters(string s)
		{
			// Convert to char array, then sort and return
			char[] a = s.ToCharArray();
			Array.Sort(a);
			return new string(a);
		}

		private async void ReadWords()
		{
			StorageFile wordsFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Words.txt", UriKind.Absolute));
			m_words = await FileIO.ReadLinesAsync(wordsFile);

			foreach (string word in m_words)
			{
				string key = SortLetters(word);

				if (!m_wordLookup.ContainsKey(key))
				{
					List<string> words = new List<string>();
					words.Add(word);
					m_wordLookup.Add(key, words);
				}
				else
				{
					m_wordLookup[key].Add(word);
				}
			}

			/*
			foreach (string word in m_words)
			{
				m_gaddag.Add(word);
			}
			*/
		}

		void GameTimer_Tick(object sender, object e)
		{
			if (DateTime.Now.Subtract(m_messageDisplayTime).TotalSeconds > 5 && MessageTextBox.Visibility == Windows.UI.Xaml.Visibility.Visible)
			{
				MessageTextBox.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
			}

			if (m_turnState == eTurnState.ComputersTurn)
			{
				if (DetermineComputersWord())
				{
					m_turnState = eTurnState.PlayersTurn;
				}
				else
				{
					MessageTextBox.Text = "Computer could not find a word. Exchanging letters.";
					MessageTextBox.Visibility = Windows.UI.Xaml.Visibility.Visible;
					m_messageDisplayTime = DateTime.Now;

					for (int i = 0; i < m_computersTiles.Count; i++)
					{
						m_computersTiles[i].TileStatus = eTileState.InBag;
						m_letterBag.Add(m_computersTiles[i]);
					}

					m_computersTiles.Clear();
					for (int i = 0; i < 7; i++)
					{
						int nextLetter = m_randomiser.Next(m_letterBag.Count - 1);

						TileControl tile = m_letterBag[nextLetter];

						tile.TileStatus = eTileState.OnComputerPanel;
						m_computersTiles.Add(tile);

						m_letterBag.RemoveAt(nextLetter);
					}
				}
			}
			else if (m_turnState == eTurnState.PlayersTurn)
			{
			}
			else
			{
			}
		}

		private List<string> SortByLength(List<string> unsortedList, bool descendingOrder = true)
		{
			if (descendingOrder)
			{
				// Use LINQ to sort the array received and return a copy.
				var sorted = from s in unsortedList
							 orderby s.Length descending
							 select s;
				return sorted.ToList();
			}
			else
			{
				// Use LINQ to sort the array received and return a copy.
				var sorted = from s in unsortedList
							 orderby s.Length ascending
							 select s;
				return sorted.ToList();
			}
		}

		private List<int> IndicesOfStringInString(string word, string key)
		{
			int i = 0;
			List<int> indices = new List<int>();

			while ((i = word.IndexOf(key, i)) != -1)
			{
				indices.Add(i);

				// Increment the index.
				i++;
			}

			return indices;
		}

		private bool DetermineComputersWord()
		{
			m_computersWordFound = false;

			string computersLetters = "";

			foreach (TileControl tile in m_computersTiles)
			{
				if (tile.Letter != BLANK)
				{
					computersLetters += tile.Letter;
				}
			}

#if DEBUG
			ComputerTextBlock.Text = computersLetters;
#endif

			List<string> existingPlays = new List<string>();
			Dictionary<TileControl, List<string>> playableSpaces = GetPlayableSpaces(computersLetters.Length);

			foreach(TileControl startTile in playableSpaces.Keys)
			{
				foreach(string word in playableSpaces[startTile])
				{
					existingPlays.Add(word);
				}
			}
			if (existingPlays.Count == 0)
			{
				existingPlays.Add("");
			}

			Dictionary<string, TileControl> possiblePlays = FindPossiblePlays(computersLetters, playableSpaces);

			if (m_firstWord)
			{
				m_firstWord = false;
				//List<string> possibleWords = FindPossibleWords(computersLetters, existingPlays);
				//m_computersWordFound = PlaceComputersFirstWord(possibleWords);
				m_computersWordFound = PlaceComputersFirstWord(possiblePlays);
			}
			else
			{
				m_computersWordFound = PlaceComputersWord(possiblePlays);
				/*
				List<string> possibleWords = FindPossibleWords(computersLetters, SortByLength(existingPlays));

				// Now that we have a list of possible words we need to find out which ones will fit on the 
				// scrabble grid with all of the other words.
				foreach (string word in possibleWords)
				{
					// For each word 
					foreach (string existingPlay in existingPlays)
					{
						if (word.Contains(existingPlay))
						{
							List<int> indeciseOfExistingPlay = IndicesOfStringInString(word, existingPlay);

							foreach (int indexOfExistingPlay in indeciseOfExistingPlay)
							{
								bool haveRequiredLetters = true;
								string remainder = word.Substring(0, indexOfExistingPlay) + word.Substring(indexOfExistingPlay + existingPlay.Length);
								List<char> availableLetters = computersLetters.ToList();

								foreach (char letter in remainder)
								{
									if (!availableLetters.Contains(letter))
									{
										haveRequiredLetters = false;
									}
									availableLetters.Remove(letter);
								}
								if (!haveRequiredLetters)
								{
									continue;
								}

								foreach (TileControl startTile in playableSpaces.Keys)
								{
									if (playableSpaces[startTile].Contains(existingPlay))
									{
										if (existingPlay.Length == 1)
										{
											if (startTile.GridX + 1 < 15 && m_boardTiles[startTile.GridX + 1, startTile.GridY] == null)
											{
#if DEBUG
												ComputerTextBlock.Text = computersLetters + " - " + word;
#endif
												if (TryToPlaceComputersWord(word, startTile.GridX - indexOfExistingPlay, startTile.GridY, true))
												{
													m_computersWordFound = true;
													break;
												}
											}
											else
											{
#if DEBUG
												ComputerTextBlock.Text = computersLetters + " - " + word;
#endif
												if (TryToPlaceComputersWord(word, startTile.GridX, startTile.GridY - indexOfExistingPlay, false))
												{
													m_computersWordFound = true;
													break;
												}
											}
										}
										else if (startTile.GridX + 1 < 15 && m_boardTiles[startTile.GridX + 1, startTile.GridY] != null &&
													m_boardTiles[startTile.GridX + 1, startTile.GridY].Letter[0] == existingPlay[1])
										{
#if DEBUG
											ComputerTextBlock.Text = computersLetters + " - " + word;
#endif
											if (TryToPlaceComputersWord(word, startTile.GridX - indexOfExistingPlay, startTile.GridY, true))
											{
												m_computersWordFound = true;
												break;
											}
										}
										else if (startTile.GridY + 1 < 15 && m_boardTiles[startTile.GridX, startTile.GridY + 1] != null &&
													m_boardTiles[startTile.GridX, startTile.GridY + 1].Letter[0] == existingPlay[1])
										{
#if DEBUG
											ComputerTextBlock.Text = computersLetters + " - " + word;
#endif
											if (TryToPlaceComputersWord(word, startTile.GridX, startTile.GridY - indexOfExistingPlay, false))
											{
												m_computersWordFound = true;
												break;
											}
										}
									}
								}
							}
						}

						if (m_computersWordFound)
						{
							break;
						}
					}
					
					if (m_computersWordFound)
					{
						break;
					}					
				}
				*/
			}

			for (int i = 7 - m_computersTiles.Count; i > 0; i--)
			{
				int nextLetter = m_randomiser.Next(m_letterBag.Count - 1);

				TileControl tile = m_letterBag[nextLetter];

				tile.TileStatus = eTileState.OnComputerPanel;
				m_computersTiles.Add(tile);

				m_letterBag.RemoveAt(nextLetter);
			}

			return m_computersWordFound;
		}

		private bool PlaceComputersFirstWord(Dictionary<string, TileControl> possiblePlays)
		{
			bool result = false;
			string longestWord = "";

			foreach (string word in possiblePlays.Keys)
			{
				if (longestWord.Length < word.Length)
				{
					longestWord = word;
				}
			}

			if (longestWord.Length > 0)
			{
				int x = 7;
				int y = 7;

				ComputersWords.Items.Add(longestWord);

				for (int i = 0; i < longestWord.Length; i++)
				{
					string letter = longestWord.Substring(i, 1);
					TileControl tilePlayed = null;

					foreach (TileControl tile in m_computersTiles)
					{
						if (tile.Letter == letter)
						{
							PlaceTileOnBoard(tile, x, y);
							x++;
							tilePlayed = tile;
							break;
						}
					}

					if (tilePlayed != null)
					{
						m_computersTiles.Remove(tilePlayed);
						tilePlayed.TileStatus = eTileState.Played;
						m_playedTiles.Add(tilePlayed);
						m_boardTiles[tilePlayed.GridX, tilePlayed.GridY] = tilePlayed;
					}
				}

				result = true;
			}

			return result;
		}
		private bool PlaceComputersWord(Dictionary<string, TileControl> possiblePlays)
		{
			// Now that we have a list of possible words we need to find out which ones will fit on the 
			// scrabble grid with all of the other words.
			foreach (string word in possiblePlays.Keys)
			{
				TileControl startTile = possiblePlays[word];

				if (startTile.GridX + 1 < 15 && m_boardTiles[startTile.GridX + 1, startTile.GridY] == null)
				{
#if DEBUG
					ComputerTextBlock.Text = computersLetters + " - " + word;
#endif
					if (TryToPlaceComputersWord(word, startTile.GridX - indexOfExistingPlay, startTile.GridY, true))
					{
						m_computersWordFound = true;
						break;
					}
				}
				else
				{
#if DEBUG
					ComputerTextBlock.Text = computersLetters + " - " + word;
#endif
					if (TryToPlaceComputersWord(word, startTile.GridX, startTile.GridY - indexOfExistingPlay, false))
					{
						m_computersWordFound = true;
						break;
					}
				}
			
				else if (startTile.GridX + 1 < 15 && m_boardTiles[startTile.GridX + 1, startTile.GridY] != null &&
						 m_boardTiles[startTile.GridX + 1, startTile.GridY].Letter[0] == existingPlay[1])
				{
#if DEBUG
					ComputerTextBlock.Text = computersLetters + " - " + word;
#endif
					if (TryToPlaceComputersWord(word, startTile.GridX - indexOfExistingPlay, startTile.GridY, true))
					{
						m_computersWordFound = true;
						break;
					}
				}
				else if (startTile.GridY + 1 < 15 && m_boardTiles[startTile.GridX, startTile.GridY + 1] != null &&
							m_boardTiles[startTile.GridX, startTile.GridY + 1].Letter[0] == existingPlay[1])
				{
#if DEBUG
					ComputerTextBlock.Text = computersLetters + " - " + word;
#endif
					if (TryToPlaceComputersWord(word, startTile.GridX, startTile.GridY - indexOfExistingPlay, false))
					{
						m_computersWordFound = true;
						break;
					}
				}
			}
								
		/*					
						
					

					if (m_computersWordFound)
					{
						break;
					}
				}

				if (longestWord.Length > 0)
			{
				int x = 7;
				int y = 7;

				ComputersWords.Items.Add(longestWord);

				for (int i = 0; i < longestWord.Length; i++)
				{
					string letter = longestWord.Substring(i, 1);
					TileControl tilePlayed = null;

					foreach (TileControl tile in m_computersTiles)
					{
						if (tile.Letter == letter)
						{
							PlaceTileOnBoard(tile, x, y);
							x++;
							tilePlayed = tile;
							break;
						}
					}

					if (tilePlayed != null)
					{
						m_computersTiles.Remove(tilePlayed);
						tilePlayed.TileStatus = eTileState.Played;
						m_playedTiles.Add(tilePlayed);
						m_boardTiles[tilePlayed.GridX, tilePlayed.GridY] = tilePlayed;
					}
				}

				result = true;
			}
	*/
			return true;
		}

		private bool PlaceComputersFirstWord(List<string> possibleWords)
		{
			bool result = false;
			string longestWord = "";

			foreach (string word in possibleWords)
			{
				if (longestWord.Length < word.Length)
				{
					longestWord = word;
				}
			}

			if (longestWord.Length > 0)
			{
				int x = 7;
				int y = 7;

				ComputersWords.Items.Add(longestWord);

				for (int i = 0; i < longestWord.Length; i++)
				{
					string letter = longestWord.Substring(i, 1);
					TileControl tilePlayed = null;

					foreach (TileControl tile in m_computersTiles)
					{
						if (tile.Letter == letter)
						{
							PlaceTileOnBoard(tile, x, y);
							x++;
							tilePlayed = tile;
							break;
						}
					}

					if (tilePlayed != null)
					{
						m_computersTiles.Remove(tilePlayed);
						tilePlayed.TileStatus = eTileState.Played;
						m_playedTiles.Add(tilePlayed);
						m_boardTiles[tilePlayed.GridX, tilePlayed.GridY] = tilePlayed;
					}
				}

				result = true;
			}

			return result;
		}

		private bool TryToPlaceComputersWord(string word, int startX, int startY, bool horizontal)
		{
			int index = 0;
			int existingTiles = 0;
			List<TileControl> computersWordTiles = new List<TileControl>();

			if(startX < 0 || startY < 0)
			{
				return false;
			}

			if (horizontal)
			{
				bool exitFunction = false;

				if (startX + word.Length >= 15)
				{
					return false;
				}

				for (int X = startX; X < 15 && X - startX < word.Length; X++)
				{
					// If a letter is encountered that does not fit the word then it does not fit.
					if (m_boardTiles[X, startY] != null && !word.Contains(m_boardTiles[X, startY].Letter))
					{
						exitFunction = true;
						break;
					}
					else if(m_boardTiles[X, startY] == null)
					{
						for (int i = 0; i < m_computersTiles.Count; i++)
						{
							TileControl tile = m_computersTiles[i];

							if (tile.Letter[0] == word[index])
							{
								tile.GridX = X;
								tile.GridY = startY;
								computersWordTiles.Add(tile);
								m_computersTiles.Remove(tile);
								break;
							}
						}
					}
					else
					{
						existingTiles++;
					}
					index++;
				}
				if(exitFunction)
				{
					foreach(TileControl tile in computersWordTiles)
					{
						m_computersTiles.Add(tile);
					}
				}
			}
			else
			{
				bool exitFunction = false;

				if (startY + word.Length >= 15)
				{
					return false;
				}

				for (int Y = startY; Y < 15 && Y - startY < word.Length; Y++)
				{
					// If a letter is encountered that does not fit the word then it does not fit.
					if (m_boardTiles[startX, Y] != null && !word.Contains(m_boardTiles[startX, Y].Letter))
					{
						exitFunction = true;
						break;
					}
					else if (m_boardTiles[startX, Y] == null)
					{
						for (int i = 0; i < m_computersTiles.Count; i++)
						{
							TileControl tile = m_computersTiles[i];

							if (tile.Letter[0] == word[index])
							{
								tile.GridX = startX;
								tile.GridY = Y;
								computersWordTiles.Add(tile);
								m_computersTiles.Remove(tile);
								break;
							}
						}
					}
					else
					{
						existingTiles++;
					}
					index++;
				}
				if (exitFunction)
				{
					foreach (TileControl tile in computersWordTiles)
					{
						m_computersTiles.Add(tile);
					}
				}
			}

			if (computersWordTiles.Count == 0)
			{
				return false;
			}

			if (computersWordTiles.Count + existingTiles != word.Length)
			{
				foreach (TileControl tile in computersWordTiles)
				{
					m_computersTiles.Add(tile);
				}

				return false;
			}

			// Check that all words created by the placement of this word are valid.
			List<string> wordsPlayed = GetPlayedWords(horizontal, computersWordTiles);

			foreach (string playedWord in wordsPlayed)
			{
				if (playedWord.Length > 0)
				{
					if (!m_words.Contains(playedWord))
					{
						foreach (TileControl tile in computersWordTiles)
						{
							m_computersTiles.Add(tile);
						}

						return false;
					}
				}
			}

			foreach (string playedWord in wordsPlayed)
			{
				m_allWordsPlayed.Add(playedWord);
				ComputersWords.Items.Add(playedWord);
			}

			// If we get to here then the word should fit successfully.
			foreach (char letter in word)
			{
				if (m_boardTiles[startX, startY] == null)
				{
					TileControl tilePlayed = null;

					foreach (TileControl tile in computersWordTiles)
					{
						if (tile.Letter == letter.ToString())
						{
							PlaceTileOnBoard(tile, startX, startY);
							tilePlayed = tile;
							break;
						}
					}

					if (tilePlayed != null)
					{
						computersWordTiles.Remove(tilePlayed);
						tilePlayed.TileStatus = eTileState.Played;
						m_playedTiles.Add(tilePlayed);
						m_boardTiles[tilePlayed.GridX, tilePlayed.GridY] = tilePlayed;
					}
				}
				if (horizontal)
				{
					startX++;
				}
				else
				{
					startY++;
				}
			}
			return true;
		}



		private void FillLetterBag()
		{
			m_letterBag.Clear();

			foreach (ScrabbleLetterConfig scrabbleLetter in s_scrabbleLetters)
			{
				for (int i = 0; i < scrabbleLetter.NumberOf; i++)
				{
					TileControl tile = new TileControl(scrabbleLetter.Letter, scrabbleLetter.LetterValue);
					tile.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
					tile.RenderTransform = new TranslateTransform();
					tile.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;
					tile.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Top;
					MainGrid.Children.Add(tile);

					m_letterBag.Add(tile);
				}
			}
		}

		private void StartNewGame()
		{
			FillLetterBag();

			for (int i = 0; i < 7; i++)
			{
				int nextLetter = m_randomiser.Next(m_letterBag.Count - 1);

				TileControl tile = m_letterBag[nextLetter];

				tile.TileStatus = eTileState.OnComputerPanel;
				m_computersTiles.Add(tile);

				m_letterBag.RemoveAt(nextLetter);
			}

			for (int i = 0; i < 7; i++)
			{
				int nextLetter = m_randomiser.Next(m_letterBag.Count - 1);

				TileControl tile = m_letterBag[nextLetter];
				tile.TileStatus = eTileState.OnPlayerPanel;
				tile.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
				tile.ManipulationStarting += DragLetter_ManipulationStarting;
				tile.ManipulationDelta += DragLetter_ManipulationDelta;
				tile.ManipulationCompleted += DragLetter_ManipulationCompleted;

				PlaceTileOnPanel(tile, i / 2);

				m_panelTiles.Add(tile);

				m_letterBag.RemoveAt(nextLetter);
			}

			if (m_randomiser.Next(100) > 50)
			{
				m_turnState = eTurnState.PlayersTurn;
				MessageTextBox.Text = "You won the draw, it is your turn to place a word.";
				MessageTextBox.Visibility = Windows.UI.Xaml.Visibility.Visible;
				m_messageDisplayTime = DateTime.Now;
			}
			else
			{
				m_turnState = eTurnState.ComputersTurn;
				MessageTextBox.Text = "The Computer won the draw.";
				MessageTextBox.Visibility = Windows.UI.Xaml.Visibility.Visible;
				m_messageDisplayTime = DateTime.Now;
			}

			m_firstWord = true;
			m_gameStartTime = DateTime.Now;
			m_gameTimer.Start();
		}

		private void TileMoved()
		{
			if (m_panelTiles.Count == 7)
			{
				PlayButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
				RecallLettersButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
				PassButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
				SwapLettersButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
			}
			else
			{
				PlayButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
				RecallLettersButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
				PassButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
				SwapLettersButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
			}
		}


		List<TileControl> SortCurrentWordTiles(bool horizontal, List<TileControl> currentWordTiles)
		{
			int index = -1;
			List<TileControl> playedTiles = new List<TileControl>();

			// Sort the letters into the order in which they appear on the board.
			foreach (TileControl tile in currentWordTiles)
			{
				if (playedTiles.Count == 0)
				{
					playedTiles.Add(tile);
				}
				else
				{
					bool before = false;

					for (index = 0; index < playedTiles.Count; index++)
					{
						if (horizontal)
						{
							if (tile.GridX < playedTiles[index].GridX)
							{
								before = true;
								break;
							}
						}
						else
						{
							if (tile.GridY < playedTiles[index].GridY)
							{
								before = true;
								break;
							}
						}
					}

					if (before)
					{
						playedTiles.Insert(index, tile);
					}
					else
					{
						playedTiles.Add(tile);
					}
				}
			}

			return playedTiles;
		}

		private List<string> GetPlayedWords(bool horizontal, List<TileControl> currentWordTiles)
		{
			string playedWord = string.Empty;
			List<string> playedWords = new List<string>();
			List<TileControl> playedTiles = SortCurrentWordTiles(horizontal, currentWordTiles);

			if (horizontal)
			{
				playedWord = GetMainWordPlayedHorizontal(playedTiles);
			}
			else
			{
				playedWord = GetMainWordPlayedVertical(playedTiles);
			}

			if (playedWord.Length > 0)
			{
				playedWords.Add(playedWord);
				if (horizontal)
				{
					GetSideWordsForWordPlayedHorizontal(playedTiles, ref playedWords);
				}
				else
				{
					GetSideWordsForWordPlayedVertical(playedTiles, ref playedWords);
				}
			}

			return playedWords;
		}

		private string GetMainWordPlayedHorizontal(List<TileControl> playedTiles)
		{
			int index;
			int lastX = -1;

			// Find the word that has been played.
			string playedWord = playedTiles[0].Letter;

			// Prepend any letters that appear before the played word.
			if (playedTiles[0].GridX > 0)
			{
				int previousX = playedTiles[0].GridX - 1;
				while (previousX >= 0 && m_boardTiles[previousX, playedTiles[0].GridY] != null)
				{
					playedWord = m_boardTiles[previousX, playedTiles[0].GridY].Letter + playedWord;
					previousX--;
				}
			}

			// Now fill in the played letters and any that are already on the board.
			lastX = playedTiles[0].GridX;
			for (index = 1; index < playedTiles.Count; index++)
			{
				// Fill in any letters that are already on the board.
				if (lastX != playedTiles[index].GridX - 1)
				{
					for (int X = lastX + 1; X < playedTiles[index].GridX; X++)
					{
						if (m_boardTiles[X, playedTiles[index].GridY] != null)
						{
							playedWord += m_boardTiles[X, playedTiles[index].GridY].Letter;
						}
						else
						{
							MessageTextBox.Text = "There seems to be a gap in your word!";
							MessageTextBox.Visibility = Windows.UI.Xaml.Visibility.Visible;
							m_messageDisplayTime = DateTime.Now;
							playedWord = string.Empty;
							break;
						}
					}
				}

				// Add the current tile.
				playedWord += playedTiles[index].Letter;

				// Save the last X value;
				lastX = playedTiles[index].GridX;
			}

			// Append any letters that appear after the played word.
			if (lastX < 15)
			{
				int nextX = lastX + 1;
				while (nextX < 15 && m_boardTiles[nextX, playedTiles[0].GridY] != null)
				{
					playedWord += m_boardTiles[nextX, playedTiles[0].GridY].Letter;
					nextX++;
				}
			}

			return playedWord;
		}

		private string GetMainWordPlayedVertical(List<TileControl> playedTiles)
		{
			int index;
			int lastY = -1;

			// Find the word that has been played.
			string playedWord = playedTiles[0].Letter;

			// Prepend any letters that appear before the played word.
			if (playedTiles[0].GridY > 0)
			{
				int previousY = playedTiles[0].GridY - 1;
				while (previousY >= 0 && m_boardTiles[playedTiles[0].GridX, previousY] != null)
				{
					playedWord = m_boardTiles[playedTiles[0].GridX, previousY].Letter + playedWord;
					previousY--;
				}
			}

			// Now fill in the played letters and any that are already on the board.
			lastY = playedTiles[0].GridY;
			for (index = 1; index < playedTiles.Count; index++)
			{
				// Fill in any letters that are already on the board.
				if (lastY != playedTiles[index].GridY - 1)
				{
					for (int Y = lastY + 1; Y < playedTiles[index].GridY; Y++)
					{
						if (m_boardTiles[playedTiles[index].GridX, Y] != null)
						{
							playedWord += m_boardTiles[playedTiles[index].GridX, Y].Letter;
						}
						else
						{
							MessageTextBox.Text = "There seems to be a gap in your word!";
							MessageTextBox.Visibility = Windows.UI.Xaml.Visibility.Visible;
							m_messageDisplayTime = DateTime.Now;
							playedWord = string.Empty;
							break;
						}
					}
				}
				// Add the current tile.
				playedWord += playedTiles[index].Letter;

				// Save the last Y value;
				lastY = playedTiles[index].GridY;
			}

			// Append any letters that appear after the played word.
			if (lastY < 15)
			{
				int nextY = lastY + 1;
				while (nextY < 15 && m_boardTiles[playedTiles[0].GridX, nextY] != null)
				{
					playedWord += m_boardTiles[playedTiles[0].GridX, nextY].Letter;
					nextY++;
				}
			}

			return playedWord;
		}

		private void GetSideWordsForWordPlayedHorizontal(List<TileControl> playedTiles, ref List<string> playedWords)
		{
			foreach (TileControl tile in playedTiles)
			{
				if (tile.GridY > 0 && m_boardTiles[tile.GridX, tile.GridY - 1] != null)
				{
					int Y = tile.GridY - 1;
					string word = string.Empty;

					// Work up the column until an empty space is found.
					while (Y > 0 && m_boardTiles[tile.GridX, Y - 1] != null)
					{
						Y--;
					}
					// Now work back down to build up the word.
					while (Y < 14 && m_boardTiles[tile.GridX, Y + 1] != null)
					{
						word += m_boardTiles[tile.GridX, Y].Letter;
						Y++;
					}
					word += tile.Letter;

					playedWords.Add(word);
				}
				if (tile.GridY < 14 && m_boardTiles[tile.GridX, tile.GridY + 1] != null)
				{
					int Y = tile.GridY + 1;
					string word = tile.Letter;

					// Work down the column until an empty space is found.
					while (Y < 14 && m_boardTiles[tile.GridX, Y] != null)
					{
						word += m_boardTiles[tile.GridX, Y].Letter;
						Y++;
					}

					playedWords.Add(word);
				}
			}
		}

		private void GetSideWordsForWordPlayedVertical(List<TileControl> playedTiles, ref List<string> playedWords)
		{
			foreach (TileControl tile in playedTiles)
			{
				if (tile.GridX > 0 && m_boardTiles[tile.GridX - 1, tile.GridY] != null)
				{
					int X = tile.GridX - 1;
					string word = string.Empty;

					// Work back along the row until an empty space is found.
					while (X > 0 && m_boardTiles[X - 1, tile.GridY] != null)
					{
						X--;
					}
					// Now work forward to build up the word.
					while (X < 14 && m_boardTiles[X, tile.GridY] != null)
					{
						word += m_boardTiles[X, tile.GridY].Letter;
						X++;
					}
					word += tile.Letter;

					playedWords.Add(word);
				}
				if (tile.GridX < 14 && m_boardTiles[tile.GridX + 1, tile.GridY] != null)
				{
					int X = tile.GridX + 1;
					string word = tile.Letter;

					// Work along the row until an empty space is found.
					while (X < 14 && m_boardTiles[X, tile.GridY] != null)
					{
						word += m_boardTiles[X, tile.GridY].Letter;
						X++;
					}

					playedWords.Add(word);
				}
			}
		}

		public Dictionary<ExistingPlay, List<string>> GetPlayableSpaces(int maxLetters)
		{
			ExistingPlay startTile = null;
			Dictionary<ExistingPlay, List<string>> playableSpaces = new Dictionary<ExistingPlay, List<string>>();
			Dictionary<ExistingPlay, List<string>> horizontalSpaces = new Dictionary<ExistingPlay, List<string>>();
			Dictionary<ExistingPlay, List<string>> verticalSpaces = new Dictionary<ExistingPlay, List<string>>();

			// Find all playable spaces across the board.
			for (int y = 0; y < 15; y++)
			{
				startTile = null;
				for (int x = 0; x < 15; x++)
				{
					if (m_boardTiles[x, y] != null)
					{
						if (startTile == null)
						{
							startTile = new ExistingPlay() { StartTile = m_boardTiles[x, y], Horizontal = true };
						}
						if (!horizontalSpaces.ContainsKey(startTile))
						{
							List<string> letters = new List<string>();
							letters.Add(m_boardTiles[x, y].Letter);
							horizontalSpaces.Add(startTile, letters);
						}
						else
						{
							if(horizontalSpaces[startTile].Count == 1)
							{
								horizontalSpaces[startTile].Add(horizontalSpaces[startTile][0]);
							}
							horizontalSpaces[startTile][1] += m_boardTiles[x, y].Letter;
						}
					}
					else 
					{
						startTile = null;
					}
				}
			}

			for (int x = 0; x < 15; x++)
			{
				startTile = null;
				for (int y = 0; y < 15; y++)
				{
					if (m_boardTiles[x, y] != null)
					{
						if (startTile == null)
						{
							startTile = new ExistingPlay() { StartTile = m_boardTiles[x, y], Horizontal = false };
						}
						if (!verticalSpaces.ContainsKey(startTile))
						{
							List<string> letters = new List<string>();
							letters.Add(m_boardTiles[x, y].Letter);
							verticalSpaces.Add(startTile, letters);
						}
						else
						{
							if (verticalSpaces[startTile].Count == 1)
							{
								verticalSpaces[startTile].Add(verticalSpaces[startTile][0]);
							}
							verticalSpaces[startTile][1] += m_boardTiles[x, y].Letter;
						}
					}
					else
					{
						startTile = null;
					}
				}
			}

			foreach (ExistingPlay play in horizontalSpaces.Keys)
			{
				foreach (string word in horizontalSpaces[play])
				{
					TileControl tile = play.StartTile;
					// If a single letter has a letter both horizontally and vertically beside then it is not a playable letter.
					if(word.Length == 1)
					{
						if ((tile.GridX - 1 > 0 && m_boardTiles[tile.GridX - 1, tile.GridY] != null) ||     // Tile to the left.
							(tile.GridX + 1 < 15 && m_boardTiles[tile.GridX + 1, tile.GridY] != null))      // Tile to the right.
						{
							if ((tile.GridY - 1 > 0 && m_boardTiles[tile.GridX, tile.GridY - 1] != null) || // Tile above.
								(tile.GridY + 1 < 15 && m_boardTiles[tile.GridX, tile.GridY + 1] != null))  // Tile below.
							{
								continue;
							}
						}
					}
					if (!playableSpaces.ContainsKey(play))
					{
						playableSpaces.Add(play, new List<string>());
					}

					if (!playableSpaces[tile].Contains(word))
					{
						playableSpaces[tile].Add(word);
					}
				}
			}

			foreach (TileControl tile in verticalSpaces.Keys)
			{
				foreach (string word in verticalSpaces[tile])
				{
					if (!playableSpaces.ContainsKey(tile))
					{
						playableSpaces.Add(tile, new List<string>());
					}

					if (!playableSpaces[tile].Contains(word))
					{
						playableSpaces[tile].Add(word);
					}
				}
			}

			return playableSpaces;
		}

		public Dictionary<string, TileControl> FindPossiblePlays(string playersLetters, Dictionary<TileControl, List<string>> playableSpaces)
		{
			DateTime start = DateTime.Now;
			List<string> keysChecked = new List<string>();
			Dictionary<string, TileControl> possiblePlays = new Dictionary<string, TileControl>;

			foreach (TileControl tile in playableSpaces.Keys)
			{
				// For each place that a word can be made ...
				foreach (string playableSpace in playableSpaces[tile])
				{
					// Find all words using 1 to all of the players letters.
					for (int numberOfCharacters = 1; numberOfCharacters <= playersLetters.Length; numberOfCharacters++)
					{
						for (int i = 0; i <= playersLetters.Length - numberOfCharacters; i++)
						{
							// Select the first n letters
							string testCharacters = playersLetters.Substring(i, numberOfCharacters);
							string key = SortLetters(playableSpace + testCharacters);

							// If the first n letters make up valid words then add them to the list.
							if (!keysChecked.Contains(key))
							{
								keysChecked.Add(key);
								if (m_wordLookup.ContainsKey(key))
								{
									foreach (string actualWord in m_wordLookup[key])
									{
										if (actualWord.Contains(playableSpace))
										{
											possiblePlays.Add(actualWord, tile);
										}
									}
								}
							}

							// Now add each of the remaining letters in turn and check to see if that makes a word.
							for (int j = i + numberOfCharacters; j < playersLetters.Length; j++)
							{
								key = SortLetters(playableSpace + testCharacters + playersLetters.Substring(j, 1));

								if (!keysChecked.Contains(key))
								{
									keysChecked.Add(key);
									if (m_wordLookup.ContainsKey(key))
									{
										foreach (string actualWord in m_wordLookup[key])
										{
											if (actualWord.Contains(playableSpace))
											{
												possiblePlays.Add(actualWord, tile);
											}
										}
									}
								}
							}
						}
					}
				}
			}

			DateTime end = DateTime.Now;

			TimeSpan duration = end.Subtract(start);
			MessageTextBox.Text = duration.Ticks.ToString();
			MessageTextBox.Visibility = Windows.UI.Xaml.Visibility.Visible;
			m_messageDisplayTime = DateTime.Now;

			return possiblePlays;
			// Sort all words (longest first) and return the list.
			//return SortByLength(possibleWords);
		}
		
		public List<string> FindPossibleWords(string playersLetters, List<string> existingPlays)
		{
			DateTime start = DateTime.Now;

			List<string> possibleWords = new List<string>();
			List<string> keysChecked = new List<string>();

			// For each place that a word can be made ...
			foreach (string playableSpace in existingPlays)
			{
				// Find all words using 1 to all of the players letters.
				for (int numberOfCharacters = 1; numberOfCharacters <= playersLetters.Length; numberOfCharacters++)
				{
					for (int i = 0; i <= playersLetters.Length - numberOfCharacters; i++)
					{
						// Select the first n letters
						string testCharacters = playersLetters.Substring(i, numberOfCharacters);
						string key = SortLetters(playableSpace + testCharacters);

						// If the first n letters make up valid words then add them to the list.
						if (!keysChecked.Contains(key))
						{
							keysChecked.Add(key);
							if (m_wordLookup.ContainsKey(key))
							{
								foreach (string actualWord in m_wordLookup[key])
								{
									if (actualWord.Contains(playableSpace))
									{
										possibleWords.Add(actualWord);
									}
								}
							}
						}

						// Now add each of the remaining letters in turn and check to see if that makes a word.
						for (int j = i + numberOfCharacters; j < playersLetters.Length; j++)
						{
							key = SortLetters(playableSpace + testCharacters + playersLetters.Substring(j, 1));

							if (!keysChecked.Contains(key))
							{
								keysChecked.Add(key);
								if (m_wordLookup.ContainsKey(key))
								{
									foreach (string actualWord in m_wordLookup[key])
									{
										if (actualWord.Contains(playableSpace))
										{
											possibleWords.Add(actualWord);
										}
									}
								}
							}
						}
					}
				}
			}

			DateTime end = DateTime.Now;

			TimeSpan duration = end.Subtract(start);
			MessageTextBox.Text = duration.Ticks.ToString();
			MessageTextBox.Visibility = Windows.UI.Xaml.Visibility.Visible;
			m_messageDisplayTime = DateTime.Now;

			// Sort all words (longest first) and return the list.
			return SortByLength(possibleWords);

		}
	}
}
