// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SouXiaoAVE.Services;

public sealed class TcpServer : IAsyncDisposable
{
    private readonly Int32 _port;
    private readonly IPAddress _bindAddress;
    private TcpListener? _listener;
    private readonly List<TcpClient> _clients;
    private readonly Lock _lock;
    private readonly CancellationTokenSource _cts;
    private Boolean _disposed;
    private Boolean _isRunning;

    public event EventHandler<String>? MessageReceived;
    public event EventHandler<String>? ClientConnected;
    public event EventHandler<String>? ClientDisconnected;
    public event EventHandler<Exception>? ErrorOccurred;

    public Boolean IsRunning => _isRunning;
    public Int32 ConnectedClientCount
    {
        get
        {
            lock (_lock)
            {
                return _clients.Count;
            }
        }
    }

    public TcpServer(Int32 port, IPAddress? bindAddress = null)
    {
        _port = port;
        _bindAddress = bindAddress ?? IPAddress.Any;
        _clients = new List<TcpClient>();
        _lock = new Lock();
        _cts = new CancellationTokenSource();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_isRunning)
            return;

        _listener = new TcpListener(_bindAddress, _port);
        _listener.Start();
        _isRunning = true;

        _ = Task.Run(() => AcceptClientsAsync(_cts.Token), cancellationToken);
    }

    public async Task StopAsync()
    {
        if (!_isRunning)
            return;

        _cts.Cancel();
        _listener?.Stop();

        lock (_lock)
        {
            foreach (TcpClient client in _clients)
            {
                try
                {
                    client.Close();
                }
                catch { }
            }
            _clients.Clear();
        }

        _isRunning = false;
    }

    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener is not null)
        {
            try
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(cancellationToken);
                String clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";

                lock (_lock)
                {
                    _clients.Add(client);
                }

                ClientConnected?.Invoke(this, clientEndpoint);
                _ = HandleClientAsync(client, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        String clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";

        try
        {
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new(stream, Encoding.UTF8);
            using StreamWriter writer = new(stream, Encoding.UTF8) { AutoFlush = true };

            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                String? message = await reader.ReadLineAsync(cancellationToken);
                if (message is null)
                    break;

                MessageReceived?.Invoke(this, message);
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
        }
        finally
        {
            lock (_lock)
            {
                _clients.Remove(client);
            }
            ClientDisconnected?.Invoke(this, clientEndpoint);
            client.Close();
        }
    }

    public async Task BroadcastAsync(String message)
    {
        ThrowIfDisposed();

        List<TcpClient> clientsCopy;
        lock (_lock)
        {
            clientsCopy = [.. _clients];
        }

        Byte[] data = Encoding.UTF8.GetBytes(message + "\n");

        foreach (TcpClient client in clientsCopy)
        {
            try
            {
                if (client.Connected)
                {
                    NetworkStream stream = client.GetStream();
                    await stream.WriteAsync(data);
                }
            }
            catch { }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TcpServer));
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await StopAsync();
            _cts.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
