using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace JigsawPrototype.Core.Services.Ads
{
    public sealed class SimulatedAdsService : IAdsService
    {
        public sealed class Config
        {
            public int SimulatedDelayMs = 1000;
        }

        private readonly int _simulatedDelayMs;

        public SimulatedAdsService(Config config)
        {
            _simulatedDelayMs = config.SimulatedDelayMs;
        }

        public async UniTask<AdResult> ShowRewardedAsync(string placement, CancellationToken ct)
        {
            await UniTask.Delay(_simulatedDelayMs, cancellationToken: ct);

            // 70% success
            return Random.value < 0.7 ? AdResult.Success : AdResult.Failed;
        }
    }
}

