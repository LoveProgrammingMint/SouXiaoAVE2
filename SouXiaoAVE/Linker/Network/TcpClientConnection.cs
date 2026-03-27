// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using SouXiaoAVE.Service.Network.Protocol;

namespace SouXiaoAVE.Linker.Network;

public sealed class TcpClientConnection : IDisposable
{
    private System.Net.Sockets.TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private Boolean _isConnected = false;
    private Boolean _disposed = false;

    private readonly String _host;
    private readonly Int32 _port;
    private readonly Int32 _timeoutMs;
    private readonly Object _lock = new();

    public Boolean IsConnected => _isConnected && (_tcpClient?.Connected ?? false);
    public String Host => _host;
    public Int32 Port => _port;

    public event EventHandler<DisconnectedEventArgs>? Disconnected;

    public TcpClientConnection(String host = "localhost", Int32 port = NetworkConstants.DefaultPort, Int32 timeoutMs = NetworkConstants.TimeoutMs)
    {
        _host = host;
        _port = port;
        _timeoutMs = timeoutMs;
    }

    public async Task<Boolean> ConnectAsync()
    {
        lock (_lock)
        {
            if (_isConnected)
            {
                return true;
            }
        }

        try
        {
            _tcpClient = new System.Net.Sockets.TcpClient();
            using CancellationTokenSource cts = new(_timeoutMs);
            await _tcpClient.ConnectAsync(_host, _port, cts.Token);

            _stream = _tcpClient.GetStream();
            _isConnected = true;

            return true;
        }
        catch (Exception)
        {
            _isConnected = false;
            return false;
        }
    }

    public Boolean Disconnect()
    {
        lock (_lock)
        {
            if (!_isConnected)
            {
                return true;
            }

            try
            {
                _stream?.Close();
                _tcpClient?.Close();
                _isConnected = false;

                Disconnected?.Invoke(this, new DisconnectedEventArgs());

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public async Task<Byte[]?> SendAndReceiveAsync(Byte[] data)
    {
        if (!_isConnected || _stream is null)
        {
            return null;
        }

        try
        {
            await _stream.WriteAsync(data, CancellationToken.None);

            Byte[] buffer = new Byte[NetworkConstants.BufferSize];
            return await ReadMessageAsync(_stream, buffer);
        }
        catch (Exception)
        {
            _isConnected = false;
            return null;
        }
    }

    private static async Task<Byte[]?> ReadMessageAsync(NetworkStream stream, Byte[] buffer)
    {
        Int32 headerRead = 0;
        while (headerRead < NetworkConstants.HeaderSize)
        {
            Int32 read = await stream.ReadAsync(buffer.AsMemory(headerRead, NetworkConstants.HeaderSize - headerRead));
            if (read == 0)
            {
                return null;
            }
            headerRead += read;
        }

        Int32 messageLength = BitConverter.ToInt32(buffer, 0);

        if (messageLength <= 0 || messageLength > NetworkConstants.BufferSize)
        {
            return null;
        }

        Byte[] messageData = new Byte[NetworkConstants.HeaderSize + messageLength];
        Buffer.BlockCopy(buffer, 0, messageData, 0, NetworkConstants.HeaderSize);

        Int32 bodyRead = 0;
        while (bodyRead < messageLength)
        {
            Int32 read = await stream.ReadAsync(messageData.AsMemory(NetworkConstants.HeaderSize + bodyRead, messageLength - bodyRead));
            if (read == 0)
            {
                return null;
            }
            bodyRead += read;
        }

        return messageData;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Disconnect();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

public class DisconnectedEventArgs : EventArgs { }
