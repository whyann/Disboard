using Disboard;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Documents.DocumentStructures;

namespace Gapos
{
    class Gapos : DisboardGameUsingDM
    {
        // 여기에 멤버변수를 선언하세요.
        int currentPlayerIndex = 0;
        int MapSize = 7;
        int GameTurn = 0; //게임의 상태를 결정. 1은 플레이어 메세지 입력 받음. 2는 계산 후 출력

        class Gamer
        {
            public DisboardPlayer player;
            int pCode;
            int position;
            int direction;
            int healthPoint;
            int stamina;
            int staminaMax;
            int defense;
            int defenseTemp;
            MapTile[] maps;
            Gamer[] gamers;
            List<int> actions;
            string name;
            Gapos gapos;

            public Gamer(DisboardPlayer initPlayer, int pos, int dir, int code, MapTile[] map, Gamer[] g, string name, Gapos gp)
            {
                player = initPlayer;
                position = pos;
                direction = dir;
                pCode = code;
                healthPoint = 8;
                stamina = 1;
                staminaMax = 3;
                defense = 0;
                defenseTemp = 0;
                maps = map;
                gamers = g;
                maps[pos].AddPlayer(code);
                actions = new List<int>();
                this.name = name;
                gapos = gp;
            }

            public int Move(int pos)//pos만큼 이동. 맵 밖으로 나가지는 않는다.
            {
                int newpos = position;
                newpos += direction * pos;
                int mapSize = maps.Length;
                if (newpos >= mapSize)
                {
                    maps[position].RemovePlayer(pCode);
                    position = mapSize - 1;
                    maps[position].AddPlayer(pCode);
                    return 1;//맵끝
                }
                else if (newpos < 0)
                {
                    maps[position].RemovePlayer(pCode);
                    position = 0;
                    maps[position].AddPlayer(pCode);
                    return 1;//맵끝
                }
                maps[position].RemovePlayer(pCode);
                position = newpos;
                maps[position].AddPlayer(pCode);
                return 0;//제대로 이동함
            }

