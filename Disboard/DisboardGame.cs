﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Disboard
{
    /// <summary>
    /// 이 클래스를 상속하여 게임을 구현합니다.
    /// </summary>
    /// <remarks>
    /// 생성자에서는 Send, OnFinish 등을 호출하지 않아야 합니다. 대신 OnStart를 사용하세요.
    /// </remarks>
    public abstract class DisboardGame
    {
        /// <summary>
        /// 그룹 채널의 BOT start, BOT restart 명령어에 의해 호출됩니다.
        /// </summary>
        /// <remarks>
        /// 생성자에서는 Send, OnFinish 등을 호출하지 않아야 합니다. 대신 OnStart를 사용하세요.
        /// </remarks>
        /// <param name="initData">게임 생성에 필요한 데이터입니다.</param>
        public DisboardGame(DisboardGameInitData initData)
        {
            IsDebug = initData.IsDebug;
            Channel = new DisboardChannel(initData.Channel, initData.MessageQueue);
            InitialPlayers = initData.Players;
            OnFinish = () => initData.OnFinish(initData.Channel.Id);
            MessageQueue = initData.MessageQueue;
            Render = controlConstructor => initData.Dispatcher.Invoke(() => controlConstructor().Render());
        }

        internal bool IsDebug { get; }
        internal ConcurrentQueue<Task> MessageQueue { get; }
        internal Semaphore Semaphore { get; } = new Semaphore();

        /// <summary>
        /// 게임이 생성된 그룹 채널입니다.
        /// </summary>
        public DisboardChannel Channel { get; }
        /// <summary>
        /// 그룹 채널에 메시지를 전송합니다. Discord embed를 포함할 수 있습니다.
        /// </summary>
        /// <remarks>
        /// 생성자에서는 호출하지 않아야 합니다. 대신 OnStart를 사용하세요.
        /// </remarks>
        public SendType Send => Channel.Send;
        /// <summary>
        /// 그룹 채널에 한 장의 이미지를 전송합니다. 메시지나 Discord embed를 포함할 수 있습니다.
        /// </summary>
        /// <remarks>
        /// 생성자에서는 호출하지 않아야 합니다. 대신 OnStart를 사용하세요.
        /// </remarks>
        public SendImageType SendImage => Channel.SendImage;
        /// <summary>
        /// 그룹 채널에 여러 장의 이미지를 전송합니다. 메시지나 Discord embed를 포함할 수 있습니다.
        /// </summary>
        /// <remarks>
        /// 생성자에서는 호출하지 않아야 합니다. 대신 OnStart를 사용하세요.
        /// </remarks>
        public SendImagesType SendImages => Channel.SendImages;
        /// <summary>
        /// 이 채널의 URL입니다. 대화방에 전송하면 URL을 클릭했을 때 이 채널을 보게 됩니다. 
        /// </summary>
        public string GroupURL => Channel.URL;
        /// <summary>
        /// 게임이 종료되면 OnFinish를 호출해야 합니다.
        /// </summary>
        /// <remarks>
        /// 생성자에서는 호출하지 않아야 합니다. 대신 OnStart를 사용하세요.
        /// </remarks>
        public Action OnFinish { get; }
        /// <summary>
        /// 게임에 참여하는 플레이어들입니다. 게임에 참가하는 인원은 게임 시작 후 변경할 수 없습니다. 순서는 임의로 결정됩니다.
        /// </summary>
        public IReadOnlyList<DisboardPlayer> InitialPlayers { get; }
        /// <summary>
        /// WPF 컨트롤을 사용하여 이미지를 그릴 수 있습니다. EchoVisual.cs를 예제로써 참고하세요.
        /// 사용하려면 Main 함수 윗줄에 [System.STAThread()]를 추가해야 합니다.
        /// </summary>
        /// <remarks>
        /// 생성자에서는 호출하지 않아야 합니다. 대신 OnStart를 사용하세요.
        /// </remarks>
        public RenderType Render { get; }
        /// <summary>
        /// 게임이 시작될 때 한 번 호출됩니다. OnGroup과 OnTick은 반드시 OnStart 이후에 호출됩니다.
        /// </summary>
        /// <remarks>
        /// 생성자에서는 Send, OnFinish 등을 호출하지 않아야 합니다. 대신 OnStart를 사용하세요.
        /// </remarks>
        public abstract void Start();
        /// <summary>
        /// 그룹 채널에서 플레이어의 메시지가 작성되면 호출됩니다.
        /// </summary>
        /// <param name="author">메시지를 작성한 플레이어입니다. 반드시 게임에 참여하고 있습니다.</param>
        /// <param name="message">플레이어가 작성한 메시지의 내용입니다.</param>
        public abstract void OnGroup(DisboardPlayer author, string message);
        /// <summary>
        /// 매 0.1초마다 호출됩니다. 호출 간격은 정확하지 않습니다.
        /// </summary>
        public virtual void OnTick() { }
    }
}
