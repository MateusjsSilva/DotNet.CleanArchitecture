using CleanArchitecture.Application.Behaviors;
using CleanArchitecture.Application.Common;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Text;
using System.Text.Json;

namespace CleanArchitecture.UnitTests.Behaviors;

public sealed class CachingBehaviorTests
{
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly CachingBehavior<CacheableQuery, string> _behavior;

    public CachingBehaviorTests()
    {
        _behavior = new CachingBehavior<CacheableQuery, string>(
            _cache,
            NullLogger<CachingBehavior<CacheableQuery, string>>.Instance);
    }

    [Fact]
    public async Task Handle_CacheMiss_ShouldCallHandlerAndStoreResult()
    {
        // Arrange
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns((byte[]?)null);

        var query = new CacheableQuery("key-1");
        var handlerCallCount = 0;

        Task<string> Next()
        {
            handlerCallCount++;
            return Task.FromResult("handler-result");
        }

        // Act
        var result = await _behavior.Handle(query, Next, CancellationToken.None);

        // Assert
        result.Should().Be("handler-result");
        handlerCallCount.Should().Be(1);
        await _cache.Received(1).SetAsync(
            "key-1",
            Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CacheHit_ShouldReturnCachedValueWithoutCallingHandler()
    {
        // Arrange
        var cached = JsonSerializer.Serialize("cached-value");
        _cache.GetAsync("key-1", Arg.Any<CancellationToken>())
              .Returns(Encoding.UTF8.GetBytes(cached));

        var query = new CacheableQuery("key-1");
        var handlerCalled = false;

        Task<string> Next()
        {
            handlerCalled = true;
            return Task.FromResult("handler-result");
        }

        // Act
        var result = await _behavior.Handle(query, Next, CancellationToken.None);

        // Assert
        result.Should().Be("cached-value");
        handlerCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenRequestIsICacheInvalidator_ShouldEvictKeysAfterHandler()
    {
        // Arrange
        var command = new InvalidatingCommand("key-a", "key-b");
        var behavior = new CachingBehavior<InvalidatingCommand, Unit>(
            _cache,
            NullLogger<CachingBehavior<InvalidatingCommand, Unit>>.Instance);

        // Act
        await behavior.Handle(command, () => Task.FromResult(Unit.Value), CancellationToken.None);

        // Assert
        await _cache.Received(1).RemoveAsync("key-a", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync("key-b", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRequestIsNeither_ShouldPassThrough()
    {
        // Arrange
        var behavior = new CachingBehavior<PlainQuery, string>(
            _cache,
            NullLogger<CachingBehavior<PlainQuery, string>>.Instance);

        var handlerCalled = false;
        Task<string> Next() { handlerCalled = true; return Task.FromResult("ok"); }

        // Act
        var result = await behavior.Handle(new PlainQuery(), Next, CancellationToken.None);

        // Assert
        result.Should().Be("ok");
        handlerCalled.Should().BeTrue();
        await _cache.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>());
    }

    // --- Helpers ---

    private sealed record CacheableQuery(string Key) : IRequest<string>, ICacheableQuery
    {
        public string CacheKey => Key;
        public TimeSpan? AbsoluteExpiration => TimeSpan.FromMinutes(1);
    }

    private sealed record InvalidatingCommand(params string[] Keys) : IRequest, ICacheInvalidator
    {
        public IEnumerable<string> CacheKeysToInvalidate => Keys;
    }

    private sealed record PlainQuery : IRequest<string>;
}
