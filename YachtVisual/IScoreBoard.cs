﻿using System.Collections.Generic;

namespace YachtVisual
{
    interface IScoreBoard
    {
        IReadOnlyDictionary<string, IScorePlace> Places { get; }
        int TotalScore { get; }

        void Submit(string initial, int[] dices);
    }
}