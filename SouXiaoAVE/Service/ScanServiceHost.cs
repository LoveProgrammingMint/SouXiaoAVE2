// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using SouXiaoAVE.Linker.Enums;
using SouXiaoAVE.Linker.Models;
using SouXiaoAVE.Service.Handlers;
using SouXiaoAVE.Service.Network;

namespace SouXiaoAVE.Service;

public sealed class ScanServiceHost : IDisposable
{
    private static ScanServiceHost? _instance;
    private static readonly Object _instanceLock = new();

    private TcpServer? _tcpServer;
    private MessageProcessor? _messageProcessor;
    private TaskHandlerFactory? _handlerFactory;
    private Boolean _isRunning = false;
    private Boolean _disposed = false;

    private readonly ServiceConfig _config;
    private readonly Int32 _port;

    public static ScanServiceHost Instance
    {
        get
        {
            lock (_instanceLock)
            {
                _instance ??= new ScanServiceHost();
                return _instance;
            }
        }
    }

    public Boolean IsRunning => _isRunning;
    public Int32 Port => _port;
    public Int32 ActiveConnections => _tcpServer?.ActiveConnections ?? 0;

    private ScanServiceHost(Int32 port = 9527, ServiceConfig? config = null)
    {
        _port = port;
        _config = config ?? ServiceConfig.Default;
    }

    public Boolean Start()
    {
        if (_isRunning)
        {
            return false;
        }

        try
        {
            _handlerFactory = new TaskHandlerFactory();
            _messageProcessor = new MessageProcessor(_handlerFactory, _config.QuickScanMode);
            _tcpServer = new TcpServer(_port);

            _tcpServer.MessageReceived += OnMessageReceived;
            _tcpServer.ClientConnected += OnClientConnected;
            _tcpServer.ClientDisconnected += OnClientDisconnected;
            _tcpServer.ServerError += OnServerError;

            Boolean started = _tcpServer.Start();
            _isRunning = started;

            return started;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public Boolean Stop()
    {
        if (!_isRunning)
        {
            return false;
        }

        _tcpServer?.Stop();
        _isRunning = false;

        return true;
    }

    private async void OnMessageReceived(Object? sender, MessageReceivedEventArgs e)
    {
        try
        {
            Byte[] response = await _messageProcessor!.ProcessAsync(e.ClientId, e.MessageData, e.Stream);
            await e.Stream.WriteAsync(response, CancellationToken.None);
        }
        catch (Exception)
        {
        }
    }

    private void OnClientConnected(Object? sender, ClientConnectedEventArgs e)
    {
    }

    private void OnClientDisconnected(Object? sender, ClientDisconnectedEventArgs e)
    {
    }

    private void OnServerError(Object? sender, ServerErrorEventArgs e)
    {
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _tcpServer?.Dispose();
        _messageProcessor?.Dispose();
        _handlerFactory?.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
