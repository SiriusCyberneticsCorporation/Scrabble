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
using Windows.UI.Xaml.Documents;
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
		private int m_playersScore = 0;
		private int m_computersScore = 0;
		private bool m_restartLastGame = false;
		private bool m_computersWordFound = false;
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
		private eScrabbleScores[,] m_boardScores = new eScrabbleScores[15, 15];
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

			this.SizeChanged += GamePage_SizeChanged;

			m_randomiser = new Random((int)DateTime.Now.TimeOfDay.TotalSeconds);

			m_gameTimer.Tick += GameTimer_Tick;
			m_gameTimer.Interval = new TimeSpan(0, 0, 1);

			//-------------------------------------------------------------------------------------
			// SET UP THE SCRABBLE BOARD SCORES
			//-------------------------------------------------------------------------------------
			// Start by setting every square to letter value.
			for (int i = 0; i < 15; i++)
			{
				for (int j = 0; j < 15; j++)
				{
					m_boardScores[i, j] = eScrabbleScores.LetterValue;
				}
			}
			// Place the Double Letter squares.
			m_boardScores[3, 0] = eScrabbleScores.DoubleLetterValue;
			m_boardScores[11, 0] = eScrabbleScores.DoubleLetterValue;
			m_boardScores[6, 2] = eScrabbleScores.DoubleLetterValue;
			m_boardScores[8, 2] = eScrabbleScores.DoubleLetterValue;
			m_boardScores[0, 3] = eScrabbleScores.DoubleLetterValue;
			m_boardScores[7, 3] = eScrabbleScores.DoubleLetterValue;
			m_boardScores[14, 3] = eScrabbleScores.DoubleLetterValue;
			m_boardScores[2, 6] = eScrabbleScores.DoubleLetterValue;
			m_boardScores[6, 6] = eScrabbleScores.DoubleLetterValue;
			m_boardScores[8, 6] = eScrabbleScores.DoubleLetterValue;
			m_boardScores[12, 6] = eScrabbleScores.DoubleLetterValue;
			m_boardScores[3, 7] = eScrabbleScores.DoubleLetterValue;
			m_boardScores[11, 7] = eScrabbleScores.DoubleLetterValue;
			m_boardScores[2, 8] = eScrabbleScores.DoubleLetterValue;
			m_boardScores[6, 8] = eScrabbleScores.DoubleLetterValue;
			m_boardScores[8, 8] = eScrabbleScores.DoubleLetterValue;
			m_boardScores[12, 8] = eScrabbleScores.DoubleLetterValue;
			m_boardScores[0, 11] = eScrabbleScores.DoubleLetterValue;
			m_boardScores[7, 11] = eScrabbleScores.DoubleLetterValue;
			m_boardScores[14, 11] = eScrabbleScores.DoubleLetterValue;
			m_boardScores[6, 12] = eScrabbleScores.DoubleLetterValue;
			m_boardScores[8, 12] = eScrabbleScores.DoubleLetterValue;
			m_boardScores[3, 14] = eScrabbleScores.DoubleLetterValue;
			m_boardScores[11, 14] = eScrabbleScores.DoubleLetterValue;
			// Place Triple Letter squares.
			m_boardScores[5, 1] = eScrabbleScores.TripleLetterValue;
			m_boardScores[9, 1] = eScrabbleScores.TripleLetterValue;
			m_boardScores[1, 5] = eScrabbleScores.TripleLetterValue;
			m_boardScores[5, 5] = eScrabbleScores.TripleLetterValue;
			m_boardScores[9, 5] = eScrabbleScores.TripleLetterValue;
			m_boardScores[13, 5] = eScrabbleScores.TripleLetterValue;
			m_boardScores[1, 9] = eScrabbleScores.TripleLetterValue;
			m_boardScores[5, 9] = eScrabbleScores.TripleLetterValue;
			m_boardScores[9, 9] = eScrabbleScores.TripleLetterValue;
			m_boardScores[13, 9] = eScrabbleScores.TripleLetterValue;
			m_boardScores[5, 13] = eScrabbleScores.TripleLetterValue;
			m_boardScores[9, 13] = eScrabbleScores.TripleLetterValue;
			// Place Double Word squares.
			m_boardScores[1, 1] = eScrabbleScores.DoubleWordValue;
			m_boardScores[13, 1] = eScrabbleScores.DoubleWordValue;
			m_boardScores[2, 2] = eScrabbleScores.DoubleWordValue;
			m_boardScores[12, 2] = eScrabbleScores.DoubleWordValue;
			m_boardScores[3, 3] = eScrabbleScores.DoubleWordValue;
			m_boardScores[11, 3] = eScrabbleScores.DoubleWordValue;
			m_boardScores[4, 4] = eScrabbleScores.DoubleWordValue;
			m_boardScores[10, 4] = eScrabbleScores.DoubleWordValue;
			m_boardScores[7, 7] = eScrabbleScores.DoubleWordValue;
			m_boardScores[4, 10] = eScrabbleScores.DoubleWordValue;
			m_boardScores[10, 10] = eScrabbleScores.DoubleWordValue;
			m_boardScores[3, 11] = eScrabbleScores.DoubleWordValue;
			m_boardScores[11, 11] = eScrabbleScores.DoubleWordValue;
			m_boardScores[2, 12] = eScrabbleScores.DoubleWordValue;
			m_boardScores[12, 12] = eScrabbleScores.DoubleWordValue;
			m_boardScores[1, 13] = eScrabbleScores.DoubleWordValue;
			m_boardScores[13, 13] = eScrabbleScores.DoubleWordValue;
			// Place Triple Word squares.
			m_boardScores[0, 0] = eScrabbleScores.TripleWordValue;
			m_boardScores[7, 0] = eScrabbleScores.TripleWordValue;
			m_boardScores[14, 0] = eScrabbleScores.TripleWordValue;
			m_boardScores[0, 7] = eScrabbleScores.TripleWordValue;
			m_boardScores[14, 7] = eScrabbleScores.TripleWordValue;
			m_boardScores[0, 14] = eScrabbleScores.TripleWordValue;
			m_boardScores[7, 14] = eScrabbleScores.TripleWordValue;
			m_boardScores[14, 14] = eScrabbleScores.TripleWordValue;
			//-------------------------------------------------------------------------------------

			FillLetterBag();

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
				StartGame();
			}
		}

		private void GamePage_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (ScrabbleBoard.Height > 0 && ScrabbleBoard.Width > 0)
			{
				// Decide whether height or width is the governing factor
				if (CentreRowDefinition.ActualHeight / 16 > CentreColumnDefinition.ActualWidth / 15)    // Width
				{
					ScrabbleBoard.Height = 16 * CentreColumnDefinition.ActualWidth / 15;
				}
				else
				{
					ScrabbleBoard.Width = 15 * CentreRowDefinition.ActualHeight / 16;
				}
				ResizeTiles();
			}
		}

		private void ScrabbleBoard_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			ResizeTiles();
		}

		private void ResizeTiles()
		{
			m_tileSize = ScrabbleBoard.ActualWidth / 15;

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
			else
			{
				TileControl draggedItem = (TileControl)sender;

				if(!(draggedItem.TileStatus == eTileState.ComposingNewWord || draggedItem.TileStatus == eTileState.OnPlayerPanel))
				{
					e.Handled = true;
				}
			}
		}

		private void DragLetter_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			if (!e.IsInertial)
			{
				TileControl draggedItem = (TileControl)sender;
				TranslateTransform tileRenderTransform = draggedItem.RenderTransform as TranslateTransform;

				tileRenderTransform.X += e.Delta.Translation.X;
				tileRenderTransform.Y += e.Delta.Translation.Y;

				if (m_previousImage != null)
				{
					Canvas.SetZIndex(m_previousImage, 0);
				}
				m_previousImage = draggedItem;
				Canvas.SetZIndex(draggedItem, 1);
			}
			else
			{
				e.Complete();
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

			draggedItem.Width = m_tileSize * 0.95;
			draggedItem.Height = m_tileSize * 0.95;
			draggedItem.GridX = gridX;
			draggedItem.GridY = 16;
			draggedItem.Visibility = Visibility.Visible;

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

			tileRenderTransform.X = positionOfBoard.X + (m_tileSize * gridX) + m_tileSize * 0.015;
			tileRenderTransform.Y = positionOfBoard.Y + (m_tileSize * gridY) + m_tileSize * 0.015;

			if (draggedItem.GridY != 16 && draggedItem.GridX >= 0 && draggedItem.GridY >= 0)
			{
				m_boardSpaceFilled[draggedItem.GridX, draggedItem.GridY] = false;
			}

			m_boardSpaceFilled[gridX, gridY] = true;
			draggedItem.Width = m_tileSize * 0.95;
			draggedItem.Height = m_tileSize * 0.95;
			draggedItem.GridX = gridX;
			draggedItem.GridY = gridY;
			draggedItem.Visibility = Visibility.Visible;

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

		#endregion Letter Dragging Functionality

		private void ScrabbleBoard_Loaded(object sender, RoutedEventArgs e)
		{

		}

		private void NewGameButton_Click(object sender, RoutedEventArgs e)
		{
			PlayButton.Visibility = Visibility.Collapsed;
			RecallLettersButton.Visibility = Visibility.Collapsed;
			PassButton.Visibility = Visibility.Visible;
			SwapLettersButton.Visibility = Visibility.Visible;
			ShuffleLettersButton.Visibility = Visibility.Visible;
			NewGameButton.Visibility = Visibility.Collapsed;

			m_playersScore = 0;
			m_computersScore = 0;
			PlayersScoreTextBlock.Text = "0";
			ComputersScoreTextBlock.Text = "0";
			PlayersWords.Blocks.Clear();
			ComputersWords.Blocks.Clear();

			foreach (TileControl tile in m_currentWordTiles)
			{
				tile.Visibility = Visibility.Collapsed;
				tile.TileStatus = eTileState.InBag;
				m_letterBag.Add(tile);
			}
			m_currentWordTiles.Clear();

			foreach (TileControl tile in m_playedTiles)
			{
				tile.Visibility = Visibility.Collapsed;
				tile.TileStatus = eTileState.InBag;
				m_letterBag.Add(tile);
			}
			m_playedTiles.Clear();

			foreach (TileControl tile in m_panelTiles)
			{
				tile.Visibility = Visibility.Collapsed;
				tile.TileStatus = eTileState.InBag;
				m_letterBag.Add(tile);
			}
			m_panelTiles.Clear();

			foreach (TileControl tile in m_computersTiles)
			{
				tile.Visibility = Visibility.Collapsed;
				tile.TileStatus = eTileState.InBag;
				m_letterBag.Add(tile);
			}
			m_computersTiles.Clear();

			for (int x = 0; x < 15; x++)
			{
				for (int y = 0; y < 15; y++)
				{
					if (m_boardTiles[x, y] != null)
					{
						m_boardTiles[x, y].TileStatus = eTileState.InBag;
						m_letterBag.Add(m_boardTiles[x, y]);
						m_boardTiles[x, y] = null;
					}
					m_boardSpaceFilled[x, y] = false;
				}
			}

			StartGame();
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
				if (m_letterBag.Count > 0)
				{
					int nextLetter = m_randomiser.Next(m_letterBag.Count - 1);

					TileControl tile = m_letterBag[nextLetter];

					tile.TileStatus = eTileState.OnPlayerPanel;
					m_panelTiles.Add(tile);

					m_letterBag.RemoveAt(nextLetter);
				}
				else
				{
					break;
				}
			}
		}

		private void RecallLettersButton_Click(object sender, RoutedEventArgs e)
		{
			if (m_currentWordTiles.Count > 0)
			{
				while ( m_currentWordTiles.Count > 0)
				{
					PlaceTileOnPanel(m_currentWordTiles[0], 0);
				}
			}
		}

		private void PlayButton_Click(object sender, RoutedEventArgs e)
		{
			if (m_currentWordTiles.Count == 0)
			{
				MessageTextBox.Text = "You have not placed any letters!";
				MessageTextBox.Visibility = Visibility.Visible;
				m_messageDisplayTime = DateTime.Now;
			}
			else
			{
				MessageTextBox.Visibility = Visibility.Collapsed;

				bool allWordsOk = true;
				List<WordAndScore> wordsPlayed = new List<WordAndScore>();

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
						MessageTextBox.Visibility = Visibility.Visible;
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
				}

				foreach (WordAndScore playedWord in wordsPlayed)
				{
					if (playedWord.Word.Length > 0)
					{
						if (!m_words.Contains(playedWord.Word))
						{
							MessageTextBox.Text = "My dictionary does not contain the word '" + playedWord.Word + "'.";
							MessageTextBox.Visibility = Visibility.Visible;
							m_messageDisplayTime = DateTime.Now;
							allWordsOk = false;
							break;
						}
					}
				}

				if (allWordsOk)
				{
					int totalScore = m_currentWordTiles.Count == 7 ? 50 : 0;

					Paragraph scoreText = new Paragraph();

					foreach (WordAndScore playedWord in wordsPlayed)
					{
						if (playedWord.Score == 0 || playedWord.Word.Length == 0)
						{
							int x = 0;
						}
						totalScore += playedWord.Score;
						m_allWordsPlayed.Add(playedWord.Word);

						if (scoreText.Inlines.Count > 0)
						{
							scoreText.Inlines.Add(new Run() { Text = "\r\n + " });
						}
						scoreText.Inlines.Add(new Run() { Text = playedWord.Word + " - " + playedWord.Score.ToString() });
					}
					if (totalScore == 0 || wordsPlayed.Count == 0)
					{
						int x = 0;
					}
					if (m_currentWordTiles.Count == 7)
					{
						scoreText.Inlines.Add(new Run() { Text = "\r\n + All Tiles Bonus - 50" });
					}
					if (wordsPlayed.Count > 1 || m_currentWordTiles.Count == 7)
					{
						scoreText.Inlines.Add(new Run() { Text = "\r\nTotal Score - " + totalScore.ToString() });
					}
					PlayersWords.Blocks.Add(scoreText);

					foreach (TileControl tile in m_currentWordTiles)
					{
						tile.TileStatus = eTileState.Played;
						tile.ManipulationStarting -= DragLetter_ManipulationStarting;
						tile.ManipulationDelta -= DragLetter_ManipulationDelta;
						tile.ManipulationCompleted -= DragLetter_ManipulationCompleted;
						m_playedTiles.Add(tile);
						m_boardTiles[tile.GridX, tile.GridY] = tile;
					}

					m_playersScore += totalScore;
					PlayersScoreTextBlock.Text = m_playersScore.ToString();
					m_currentWordTiles.Clear();
					m_turnState = eTurnState.ComputersTurn;

					for (int i = 7 - m_panelTiles.Count; i > 0; i--)
					{
						if (m_letterBag.Count > 0)
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
						else
						{
							break;
						}
					}
					TileRemainingTextBlock.Text = "Tiles Remaining : " + m_letterBag.Count.ToString();
				}
			}
		}

		private void PassButton_Click(object sender, RoutedEventArgs e)
		{
			m_turnState = eTurnState.ComputersTurn;
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
		}

		void GameTimer_Tick(object sender, object e)
		{
			// Check for GAME OVER
			if (m_letterBag.Count == 0)
			{
				if ((m_panelTiles.Count == 0 && m_currentWordTiles.Count == 0) || m_computersTiles.Count == 0)
				{
					m_turnState = eTurnState.GameOver;
					if (m_computersScore > m_playersScore)
					{
						MessageTextBox.Text = "GAME OVER - The computer won this round.";
					}
					else
					{
						MessageTextBox.Text = "GAME OVER - You beat the computer.";
					}
					MessageTextBox.Visibility = Visibility.Visible;
					PlayButton.Visibility = Visibility.Collapsed;
					RecallLettersButton.Visibility = Visibility.Collapsed;
					PassButton.Visibility = Visibility.Collapsed;
					SwapLettersButton.Visibility = Visibility.Collapsed;
					ShuffleLettersButton.Visibility = Visibility.Collapsed;
					NewGameButton.Visibility = Visibility.Visible;
				}
			}

			if (m_turnState != eTurnState.GameOver && 
				DateTime.Now.Subtract(m_messageDisplayTime).TotalSeconds > 5 && 
				MessageTextBox.Visibility == Visibility.Visible)
			{
				MessageTextBox.Visibility = Visibility.Collapsed;
			}

			if (m_turnState == eTurnState.ComputersTurn)
			{
				m_turnState = eTurnState.ComputerIsThinking;
				MessageTextBox.Text = "Thinking...";
				MessageTextBox.Visibility = Visibility.Visible;
				m_messageDisplayTime = DateTime.Now;
				ComputerThinkingProgressRing.IsActive = true;

				PlayComputersTurn();
			}
			else if (m_turnState == eTurnState.PlayersTurn)
			{
			}
			else
			{
			}
		}

		private async void PlayComputersTurn()
		{
			if (DetermineComputersWord())
			{
				ComputerThinkingProgressRing.IsActive = false;
				m_turnState = eTurnState.PlayersTurn;
			}
			else
			{
				Paragraph scoreText = new Paragraph();
				scoreText.Inlines.Add(new Run() { Text = "Exchanged Tiles" });
				ComputersWords.Blocks.Add(scoreText);

				MessageTextBox.Text = "Computer could not find a word. Exchanging letters.";
				MessageTextBox.Visibility = Visibility.Visible;
				m_messageDisplayTime = DateTime.Now;

				for (int i = 0; i < m_computersTiles.Count; i++)
				{
					m_computersTiles[i].TileStatus = eTileState.InBag;
					m_letterBag.Add(m_computersTiles[i]);
				}

				m_computersTiles.Clear();
				for (int i = 0; i < 7; i++)
				{
					if (m_letterBag.Count > 0)
					{
						int nextLetter = m_randomiser.Next(m_letterBag.Count - 1);

						TileControl tile = m_letterBag[nextLetter];

						tile.TileStatus = eTileState.OnComputerPanel;
						m_computersTiles.Add(tile);

						m_letterBag.RemoveAt(nextLetter);
					}
				}

				ComputerThinkingProgressRing.IsActive = false;
				m_turnState = eTurnState.PlayersTurn;
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
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			m_computersWordFound = false;

			List<ExistingPlay> playableSpaces = GetPlayableSpaces(m_computersTiles.Count);			
			List<PossiblePlay> possiblePlays = FindPossiblePlays(m_computersTiles, playableSpaces);

			if (playableSpaces.Count == 0)
			{
				m_computersWordFound = PlaceComputersFirstWord(m_computersTiles);
			}
			else
			{
				if (possiblePlays.Count > 0)
				{
					PossiblePlay playToUse = possiblePlays[0];

					foreach (PossiblePlay play in possiblePlays)
					{
						if (play.TotalValue > playToUse.TotalValue)
						{
							playToUse = play;
						}
					}

					Paragraph scoreText = new Paragraph();

					foreach (WordAndScore playedWord in playToUse.WordsCreated)
					{
						m_allWordsPlayed.Add(playedWord.Word);

						if (scoreText.Inlines.Count > 0)
						{
							scoreText.Inlines.Add(new Run() { Text = "\r\n + " });
						}
						scoreText.Inlines.Add(new Run() { Text = playedWord.Word + " - " + playedWord.Score.ToString() });
					}
					if(playToUse.TilesToPlay.Count == 7)
					{
						scoreText.Inlines.Add(new Run() { Text = "\r\n + All Tiles Bonus - 50" });
					}
					if (playToUse.WordsCreated.Count > 1 || playToUse.TilesToPlay.Count == 7)
					{
						scoreText.Inlines.Add(new Run() { Text = "\r\nTotal Score - " + playToUse.TotalValue.ToString() });
					}
					ComputersWords.Blocks.Add(scoreText);

					m_computersScore += playToUse.TotalValue;
					ComputersScoreTextBlock.Text = m_computersScore.ToString();

					// If we get to here then the word should fit successfully.
					for (int index = 0; index < playToUse.TilesToPlay.Count; index++)
					{
						TileControl tile = playToUse.TilesToPlay[index];
						PlaceTileOnBoard(tile, playToUse.XValues[index], playToUse.YValues[index]);
						m_computersTiles.Remove(tile);
						tile.TileStatus = eTileState.Played;
						m_playedTiles.Add(tile);
						m_boardTiles[tile.GridX, tile.GridY] = tile;
					}
					m_computersWordFound = true;
				}
				else
				{
					m_computersWordFound = false;
				}
			}

			for (int i = 7 - m_computersTiles.Count; i > 0; i--)
			{
				if (m_letterBag.Count > 0)
				{
					int nextLetter = m_randomiser.Next(m_letterBag.Count - 1);

					TileControl tile = m_letterBag[nextLetter];

					tile.TileStatus = eTileState.OnComputerPanel;
					m_computersTiles.Add(tile);

					m_letterBag.RemoveAt(nextLetter);
				}
				else
				{
					break;
				}			
			}
			TileRemainingTextBlock.Text = "Tiles Remaining : " + m_letterBag.Count.ToString();
#if DEBUG
			DateTime end = DateTime.Now;

			TimeSpan duration = end.Subtract(start);
			MessageTextBox.Text = duration.TotalSeconds.ToString() + " Seconds";
			MessageTextBox.Visibility = Visibility.Visible;
			m_messageDisplayTime = DateTime.Now;
#endif 
			return m_computersWordFound;
		}

		public List<string> FindPossibleWords(string letters)
		{
			List<string> possibleWords = new List<string>();
			List<string> keysChecked = new List<string>();

			// Find all words using 1 to all of the players letters.
			for (int numberOfCharacters = 1; numberOfCharacters <= letters.Length; numberOfCharacters++)
			{
				for (int i = 0; i <= letters.Length - numberOfCharacters; i++)
				{
					// Select the first n letters
					string testCharacters = letters.Substring(i, numberOfCharacters);
					string key = SortLetters(testCharacters);

					// If the first n letters make up valid words then add them to the list.
					if (!keysChecked.Contains(key))
					{
						keysChecked.Add(key);
						if (m_wordLookup.ContainsKey(key))
						{
							foreach (string actualWord in m_wordLookup[key])
							{
								possibleWords.Add(actualWord);
							}
						}
					}

					// Now add each of the remaining letters in turn and check to see if that makes a word.
					for (int j = i + numberOfCharacters; j < letters.Length; j++)
					{
						key = SortLetters(testCharacters + letters.Substring(j, 1));

						if (!keysChecked.Contains(key))
						{
							keysChecked.Add(key);
							if (m_wordLookup.ContainsKey(key))
							{
								foreach (string actualWord in m_wordLookup[key])
								{
									possibleWords.Add(actualWord);
								}
							}
						}
					}
				}
			}

			// Sort all words (longest first) and return the list.
			return SortByLength(possibleWords);
		}

		private bool PlaceComputersFirstWord(List<TileControl> tiles)
		{
			bool result = false;
			string letters = string.Empty;
			string longestWord = string.Empty;

			foreach(TileControl tile in tiles)
			{
				if (tile.Letter != BLANK)       // TODO!!
				{
					letters += tile.Letter;
				}
			}

			foreach (string word in FindPossibleWords(letters))
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
				int score = 0;
				int wordMultiplier = 1;

				for (int i = 0; i < longestWord.Length; i++)
				{
					string letter = longestWord.Substring(i, 1);
					TileControl tilePlayed = null;
										
					foreach (TileControl tile in m_computersTiles)
					{
						if (tile.Letter == letter)
						{
							PlaceTileOnBoard(tile, x, y);
							score += ScoreTilePlacement(tile, ref wordMultiplier);
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

				score *= wordMultiplier;

				Paragraph scoreText = new Paragraph();
				scoreText.Inlines.Add(new Run() { Text = longestWord + " - " + score.ToString() });
				ComputersWords.Blocks.Add(scoreText);

				m_computersScore += score;
				ComputersScoreTextBlock.Text = m_computersScore.ToString();

				result = true;
			}

			return result;
		}
		
		private void FillLetterBag()
		{
			m_letterBag.Clear();

			foreach (ScrabbleLetterConfig scrabbleLetter in s_scrabbleLetters)
			{
				for (int i = 0; i < scrabbleLetter.NumberOf; i++)
				{
					TileControl tile = new TileControl(scrabbleLetter.Letter, scrabbleLetter.LetterValue);
					tile.Visibility = Visibility.Collapsed;
					tile.RenderTransform = new TranslateTransform();
					tile.HorizontalAlignment = HorizontalAlignment.Left;
					tile.VerticalAlignment = VerticalAlignment.Top;
					MainGrid.Children.Add(tile);

					m_letterBag.Add(tile);
				}
			}
		}

		private void StartGame()
		{
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
				MessageTextBox.Visibility = Visibility.Visible;
				m_messageDisplayTime = DateTime.Now;
			}
			else
			{
				m_turnState = eTurnState.ComputersTurn;
				MessageTextBox.Text = "The Computer won the draw.";
				MessageTextBox.Visibility = Visibility.Visible;
				m_messageDisplayTime = DateTime.Now;
			}

			m_gameStartTime = DateTime.Now;
			m_gameTimer.Start();
		}

		private void TileMoved()
		{
			if (m_panelTiles.Count == 7)
			{
				PlayButton.Visibility = Visibility.Collapsed;
				RecallLettersButton.Visibility = Visibility.Collapsed;
				PassButton.Visibility = Visibility.Visible;
				SwapLettersButton.Visibility = Visibility.Visible;
			}
			else
			{
				PlayButton.Visibility = Visibility.Visible;
				RecallLettersButton.Visibility = Visibility.Visible;
				PassButton.Visibility = Visibility.Collapsed;
				SwapLettersButton.Visibility = Visibility.Collapsed;
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

		private List<WordAndScore> GetPlayedWords(bool horizontal, List<TileControl> currentWordTiles)
		{
			WordAndScore playedWord = null;
			List<WordAndScore> playedWords = new List<WordAndScore>();
			List<TileControl> playedTiles = SortCurrentWordTiles(horizontal, currentWordTiles);

			if (horizontal)
			{
				playedWord = GetMainWordPlayedHorizontal(playedTiles);
			}
			else
			{
				playedWord = GetMainWordPlayedVertical(playedTiles);
			}

			if (playedWord.Word.Length > 0)
			{
				playedWords.Add(playedWord);
				if (horizontal)
				{
					GetVerticalWordsForWordPlayedHorizontal(playedTiles, ref playedWords);
				}
				else
				{
					GetHorizontalWordsForWordPlayedVertical(playedTiles, ref playedWords);
				}
			}

			return playedWords;
		}

		private int ScoreTilePlacement(TileControl tile, ref int wordMultiplier)
		{
			int score = tile.LetterValue;

			// If this is a newly placed tile then multipliers apply.
			if (tile.TileStatus != eTileState.Played)
			{
				eScrabbleScores placeMultiplier = m_boardScores[tile.GridX, tile.GridY];

				switch (placeMultiplier)
				{
					case eScrabbleScores.DoubleLetterValue:
						score = tile.LetterValue * 2;
						break;
					case eScrabbleScores.DoubleWordValue:
						score = tile.LetterValue;
						wordMultiplier *= 2;
						break;
					case eScrabbleScores.LetterValue:
						score = tile.LetterValue;
						break;
					case eScrabbleScores.TripleLetterValue:
						score = tile.LetterValue * 3;
						break;
					case eScrabbleScores.TripleWordValue:
						score = tile.LetterValue;
						wordMultiplier *= 3;
						break;
				}
			}

			return score;
		}

		private WordAndScore GetMainWordPlayedHorizontal(List<TileControl> playedTiles)
		{
			int index;
			int lastX = -1;
			int wordMultiplier = 1;
			WordAndScore playedWord = new WordAndScore();

			// Find the word that has been played.
			playedWord.Word = playedTiles[0].Letter;
			playedWord.Score = ScoreTilePlacement(playedTiles[0], ref wordMultiplier);

			// Prepend any letters that appear before the played word.
			if (playedTiles[0].GridX > 0)
			{
				int previousX = playedTiles[0].GridX - 1;
				while (previousX >= 0 && m_boardTiles[previousX, playedTiles[0].GridY] != null)
				{
					playedWord.Word = m_boardTiles[previousX, playedTiles[0].GridY].Letter + playedWord.Word;
					playedWord.Score += ScoreTilePlacement(m_boardTiles[previousX, playedTiles[0].GridY], ref wordMultiplier);
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
							playedWord.Word += m_boardTiles[X, playedTiles[index].GridY].Letter;
							playedWord.Score += ScoreTilePlacement(m_boardTiles[X, playedTiles[index].GridY], ref wordMultiplier);
						}
						else
						{
							MessageTextBox.Text = "There seems to be a gap in your word!";
							MessageTextBox.Visibility = Visibility.Visible;
							m_messageDisplayTime = DateTime.Now;
							playedWord.Word = string.Empty;
							playedWord.Score = 0;
							break;
						}
					}
				}

				// Add the current tile.
				playedWord.Word += playedTiles[index].Letter;
				playedWord.Score += ScoreTilePlacement(playedTiles[index], ref wordMultiplier);

				// Save the last X value;
				lastX = playedTiles[index].GridX;
			}

			// Append any letters that appear after the played word.
			if (lastX < 15)
			{
				int nextX = lastX + 1;
				while (nextX < 15 && m_boardTiles[nextX, playedTiles[0].GridY] != null)
				{
					playedWord.Word += m_boardTiles[nextX, playedTiles[0].GridY].Letter;
					playedWord.Score += ScoreTilePlacement(m_boardTiles[nextX, playedTiles[0].GridY], ref wordMultiplier);
					nextX++;
				}
			}

			playedWord.Score *= wordMultiplier;

			return playedWord;
		}

		private WordAndScore GetMainWordPlayedVertical(List<TileControl> playedTiles)
		{
			int index;
			int lastY = -1;
			int wordMultiplier = 1;
			WordAndScore playedWord = new WordAndScore();

			// Find the word that has been played.
			playedWord.Word = playedTiles[0].Letter;
			playedWord.Score = ScoreTilePlacement(playedTiles[0], ref wordMultiplier);

			// Prepend any letters that appear before the played word.
			if (playedTiles[0].GridY > 0)
			{
				int previousY = playedTiles[0].GridY - 1;
				while (previousY >= 0 && m_boardTiles[playedTiles[0].GridX, previousY] != null)
				{
					playedWord.Word = m_boardTiles[playedTiles[0].GridX, previousY].Letter + playedWord.Word;
					playedWord.Score += ScoreTilePlacement(m_boardTiles[playedTiles[0].GridX, previousY], ref wordMultiplier);
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
							playedWord.Word += m_boardTiles[playedTiles[index].GridX, Y].Letter;
							playedWord.Score += ScoreTilePlacement(m_boardTiles[playedTiles[index].GridX, Y], ref wordMultiplier);
						}
						else
						{
							MessageTextBox.Text = "There seems to be a gap in your word!";
							MessageTextBox.Visibility = Visibility.Visible;
							m_messageDisplayTime = DateTime.Now;
							playedWord.Word = string.Empty;
							playedWord.Score = 0;
							break;
						}
					}
				}
				// Add the current tile.
				playedWord.Word += playedTiles[index].Letter;
				playedWord.Score += ScoreTilePlacement(playedTiles[index], ref wordMultiplier);

				// Save the last Y value;
				lastY = playedTiles[index].GridY;
			}

			// Append any letters that appear after the played word.
			if (lastY < 15)
			{
				int nextY = lastY + 1;
				while (nextY < 15 && m_boardTiles[playedTiles[0].GridX, nextY] != null)
				{
					playedWord.Word += m_boardTiles[playedTiles[0].GridX, nextY].Letter;
					playedWord.Score += ScoreTilePlacement(m_boardTiles[playedTiles[0].GridX, nextY], ref wordMultiplier);
					nextY++;
				}
			}

			playedWord.Score *= wordMultiplier;

			return playedWord;
		}

		private void GetVerticalWordsForWordPlayedHorizontal(List<TileControl> playedTiles, ref List<WordAndScore> playedWords)
		{
			for (int tileIndex = 0; tileIndex < playedTiles.Count; tileIndex++)
			{
				int wordMultiplier = 1;
				WordAndScore playedWord = new WordAndScore();
				int X = playedTiles[tileIndex].GridX;
				int Y = playedTiles[tileIndex].GridY;

				// It there are no tile immediately above or below the current tile then skip to the next played tile.
				if ((Y == 0 || (Y > 0 && m_boardTiles[X, Y - 1] == null)) &&
					(Y == 14 || (Y < 14 && m_boardTiles[X, Y + 1] == null)))
				{
					continue;
				}
				// If there is a tile above the placed tile...
				if (Y > 0 && m_boardTiles[X, Y - 1] != null)
				{
					// Work up the column until an empty space is found.
					while (Y > 0 && m_boardTiles[X, Y - 1] != null)
					{
						Y--;
					}
					// Now work back down to build up the word to the played tile.
					while (Y < 14 && m_boardTiles[X, Y] != null)
					{
						playedWord.Word += m_boardTiles[X, Y].Letter;
						playedWord.Score += ScoreTilePlacement(m_boardTiles[X, Y], ref wordMultiplier);
						Y++;
					}
				}
				// Now add the tile we are placing.
				playedWord.Word += playedTiles[tileIndex].Letter;
				playedWord.Score += ScoreTilePlacement(playedTiles[tileIndex], ref wordMultiplier);
				Y++;
				// Now check after the played tile as well.
				while (Y < 15 && m_boardTiles[X, Y] != null)
				{
					playedWord.Word += m_boardTiles[X, Y].Letter;
					playedWord.Score += ScoreTilePlacement(m_boardTiles[X, Y], ref wordMultiplier);
					Y++;
				}

				if (playedWord.Word.Length > 0)
				{
					playedWord.Score *= wordMultiplier;
					playedWords.Add(playedWord);
				}
			}
		}

		private void GetHorizontalWordsForWordPlayedVertical(List<TileControl> playedTiles, ref List<WordAndScore> playedWords)
		{
			for (int tileIndex = 0; tileIndex < playedTiles.Count; tileIndex++)
			{
				int wordMultiplier = 1;
				WordAndScore playedWord = new WordAndScore();
				int X = playedTiles[tileIndex].GridX;
				int Y = playedTiles[tileIndex].GridY;

				// It there are no tile immediately above or below the current tile then skip to the next played tile.
				if ((X == 0 || (X > 0 && m_boardTiles[X - 1, Y] == null)) &&
					(X == 14 || (X < 14 && m_boardTiles[X + 1, Y] == null)))
				{
					continue;
				}

				if (X > 0 && m_boardTiles[X - 1, Y] != null)
				{
					// Work back along the row until an empty space is found.
					while (X > 0 && m_boardTiles[X - 1, Y] != null)
					{
						X--;
					}
					// Now work forward to build up the word to the played tile.
					while (X < 14 && m_boardTiles[X, Y] != null)
					{
						playedWord.Word += m_boardTiles[X, Y].Letter;
						playedWord.Score += ScoreTilePlacement(m_boardTiles[X, Y], ref wordMultiplier);
						X++;
					}
				}
				// Now add the tile we are placing.
				playedWord.Word += playedTiles[tileIndex].Letter;
				playedWord.Score += ScoreTilePlacement(playedTiles[tileIndex], ref wordMultiplier);
				X++;
				// Now check after the played tile as well.
				while (X < 15 && m_boardTiles[X, Y] != null)
				{
					playedWord.Word += m_boardTiles[X, Y].Letter;
					playedWord.Score += ScoreTilePlacement(m_boardTiles[X, Y], ref wordMultiplier);
					X++;
				}

				if (playedWord.Word.Length > 0)
				{
					playedWord.Score *= wordMultiplier;
					playedWords.Add(playedWord);
				}
			}
		}

		public List<ExistingPlay> GetPlayableSpaces(int maxLetters)
		{
			ExistingPlay currentPlay = null;
			List<ExistingPlay> playableSpaces = new List<ExistingPlay>();

			// Find all playable spaces across the board.
			for (int y = 0; y < 15; y++)
			{
				currentPlay = null;
				for (int x = 0; x < 15; x++)
				{
					if (m_boardTiles[x, y] != null)
					{
						if (currentPlay == null)
						{
							currentPlay = new ExistingPlay() { StartTile = m_boardTiles[x, y], Horizontal = true };
							playableSpaces.Add(currentPlay);
						}
						currentPlay.ExistingLetters += m_boardTiles[x, y].Letter;
					}
					else 
					{
						currentPlay = null;
					}
				}
			}

			for (int x = 0; x < 15; x++)
			{
				currentPlay = null;
				for (int y = 0; y < 15; y++)
				{
					if (m_boardTiles[x, y] != null)
					{
						if (currentPlay == null)
						{
							currentPlay = new ExistingPlay() { StartTile = m_boardTiles[x, y], Horizontal = false };
							playableSpaces.Add(currentPlay);
						}
						currentPlay.ExistingLetters += m_boardTiles[x, y].Letter;
					}
					else
					{
						currentPlay = null;
					}
				}
			}
			
			return playableSpaces;
		}

		public List<PossiblePlay> FindPossiblePlays(List<TileControl> playersTiles, List<ExistingPlay> playableSpaces)
		{
			string playersLetters = "";
			List<string> keysChecked = new List<string>();
			List<PossiblePlay> possiblePlays = new List<PossiblePlay>();

			foreach (TileControl tile in playersTiles)
			{
				if (tile.Letter != BLANK)		// TODO!!
				{
					playersLetters += tile.Letter;
				}
			}

#if DEBUG
			MessageTextBox.Text = playersLetters;
			MessageTextBox.Visibility = Visibility.Visible;
			m_messageDisplayTime = DateTime.Now;
#endif

			foreach (ExistingPlay existing in playableSpaces)
			{
				// Find all words using 1 to all of the players letters.
				for (int numberOfCharacters = 1; numberOfCharacters <= playersLetters.Length; numberOfCharacters++)
				{
					for (int i = 0; i <= playersLetters.Length - numberOfCharacters; i++)
					{
						// Select the first n letters
						string testCharacters = playersLetters.Substring(i, numberOfCharacters);
						string key = SortLetters(existing.ExistingLetters + testCharacters);

						// Start by checking the existing letters with the first n characters of the players letters.
						FindPlays(key, existing, playersTiles, ref keysChecked, ref possiblePlays);

						// Now check each of the remaining letters in turn and check to see if that makes a word.
						for (int j = i + numberOfCharacters; j < playersLetters.Length; j++)
						{
							key = SortLetters(existing.ExistingLetters + testCharacters + playersLetters.Substring(j, 1));

							FindPlays(key, existing, playersTiles, ref keysChecked, ref possiblePlays);
						}
					}
				}
			}

			return possiblePlays;
		}

		private void FindPlays(string key, ExistingPlay existing, List<TileControl> playersTiles, ref List<string> keysChecked, ref List<PossiblePlay> possiblePlays)
		{
			if (!keysChecked.Contains(key))
			{
				keysChecked.Add(key);
				if (m_wordLookup.ContainsKey(key))
				{
					foreach (string actualWord in m_wordLookup[key])
					{
						if (actualWord.Contains(existing.ExistingLetters))
						{
							// It is possible that the existing letter(s) occur multiple times in the word that has been found.
							// It is necessary that each instance be checked to see if the proposed word fits on the board.
							List<int> indeciseOfExisting = IndicesOfStringInString(actualWord, existing.ExistingLetters);

							foreach (int indexOfExistingPlay in indeciseOfExisting)
							{
								List<TileControl> tilesToPlay = new List<TileControl>();
								if (HasRequiredLetters(actualWord, playersTiles, existing.ExistingLetters, indexOfExistingPlay, ref tilesToPlay))
								{
									int tilesIndex = 0;
									bool wordFits = true;
									PossiblePlay play = new PossiblePlay() { Word = actualWord, Interaction = existing };
									
									if (existing.Horizontal)
									{
										for(int i = 0; i < indexOfExistingPlay; i++)
										{
											int X = existing.StartTile.GridX - indexOfExistingPlay + i;
											int Y = existing.StartTile.GridY;

											if (X < 0 || X > 14 || m_boardTiles[X,Y] != null)
											{
												wordFits = false;
												break;
											}

											tilesToPlay[tilesIndex].GridX = X;
											tilesToPlay[tilesIndex].GridY = Y;
											play.XValues.Add(X);
											play.YValues.Add(Y);

											tilesIndex++;
										}
										if (wordFits)
										{
											for (int i = tilesIndex; i < tilesToPlay.Count; i++)
											{
												int X = existing.StartTile.GridX + existing.ExistingLetters.Length - indexOfExistingPlay + i;
												int Y = existing.StartTile.GridY;

												if (X < 0 || X > 14 || m_boardTiles[X, Y] != null)
												{
													wordFits = false;
													break;
												}
												tilesToPlay[tilesIndex].GridX = X;
												tilesToPlay[tilesIndex].GridY = Y;
												play.XValues.Add(X);
												play.YValues.Add(Y);

												tilesIndex++;
											}
										}
									}
									else
									{
										for (int i = 0; i < indexOfExistingPlay; i++)
										{
											int X = existing.StartTile.GridX;
											int Y = existing.StartTile.GridY - indexOfExistingPlay + i;

											if (Y < 0 || Y > 14 || m_boardTiles[X, Y] != null)
											{
												wordFits = false;
												break;
											}
											tilesToPlay[tilesIndex].GridX = X;
											tilesToPlay[tilesIndex].GridY = Y;
											play.XValues.Add(X);
											play.YValues.Add(Y);

											tilesIndex++;
										}
										if (wordFits)
										{
											for (int i = tilesIndex; i < tilesToPlay.Count; i++)
											{
												int X = existing.StartTile.GridX;
												int Y = existing.StartTile.GridY + existing.ExistingLetters.Length - indexOfExistingPlay + i;

												if (Y < 0 || Y > 14 || m_boardTiles[X, Y] != null)
												{
													wordFits = false;
													break;
												}
												tilesToPlay[tilesIndex].GridX = X;
												tilesToPlay[tilesIndex].GridY = Y;
												play.XValues.Add(X);
												play.YValues.Add(Y);

												tilesIndex++;
											}
										}
									}

									if (wordFits)
									{
										// Check that all words created by the placement of this word are valid.
										List<WordAndScore> wordsPlayed = GetPlayedWords(existing.Horizontal, tilesToPlay);

										int totalScore = tilesToPlay.Count == 7 ? 50 : 0;
										bool allWordsOk = true;
										foreach (WordAndScore playedWord in wordsPlayed)
										{
											if (playedWord.Word.Length > 0)
											{
												if (!m_words.Contains(playedWord.Word))
												{
													allWordsOk = false;
													break;
												}
												totalScore += playedWord.Score;
											}
										}
										if (allWordsOk)
										{
											play.TilesToPlay = tilesToPlay;
											play.WordsCreated = wordsPlayed;
											play.TotalValue = totalScore;
											possiblePlays.Add(play);
										}
									}
								}
							}
						}
					}
				}
			}
		}

		// Checks to see if the desiredWord can be constructed from the playersLetters and the existingLetters.
		private bool HasRequiredLetters(string desiredWord, List<TileControl> playersTiles, string existingLetters, int indexOfExisting, ref List<TileControl> tilesToPlay)
		{
			string remainder = desiredWord.Substring(0, indexOfExisting) +
								desiredWord.Substring(indexOfExisting + existingLetters.Length);
			TileControl[] playersTileArray = new TileControl[playersTiles.Count];
			playersTiles.CopyTo(playersTileArray);

			List<TileControl> manipulatableList = new List<TileControl>(playersTiles);

			foreach (char letter in remainder)
			{
				TileControl found = null;

				foreach (TileControl tile in manipulatableList)
				{
					if(tile.Letter[0] == letter)
					{
						found = tile;
						break;
					}
				}

				if(found != null)
				{
					manipulatableList.Remove(found);
					tilesToPlay.Add(found);
				}
				else
				{
					return false;
				}
			}

			return true;
		}
	}
}
