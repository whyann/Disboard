﻿using DSharpPlus.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Disboard
{
    using ChannelIdType = UInt64;
    public sealed class DisboardGameInitData
    {
        internal bool IsDebug { get; }
        internal DiscordChannel Channel { get; }
        internal IReadOnlyList<DisboardPlayer> Players { get; }
        internal Action<ChannelIdType> OnFinish { get; }
        internal Dispatcher Dispatcher { get; }
        internal ConcurrentQueue<Task> MessageQueue { get; }
        internal DisboardGameInitData(bool isDebug, DiscordChannel channel, IReadOnlyList<DisboardPlayer> players, Action<ChannelIdType> onFinish, Dispatcher dispatcher, ConcurrentQueue<Task> messageQueue)
        {
            IsDebug = isDebug;
            Channel = channel;
            Players = players;
            OnFinish = onFinish;
            Dispatcher = dispatcher;
            MessageQueue = messageQueue;
        }
    }
}