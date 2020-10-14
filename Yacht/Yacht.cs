﻿using Disboard;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Disboard.Macro;

namespace Yacht
{
    class Yacht : IGame
    {
        private int _currentPlayerIndex = 0;
        private int[] __currentDices = { 0, 0, 0, 0, 0 };
        private int _currentRemainReroll = 0;

        private Random Random { get; } = new Random();
        private IReadOnlyDictionary<Player, IScoreBoard> ScoreBoards { get; }
        private SendType Send { get; }
        private SendImageType SendImage { get; }
        private RenderType Render { get; }
        private Action OnFinish { get; }

        private int[] CurrentDices
        {
            get => __currentDices;
            set
            {
                Debug.Assert(value.Length == 5);
                __currentDices = value;
            }
        }
        private IReadOnlyList<Player> Players => ScoreBoards.Keys.ToList();
        private Player? CurrentPlayer => _currentPlayerIndex != -1 ? Players[_currentPlayerIndex] : null;

        public Yacht(GameInitializeData initData)
        {
            Send = initData.Send;
            SendImage = initData.SendImage;
            Render = initData.Render;
            OnFinish = initData.OnFinish;
            var players = initData.Players.OrderBy(_ => Random.Next()).ToList();
            ScoreBoards = players.ToDictionary(_ => _, _ => new ScoreBoard() as IScoreBoard);
        }

        public async Task Start()
        {
            await Send("`명령어: R 23456, S 4k`");
            await StartTurn();
        }

        public async Task OnGroup(Player player, string message)
        {
            if (player == CurrentPlayer)
            {
                var split = message.Split();
                if (split.Length > 0 && split[0].ToLower() == "r")
                {
                    await Reroll(message);
                }
                else if (split.Length > 0 && split[0].ToLower() == "s")
                {
                    await Submit(message);
                }
            }
        }
        private async Task Reroll(string message)
        {
            if (_currentRemainReroll <= 0)
            {
                await Send(W("남은 리롤 기회가 없습니다. 점수를 적을 항목을 선택하세요. 예시: S 3k"));
                return;
            }
            var split = message.Split();
            if (split.Length != 2)
            {
                await Send(W("리롤할 주사위를 입력하세요. 예시: R 334"));
                return;
            }
            try
            {
                var dicesToReroll = split[1].Select(_ => int.Parse(_.ToString()));
                var newDices = CurrentDices.ToList(); //copy
                foreach (int diceToReroll in dicesToReroll)
                {
                    if (newDices.Contains(diceToReroll))
                    {
                        newDices.RemoveAt(newDices.LastIndexOf(diceToReroll));
                    }
                    else
                    {
                        throw new System.FormatException();
                    }
                }
                newDices.AddRange(Enumerable.Range(0, 5 - newDices.Count).Select(_ => Random.Next(6) + 1));
                CurrentDices = newDices.ToArray();
                _currentRemainReroll -= 1;
                await PrintTurn();
            }
            catch (System.FormatException)
            {
                await Send(W("리롤할 주사위를 다시 입력하세요. 예시: R 334"));
            }
        }

        private async Task Submit(string message)
        {
            Debug.Assert(CurrentPlayer != null);

            var split = message.Split();
            var scoreBoard = ScoreBoards[CurrentPlayer];
            if (split.Length != 2)
            {
                await Send(W("이니셜을 입력하세요.예시: S 3k"));
                return;
            }
            var initial = split[1];
            try
            {
                scoreBoard.Submit(initial, CurrentDices);
                await ProceedAndStartTurn();
            }
            catch (System.InvalidOperationException)
            {
                await Send(W("이미 점수를 채운 항목입니다."));
            }
            catch (CommandNotFoundException)
            {
                await Send(W("올바른 이니셜을 입력하세요. 예시: S 3k"));
            }
        }

        private async Task ProceedAndStartTurn()
        {
            if (ScoreBoards.Values.All(_ => _.Places.Values.All(_ => _.IsOpen == false)))
            {
                _currentPlayerIndex = -1;
                await SendImage(GetBoardImage());
                var highestScore = ScoreBoards.Values.Select(_ => _.TotalScore).OrderByDescending(_ => _).First();
                var winners = Players.Where(_ => ScoreBoards[_].TotalScore == highestScore).Select(_ => _.Name);
                var winnerString = winners.Count() > 1 ? "Winners: " : "Winner: ";
                winnerString += W(string.Join(", ", winners));
                await Send(winnerString);
                OnFinish();
            }
            else
            {
                _currentPlayerIndex += 1;
                if (_currentPlayerIndex >= Players.Count)
                {
                    _currentPlayerIndex = 0;
                }
                await StartTurn();
            }
        }

        private async Task StartTurn()
        {
            CurrentDices = Enumerable.Range(0, 5).Select(_ => Random.Next(6) + 1).ToArray();
            _currentRemainReroll = 2;
            await PrintTurn();
        }

        private async Task PrintTurn()
        {
            Debug.Assert(CurrentPlayer != null);

            await SendImage(GetBoardImage());

            var checkTexts = Enumerable.Range(0, 3).Reverse().Select(_ => _ < _currentRemainReroll).Select(_ => _ ? ":arrows_counterclockwise:" : ":ballot_box_with_check:");
            var checkString = string.Join(" ", checkTexts);
            var turnIndicator = $"{CurrentPlayer.Mention} {CurrentPlayer.Name}'s turn, Reroll: " + checkString;
            await Send(turnIndicator);

            var diceTextTemplates = new List<string> { ":zero:", ":one:", ":two:", ":three:", ":four:", ":five:", ":six:" };
            var diceTexts = CurrentDices.Select(_ => diceTextTemplates[_]);
            var diceString = string.Join(" ", diceTexts);
            await Send(diceString);
        }

        private Stream GetBoardImage()
            => Render(() => new ScoreBoardGrid(ScoreBoards, CurrentPlayer, CurrentDices));
    }
}
