using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace JigsawPrototype.Core.Services.Ads
{
    public sealed class SimulatedAdsService : IAdsService
    {
        public sealed class Config
        {
            public int SimulatedDelayMs = 1000;
            public List<AdResult> NextResults;
        }

        private readonly int _simulatedDelayMs;
        private readonly Queue<AdResult> _nextResults;
        private readonly Random _random = new Random();

        public SimulatedAdsService(Config config)
        {
            _simulatedDelayMs = config.SimulatedDelayMs;
            _nextResults = new Queue<AdResult>(config.NextResults ?? new List<AdResult>());
        }

        public async UniTask<AdResult> ShowRewardedAsync(string placement, CancellationToken ct)
        {
            await UniTask.Delay(_simulatedDelayMs, cancellationToken: ct);
            await UniTask.Delay(_simulatedDelayMs, cancellationToken: ct);

            if (_nextResults.Count > 0)
            {
                return _nextResults.Dequeue();
            }

            // fallback: 70% success
            return _random.NextDouble() < 0.7 ? AdResult.Success : AdResult.Failed;
        }
    }
}

