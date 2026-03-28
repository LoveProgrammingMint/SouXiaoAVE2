// Copyright (C) 2026 LinduCMint
// This file is part of SouXiao AntiVirus Engine, licensed under MINT License.
// See LICENSE file for full terms.
// For production use or distribution, contact 3327867352@qq.com for authorization.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using SouXiaoAVE.Workflow.Dataclass;

namespace SouXiaoAVE.Workflow;

internal class MultiStagePipeline : IAsyncDisposable
{

    public int MCapacity { get; }

    public int NCapacity { get; }

    private readonly Channel<string> _channelM;
    private readonly Channel<RawDataStream> _channelN;
    private readonly CancellationTokenSource _cts = new();
    private readonly List<Task> _allWorkers = [];
    private readonly Lock _lock = new();


    public int ProducerACount { get; private set; }

    public int TransformerCount { get; private set; }

    public int ConsumerBCount { get; private set; }

    public int MPendingCount => _channelM.Reader.Count;

    public int NPendingCount => _channelN.Reader.Count;

    public MultiStagePipeline(int mCapacity = 1000, int nCapacity = 50)
    {
        MCapacity = mCapacity;
        NCapacity = nCapacity;

        _channelM = Channel.CreateBounded<string>(new BoundedChannelOptions(mCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });

        _channelN = Channel.CreateBounded<RawDataStream>(new BoundedChannelOptions(nCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });
    }

    public void AddProducersA(int count, Func<CancellationToken, Task<string>> idGenerator)
    {
        lock (_lock)
        {
            for (int i = 0; i < count; i++)
            {
                int id = ++ProducerACount;
                _allWorkers.Add(Task.Run(async () =>
                {
                    await foreach (var _ in GetInfiniteAsync(_cts.Token))
                    {
                        var item = await idGenerator(_cts.Token);
                        await _channelM.Writer.WriteAsync(item, _cts.Token);
                    }
                }, _cts.Token));
            }
        }
    }

    public void AddTransformers(int count, Func<string, CancellationToken, Task<RawDataStream>> processor)
    {
        lock (_lock)
        {
            for (int i = 0; i < count; i++)
            {
                int id = ++TransformerCount;
                _allWorkers.Add(Task.Run(async () =>
                {
                    await foreach (var idStr in _channelM.Reader.ReadAllAsync(_cts.Token))
                    {
                        var data = await processor(idStr, _cts.Token);
                        await _channelN.Writer.WriteAsync(data, _cts.Token);
                    }
                }, _cts.Token));
            }
        }
    }

    public void AddConsumersB(int count, Func<RawDataStream, CancellationToken, Task> processor)
    {
        lock (_lock)
        {
            for (int i = 0; i < count; i++)
            {
                int id = ++ConsumerBCount;
                _allWorkers.Add(Task.Run(async () =>
                {
                    await foreach (var data in _channelN.Reader.ReadAllAsync(_cts.Token))
                    {
                        await processor(data, _cts.Token);
                    }
                }, _cts.Token));
            }
        }
    }


    public void Complete() => _channelM.Writer.Complete();

    public void Cancel() => _cts.Cancel();

    public async Task WaitForCompletionAsync()
    {
        await Task.WhenAll(_allWorkers);
    }

    private static async IAsyncEnumerable<int> GetInfiniteAsync([EnumeratorCancellation] CancellationToken ct)
    {
        int i = 0;
        while (!ct.IsCancellationRequested) yield return i++;
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        try { await WaitForCompletionAsync(); } catch { /* ignore */ }
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}