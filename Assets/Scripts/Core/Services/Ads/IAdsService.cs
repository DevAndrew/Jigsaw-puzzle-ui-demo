using System.Threading;
using Cysharp.Threading.Tasks;

namespace JigsawPrototype.Core.Services.Ads
{
    public enum AdResult
    {
        Success = 0,
        Failed = 1,
    }

    public interface IAdsService
    {
        UniTask<AdResult> ShowRewardedAsync(string placement, CancellationToken ct);
    }
}