            public int MoveFront()//앞으로 이동, 상대방을 밀치거나 이동 후 몸통 박치기 판정까지 들어있다.
            {
                //상대방과 같은 위치에 있으면 상대방을 밀친다.
                if (maps[position].CheckOtherPlayer(pCode))
                {
                    if (gamers[maps[position].GetOtherPlayer(pCode)].Move(-1) == 0)
                        return 3;//상대방을 밀쳤다.
                    return 3;//상대방을 밀쳤지만 벽에 부딫였다.
                }
                //앞으로 한 칸 이동
                StaminaChange(1);
                if (Move(1) == 0)
                {
                    //이동 후 상대방과 같은 위치에 있으면 몸통박치기 판정(지금은 없고)
                    if (maps[position].CheckOtherPlayer(pCode))
                        return 0;//몸통박치기
                    return 0;//앞으로 이동했다.
                }
                return 1;//이동하지 못했다.   
            }
            public int MoveBack()//뒤로 이동, 아직 뒤로 이동에는 상대방과의 충돌을 고려하지 않았다.
            {
                StaminaChange(1);
                if (Move(-1) == 0)
                    return 0;//뒤로 잘 이동했다.
                return 1;//벽에 걸렸다.
            }
            public int StaminaChange(int sp) //스태미너를 sp만큼 변화시키고 변화량을 return
            {
                int newStamina = stamina + sp;
                int dif = 0;
                if (newStamina > staminaMax)
                {
                    dif = staminaMax - stamina;
                    stamina = staminaMax;
                }
                else if (newStamina < 0)
                {
                    dif = -stamina;
                    stamina = 0;
                }
                else
                {

                    dif = sp;
                    stamina = newStamina;
                }
                return dif;
            }
            public int Damaged(int atk)//atk의 피해를 받고 피해량을 return;
            {
                int dif = defense + defenseTemp;
                if (atk > dif)
                {
                    dif = atk - dif;
                    if (healthPoint >= dif)
                    {
                        healthPoint -= dif;
                    }
                    else
                    {
                        dif = healthPoint;
                        healthPoint = 0;
                    }
                    return dif;
                }
                return 0;
            }
            public int Attack(int atk, int atkSp, int pos)//pos만큼 떨어진 위치에 atk 체력 피해, atkSp 스태미너 피해 피해량 리턴. 맞추지 못하면 -1리턴
            {
                int newpos = pos * direction;
                newpos += position;
                if (newpos >= maps.Length || newpos < 0)
                {
                    return -1;//빗나감
                }
                if (maps[newpos].CheckOtherPlayer(pCode))
                {
                    int damage = 0;
                    damage = gamers[maps[newpos].GetOtherPlayer(pCode)].Damaged(atk);
                    gamers[maps[newpos].GetOtherPlayer(pCode)].StaminaChange(-atkSp);
                    return damage;//맞았다!
                }
                return -1; //빗나감
            }
            public int DoAction()//actions 첫번째에 저장된 숫자에 따라 지정된 행동을 한다.
            {
                if (actions.Count == 0)
                {
                    return -1;//저장된 행동 없어서 아무것도 안 함
                }
                int action = actions.First();
                actions.RemoveAt(0);
                int result = 0;
                switch (action)
                {
                    case 0://제자리에 있기
                        if (StaminaChange(1) == 1)
                            gapos.Send($"{name}는 제자리에서 스태미너를 1 회복했다.");
                        else
                            gapos.Send($"{name}는 아무것도 하지 않았다.");
                        return 0;
                    case 1://전진
                        result = MoveFront();
                        if (result == 3)
                            gapos.Send($"{name}는 상대방을 밀쳤다.");
                        else if (result == 0)
                            gapos.Send($"{name}는 앞으로 이동했다.");
                        else
                            gapos.Send($"{name}는 이동하지 못했다.");
                        return 1;
                    case 2://후진
                        result = MoveBack();
                        if (result == 0)
                            gapos.Send($"{name}는 뒤로 후퇴했다.");
                        else
                            gapos.Send($"{name}는 벽에 막혀 이동하지 못했다.");
                        return 2;
                    case 3://공격1
                        if (StaminaChange(-1) == 0)
                            gapos.Send($"{name}는 스태미너가 없어 공격하지 못했다.");
                        else
                        { 
                            result = Attack(1, 0, 0);
                            if(result == -1)
                                gapos.Send($"{name}의 공격이 빗나갔다.");
                            else
                                gapos.Send($"{name}는 공격하여 {result}의 피해를 입혔다.");
                        }
                        return 3;
                    case 4://공격2
                        Attack(1, 0, 1);
                        return 4;
                    default:
                        return 0;
                }
            }
            public void SetActions(IEnumerable<int> act)
            {
                actions.AddRange(act);
            }
            public int GetHealthPoint()
            {
                return healthPoint;
            }
            public int GetStamina()
            {
                return stamina;
            }
        }

        class MapTile
        {
            int position;
            List<int> onPlayers;

            public MapTile(int pos) 
            {
                position = pos;
                onPlayers = new List<int>();
            }
            public int GetPlayerNumber()
            {
                return onPlayers.Count;
            }
            public bool CheckOtherPlayer(int pCode)
            {
                if(onPlayers.Count == 0)
                {
                    return false;
                }
                else if(onPlayers.Count == 1 && onPlayers[0] == pCode)
                {
                    return false;
                }
                return true;
            }
            public int GetOtherPlayer(int pCode)
            {
                if (onPlayers[0] != pCode)
                {
                    return onPlayers[0];
                }
                return onPlayers[1];
            }

            public void AddPlayer(int pCode)
            {
                onPlayers.Add(pCode);
            }

            public void RemovePlayer(int pCode)
            {
                onPlayers.Remove(pCode);
            }
        }

        Gamer[] players; //pCode로 개별 플레이어에 접근 예) players[pCode]
        MapTile[] maps; //position이 곧 배열 넘버

