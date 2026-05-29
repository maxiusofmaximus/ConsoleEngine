using ConsoleEngine.Core;
using ConsoleEngine.Input;
using Xunit;

namespace ConsoleEngine.Tests;

public sealed class MockInputProviderTests
{
    [Fact]
    public void Enqueue_And_ReadKey_Dequeues()
    {
        var mock = new MockInputProvider();
        mock.Enqueue(ConsoleKey.Enter);

        ConsoleKeyInfo key = mock.ReadKey();
        Assert.Equal(ConsoleKey.Enter, key.Key);
    }

    [Fact]
    public void ReadKey_EmptyQueue_ThrowsInvalidOperationException()
    {
        var mock = new MockInputProvider();
        Assert.Throws<InvalidOperationException>(() => mock.ReadKey());
    }

    [Fact]
    public void KeyAvailable_TrueWhenKeysEnqueued()
    {
        var mock = new MockInputProvider();
        Assert.False(mock.KeyAvailable);
        mock.Enqueue(ConsoleKey.Spacebar);
        Assert.True(mock.KeyAvailable);
    }

    [Fact]
    public void KeyAvailable_FalseAfterAllKeysConsumed()
    {
        var mock = new MockInputProvider();
        mock.Enqueue(ConsoleKey.A);
        mock.ReadKey();
        Assert.False(mock.KeyAvailable);
    }

    [Fact]
    public void Remaining_TracksQueueSize()
    {
        var mock = new MockInputProvider();
        Assert.Equal(0, mock.Remaining);
        mock.Enqueue(ConsoleKey.A);
        mock.Enqueue(ConsoleKey.B);
        Assert.Equal(2, mock.Remaining);
        mock.ReadKey();
        Assert.Equal(1, mock.Remaining);
    }

    [Fact]
    public void Clear_RemovesAllKeys()
    {
        var mock = new MockInputProvider();
        mock.Enqueue(ConsoleKey.A);
        mock.Enqueue(ConsoleKey.B);
        mock.Clear();
        Assert.Equal(0, mock.Remaining);
        Assert.False(mock.KeyAvailable);
    }

    [Fact]
    public void Enqueue_ConsoleKeyInfo_Directly()
    {
        var mock = new MockInputProvider();
        var info = new ConsoleKeyInfo('Z', ConsoleKey.Z, shift: true, alt: false, control: false);
        mock.Enqueue(info);

        ConsoleKeyInfo result = mock.ReadKey();
        Assert.Equal(ConsoleKey.Z, result.Key);
        Assert.Equal('Z', result.KeyChar);
    }

    [Fact]
    public void Enqueue_PreservesOrder()
    {
        var mock = new MockInputProvider();
        mock.Enqueue(ConsoleKey.A);
        mock.Enqueue(ConsoleKey.B);
        mock.Enqueue(ConsoleKey.C);

        Assert.Equal(ConsoleKey.A, mock.ReadKey().Key);
        Assert.Equal(ConsoleKey.B, mock.ReadKey().Key);
        Assert.Equal(ConsoleKey.C, mock.ReadKey().Key);
    }

    [Fact]
    public void ReadLine_EmptyQueue_ReturnsNull()
    {
        var mock = new MockInputProvider();
        Assert.Null(mock.ReadLine());
    }

    [Fact]
    public void EnqueueLine_And_ReadLine_Dequeues()
    {
        var mock = new MockInputProvider();
        mock.EnqueueLine("quit");
        Assert.Equal("quit", mock.ReadLine());
    }

    [Fact]
    public void EnqueueLine_PreservesOrder()
    {
        var mock = new MockInputProvider();
        mock.EnqueueLine("north");
        mock.EnqueueLine("quit");
        Assert.Equal("north", mock.ReadLine());
        Assert.Equal("quit",  mock.ReadLine());
    }

    [Fact]
    public void Clear_AlsoClearsLines()
    {
        var mock = new MockInputProvider();
        mock.EnqueueLine("north");
        mock.Clear();
        Assert.Null(mock.ReadLine());
    }

    [Fact]
    public void Enqueue_WithKeyChar_PreservesChar()
    {
        var mock = new MockInputProvider();
        mock.Enqueue(ConsoleKey.D, 'd');

        ConsoleKeyInfo result = mock.ReadKey();
        Assert.Equal('d', result.KeyChar);
    }

    [Fact]
    public void ImplementsIInputProvider()
    {
        var mock = new MockInputProvider();
        mock.Enqueue(ConsoleKey.Enter);

        IInputProvider provider = mock;
        Assert.True(provider.KeyAvailable);
        _ = provider.ReadKey();
        Assert.False(provider.KeyAvailable);
        Assert.Null(provider.ReadLine());
    }
}
