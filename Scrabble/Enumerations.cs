﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scrabble
{
	public enum eTileState
	{
		Unknown,
		InBag,
		OnPlayerPanel,
		OnComputerPanel,
		ComposingNewWord,
		Played,
	}

	public enum eTurnState
	{
		Unknown,
		PlayersTurn,
		ComputersTurn,
		ComputerIsThinking,
		GameOver,
	}

	public enum eScrabbleScores
	{
		LetterValue,
		DoubleLetterValue,
		TripleLetterValue,
		DoubleWordValue,
		TripleWordValue,
	}
}