        public Gapos(DisboardGameInitData initData) : base(initData)
        {
            // 게임이 시작되었을 때의 로직을 입력합니다.
            //맵을 만들어요
            maps = new MapTile[MapSize];
            for (int i = 0; i < MapSize; i++)
            {
                maps[i] = new MapTile(i);
            }
            //참가자가 한 명인가요?
            if (InitialPlayers.Count != 1)
            {
                Send("테스트 중이라 한 명만 참가할 수 있어요");
                string tets = "";
                tets += InitialPlayers.Count;
                Send(tets);

                OnFinish();
            }
            //플레이어를 만들어요
            players = new Gamer[2];
            players[0] = new Gamer(InitialPlayers[0], 1, 1, 0, maps, players, "P1", this);
            players[1] = new Gamer(null, 4, -1, 1, maps, players, "P2", this);

            //플레이어 메세지를 입력 받습니다.
            GameTurn = 1;

            // Send는 그룹 채팅에 메시지를 보내는 함수입니다.
            // SendImage, SendImages 함수도 사용할 수 있습니다.
            Send($"첫번째 플레이어: {players[0].player.Mention}");


            // 게임을 종료하려면 OnFinish()를 호출합니다.
            // 인원 수가 안맞는 등의 이유로 게임을 시작할 수 없는 경우,
            // 생성자에서 OnFinish()를 호출하여 게임을 즉시 종료할 수도 있습니다.
        }

        public override void OnGroup(DisboardPlayer player, string message)
        {
            // 메시지를 받았을 때의 로직을 입력합니다.
            if (GameTurn != 1)//GameTurn이 1일때만 입력을 받습니다.
                return;

            if (player == InitialPlayers[currentPlayerIndex])
            {
                // Send는 그룹 채팅에 메시지를 보내는 함수입니다.
                // SendImage, SendImages 함수도 사용할 수 있습니다.
                try
                {
                    var newActions = message.Select(_ => int.Parse(_.ToString()));
                    if (newActions.Count() != 4)
                    {
                        Send("숫자 네 개를 입력해주세요. 예시: 1232");
                        return;
                    }
                    if (newActions.Max() > 4)
                    {
                        Send("액션이 4까지만 있습니다.");
                        return;
                    }
                    players[currentPlayerIndex].SetActions(newActions);
                    GameTurn = 2;//입력을 그만 받습니다.
                    while (players[currentPlayerIndex].DoAction() >= 0)
                    {
                        PrintGame(1);
                    }
                    GameTurn = 1;
                    
                }
                catch (System.FormatException)
                {
                    Send("뛰어쓰기 없이 숫자만 네 개 입력하세요. 예시: 1232");
                }
            }


            // 예제 프로젝트 Vechu를 확인하세요.
            // 표를 그리려면 BoardContext.GetBoardGrid 를 참고하세요.
            // 다양한 출력 방법을 알아보려면 TurnState.PrintTurn 을 참고하세요.
        }

        public override void OnDM (DisboardPlayer player, string message) 
        {

        }
        public override void OnTick()
        {
            // 이 함수는 0.1초마다 호출됩니다.
            // 사용하지 않는 경우 함수를 지워도 좋습니다.
        }

        void PrintGame(int printType)
        {
            string mapResult = " ";
            int hp1 = players[0].GetHealthPoint();
            int hp2 = players[1].GetHealthPoint();
            int sp1 = players[0].GetStamina();
            int sp2 = players[1].GetStamina();

            mapResult += $"P1: {hp1} / {sp1}\r\n";
            mapResult += $"P2: {hp2} / {sp2}\r\n";
            int temp = 0;
            for(int i = 0; i < MapSize; i++)
            {
                temp = maps[i].GetPlayerNumber();
                if (temp == 0)
                {
                    mapResult += " O .";
                }
                else if(temp == 2)
                {
                    mapResult += ":people_wrestling:.";
                }
                else if(maps[i].CheckOtherPlayer(0))
                {
                    mapResult += ":person_standing:.";
                }
                else
                {
                    mapResult += ":woman_standing:.";
                }
            }
            Send(mapResult);
        }
    }

    class GaposFactory : IDisboardGameFactory
    {
        public DisboardGame New(DisboardGameInitData initData) => new Gapos(initData);
        public void OnHelp(DisboardChannel channel)
        {
            // BOT help가 호출되었을 때 반응하는 로직을 작성합니다.

            string helpString = "예제 프로젝트입니다.";
            channel.Send(helpString);
        }
    }


}