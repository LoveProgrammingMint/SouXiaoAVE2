// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using SouXiaoAVE.Service.Network.Protocol;

namespace SouXiaoAVE.Service.Network;

public sealed class TcpServer : IDisposable
{
    private TcpListener? _listener;
    private CancellationTokenSource? _cancellationTokenSource;
    private Boolean _isRunning = false;
    private Boolean _disposed = false;
    private Int32 _activeConnections = 0;

    private readonly ConcurrentDictionary<Guid, TcpClient> _clients = new();
    private readonly Int32 _port;
    private readonly Int32 _maxConnections;
    private readonly Object _lock = new();

    public Boolean IsRunning => _isRunning;
    public Int32 ActiveConnections => _activeConnections;
    public Int32 Port => _port;

    public event EventHandler<ClientConnectedEventArgs>? ClientConnected;
    public event EventHandler<ClientDisconnectedEventArgs>? ClientDisconnected;
    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;
    public event EventHandler<ServerErrorEventArgs>? ServerError;

    public TcpServer(Int32 port = NetworkConstants.DefaultPort, Int32 maxConnections = 100)
    {
        _port = port;
        _maxConnections = maxConnections;
    }

    public Boolean Start()
    {
        lock (_lock)
        {
            if (_isRunning)
            {
                return false;
            }

            try
            {
                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();
                _cancellationTokenSource = new CancellationTokenSource();
                _isRunning = true;

                _ = AcceptClientsAsync(_cancellationTokenSource.Token);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public Boolean Stop()
    {
        lock (_lock)
        {
            if (!_isRunning)
            {
                return false;
            }

            _isRunning = false;
            _cancellationTokenSource?.Cancel();

            foreach (KeyValuePair<Guid, TcpClient> client in _clients)
            {
                try
                {
                    client.Value.Close();
                }
                catch { }
            }
            _clients.Clear();

            try
            {
                _listener?.Stop();
            }
            catch { }

            return true;
        }
    }

    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _isRunning)
        {
            try
            {
                TcpClient client = await _listener!.AcceptTcpClientAsync(cancellationToken);
                Guid clientId = Guid.NewGuid();

                _clients[clientId] = client;
                Interlocked.Increment(ref _activeConnections);

                ClientConnected?.Invoke(this, new ClientConnectedEventArgs(clientId, client.Client.RemoteEndPoint?.ToString() ?? "Unknown"));

                _ = HandleClientAsync(clientId, client, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                ServerError?.Invoke(this, new ServerErrorExceptionEventArgs(ex.Message, ex));
            }
        }
    }

    private async Task HandleClientAsync(Guid clientId, TcpClient client, CancellationToken cancellationToken)
    {
        NetworkStream? stream = null;

        try
        {
            stream = client.GetStream();
            Byte[] buffer = new Byte[NetworkConstants.BufferSize];

            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                Byte[]? messageData = await ReadMessageAsync(stream, buffer, cancellationToken);

                if (messageData is null)
                {
                    break;
                }

                MessageReceived?.Invoke(this, new MessageReceivedEventArgs(clientId, messageData, stream));
            }
        }
        catch (Exception ex)
        {
            ServerError?.Invoke(this, new ServerErrorExceptionEventArgs($"Client {clientId} error: {ex.Message}", ex));
        }
        finally
        {
            stream?.Close();
            client.Close();
            _clients.TryRemove(clientId, out _);
            Interlocked.Decrement(ref _activeConnections);

            ClientDisconnected?.Invoke(this, new ClientDisconnectedEventArgs(clientId));
        }
    }

    private static async Task<Byte[]?> ReadMessageAsync(NetworkStream stream, Byte[] buffer, CancellationToken cancellationToken)
    {
        Int32 headerRead = 0;
        while (headerRead < NetworkConstants.HeaderSize)
        {
            Int32 read = await stream.ReadAsync(buffer.AsMemory(headerRead, NetworkConstants.HeaderSize - headerRead), cancellationToken);
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
            Int32 read = await stream.ReadAsync(messageData.AsMemory(NetworkConstants.HeaderSize + bodyRead, messageLength - bodyRead), cancellationToken);
            if (read == 0)
            {
                return null;
            }
            bodyRead += read;
        }

        return messageData;
    }

    public async Task SendAsync(Guid clientId, Byte[] data)
    {
        if (_clients.TryGetValue(clientId, out TcpClient? client) && client.Connected)
        {
            NetworkStream stream = client.GetStream();
            await stream.WriteAsync(data, CancellationToken.None);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _cancellationTokenSource?.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

public class ClientConnectedEventArgs : EventArgs
{
    public Guid ClientId { get; }
    public String RemoteEndPoint { get; }

    public ClientConnectedEventArgs(Guid clientId, String remoteEndPoint)
    {
        ClientId = clientId;
        RemoteEndPoint = remoteEndPoint;
    }
}

public class ClientDisconnectedEventArgs : EventArgs
{
    public Guid ClientId { get; }

    public ClientDisconnectedEventArgs(Guid clientId)
    {
        ClientId = clientId;
    }
}

public class MessageReceivedEventArgs : EventArgs
{
    public Guid ClientId { get; }
    public Byte[] MessageData { get; }
    public NetworkStream Stream { get; }

    public MessageReceivedEventArgs(Guid clientId, Byte[] messageData, NetworkStream stream)
    {
        ClientId = clientId;
        MessageData = messageData;
        Stream = stream;
    }
}

public abstract class ServerErrorEventArgs : EventArgs
{
    public String Message { get; }
    public Exception? Exception { get; }

    protected ServerErrorEventArgs(String message, Exception? exception = null)
    {
        Message = message;
        Exception = exception;
    }
}

public sealed class ServerErrorExceptionEventArgs : ServerErrorEventArgs
{
    public ServerErrorExceptionEventArgs(String message, Exception? exception = null) : base(message, exception) { }
}
