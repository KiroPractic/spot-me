namespace SpotMe.Web.Components.Shared;

public delegate Task<IEnumerable<T>> ItemsProviderRequestDelegate<T>(InfiniteScrollingItemsProviderRequest context);

public sealed class InfiniteScrollingItemsProviderRequest
{
    public InfiniteScrollingItemsProviderRequest(int startIndex, CancellationToken cancellationToken)
    {
        StartIndex = startIndex;
        CancellationToken = cancellationToken;
    }

    public int StartIndex { get; }
    public CancellationToken CancellationToken { get; }
}