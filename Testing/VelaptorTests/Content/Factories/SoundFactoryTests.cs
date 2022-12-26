﻿// <copyright file="SoundFactoryTests.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

namespace VelaptorTests.Content.Factories;

using System;
using Carbonate;
using FluentAssertions;
using Helpers;
using Moq;
using Velaptor;
using Velaptor.Content.Factories;
using Velaptor.Exceptions;
using Velaptor.ReactableData;
using Xunit;

/// <summary>
/// Tests the <see cref="SoundFactory"/> class.
/// </summary>
public class SoundFactoryTests
{
    private readonly Mock<IReactable> mockReactable;
    private readonly Mock<IDisposable> mockDisposeSoundUnsubscriber;
    private readonly Mock<IDisposable> mockShutDownUnsubscriber;
    private IReactor? disposeReactor;
    private IReactor? shutDownReactor;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoundFactoryTests"/> class.
    /// </summary>
    public SoundFactoryTests()
    {
        this.mockDisposeSoundUnsubscriber = new Mock<IDisposable>();
        this.mockShutDownUnsubscriber = new Mock<IDisposable>();

        this.mockReactable = new Mock<IReactable>();
        this.mockReactable.Setup(m => m.Subscribe(It.IsAny<IReactor>()))
            .Returns<IReactor>(reactor =>
            {
                reactor.Should().NotBeNull("it is required for unit testing.");

                if (reactor.EventId == NotificationIds.SoundDisposedId)
                {
                    return this.mockDisposeSoundUnsubscriber.Object;
                }

                if (reactor.EventId == NotificationIds.SystemShuttingDownId)
                {
                    return this.mockShutDownUnsubscriber.Object;
                }

                Assert.Fail($"The event ID '{reactor.EventId}' is not recognized or accounted for in the unit test.");
                return null;
            })
            .Callback<IReactor>(reactor =>
            {
                reactor.Should().NotBeNull("it is required for unit testing.");

                if (reactor.EventId == NotificationIds.SoundDisposedId)
                {
                    this.disposeReactor = reactor;
                }
                else if (reactor.EventId == NotificationIds.SystemShuttingDownId)
                {
                    this.shutDownReactor = reactor;
                }
                else
                {
                    Assert.Fail($"The event ID '{reactor.EventId}' is not recognized or accounted for in the unit test.");
                }
            });
    }

    #region Constructor Tests
    [Fact]
    public void Ctor_WithNullReactableParam_ThrowsException()
    {
        // Arrange & Act
        var act = () =>
        {
            _ = new SoundFactory(null);
        };

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .WithMessage("The parameter must not be null. (Parameter 'reactable')");
    }
    #endregion

    #region Notification Tests
    [Fact]
    public void Reactable_WhenOnNextMessageHasNullData_ThrowsException()
    {
        // Arrange
        var expected = $"There was an issue with the '{nameof(SoundFactory)}.Constructor()' subscription source";
        expected += $" for subscription ID '{NotificationIds.SoundDisposedId}'.";

        _ = CreateSystemUnderTest();

        var mockMessage = new Mock<IMessage>();
        mockMessage.Setup(m => m.GetData<DisposeSoundData>(It.IsAny<Action<Exception>?>()))
            .Returns<Action<Exception>?>(_ => null);

        // Act
        var act = () => this.disposeReactor.OnNext(mockMessage.Object);

        // Assert
        act.Should().Throw<PushNotificationException>()
            .WithMessage(expected);
    }
    #endregion

    #region Method Tests
    [Fact]
    public void GetNewId_WhenInvoked_AddsSoundIdAndPathToList()
    {
        // Arrange
        var sut = CreateSystemUnderTest();

        // Act
        var actual = sut.GetNewId("test-file");

        // Assert
        actual.Should().Be(1);
    }

    [Fact]
    public void ShutDown_WhenInvoked_ShutsDownFactory()
    {
        // Arrange
        _ = CreateSystemUnderTest();

        // Act
        this.shutDownReactor.OnNext();

        // Assert
        this.mockDisposeSoundUnsubscriber.VerifyOnce(m => m.Dispose());
        this.mockShutDownUnsubscriber.VerifyOnce(m => m.Dispose());
    }
    #endregion

    /// <summary>
    /// Creates a new instance of <see cref="SoundFactory"/> for the purpose of testing.
    /// </summary>
    /// <returns>The instance to test.</returns>
    private SoundFactory CreateSystemUnderTest()
        => new (this.mockReactable.Object);
}
