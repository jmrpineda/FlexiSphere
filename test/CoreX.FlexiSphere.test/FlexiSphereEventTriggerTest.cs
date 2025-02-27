#region Header

// --------------------------------------------------------------------------------------
// Powered by:
// 
//     __________.__                  .___    ___________                             
//     \______   \__| ____   ____   __| _/____\__    ___/___   ____       ____  __ __ 
//      |     ___/  |/    \_/ __ \ / __ |\__  \ |    |_/ __ \_/ ___\    _/ __ \|  |  \
//      |    |   |  |   |  \  ___// /_/ | / __ \|    |\  ___/\  \___    \  ___/|  |  /
//      |____|   |__|___|  /\___  >____ |(____  /____| \___  >\___  > /\ \___  >____/ 
//                   \/     \/     \/     \/           \/     \/  \/     \/
// 
// 
// FileName: FlexiSphereEventTriggerTest.cs
//
// Author:   jmr.pineda
// eMail:    jmr.pineda@pinedatec.eu
// Profile:  http://pinedatec.eu/profile
//
//           Copyrights (c) PinedaTec.eu 2025, all rights reserved.
//           CC BY-NC-ND - https://creativecommons.org/licenses/by-nc-nd/4.0
//
//  Created at: 2025-02-04T13:18:01.096Z
//
// --------------------------------------------------------------------------------------

#endregion

using CoreX.extensions;
using CoreX.FlexiSphere.triggers;

using Shouldly;

namespace CoreX.FlexiSphere.test;

public class FlexiSphereEventTriggerTest : IClassFixture<TestFixture>
{
    private readonly TestFixture _testFixture;

    public FlexiSphereEventTriggerTest(TestFixture testFixture)
    {
        _testFixture = testFixture;
    }

    [Fact]
    public void ConfigureTrigger()
    {
        // Arrange
        IFlexiSphereEventTrigger trigger = new FlexiSphereEventTrigger();
        var action = new Func<IFlexiSphereContext?, Task<bool>>((context) => Task.FromResult(true));

        // Act
        trigger.ConfigureTrigger(action, 1000, 5, 7);

        // Assert
        trigger.ShouldNotBeNull();
        trigger.MaxConcurrents.ShouldBe(5);
        trigger.MaxOccurrences.ShouldBe(7);
    }

    [Fact]
    public async Task ActivateTrigger()
    {
        // Arrange
        IFlexiSphereEventTrigger trigger = new FlexiSphereEventTrigger();
        var action = new Func<IFlexiSphereContext?, Task<bool>>((context) => Task.FromResult(true));
        trigger.ConfigureTrigger(action, 500);

        bool isTriggered = false;
        trigger.OnTriggered += (sender, context) =>
        {
            isTriggered = true;
        };

        bool isFaulted = false;
        trigger.OnFaulted += (sender, context, exception) =>
        {
            isFaulted = true;
        };

        bool isCompleted = false;
        trigger.OnCompleted += (sender, context) =>
        {
            isCompleted = true;
        };

        // Act
        trigger.ActivateTrigger(cancellationToken: TestContext.Current.CancellationToken);

        await AppsHelper.DelayWhileAsync(5000, () => !isTriggered, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        trigger.ShouldNotBeNull();
        trigger.Counter.ShouldBeGreaterThanOrEqualTo(1);
        isTriggered.ShouldBeTrue();

        isFaulted.ShouldBeFalse();
        isCompleted.ShouldBeFalse();
    }

    [Fact]
    public void ActivateTrigger_WithOutConfigure()
    {
        // Arrange
        IFlexiSphereEventTrigger trigger = new FlexiSphereEventTrigger();

        // Act
        Action action = () => trigger.ActivateTrigger(cancellationToken: TestContext.Current.CancellationToken);
        action.ShouldThrow<NullReferenceException>();

        // Assert
        trigger.ShouldNotBeNull();
    }

    [Fact]
    public void DeactivateTrigger()
    {
        // Arrange
        IFlexiSphereEventTrigger trigger = new FlexiSphereEventTrigger();
        var action = new Func<IFlexiSphereContext?, Task<bool>>((context) => Task.FromResult(true));
        trigger.ConfigureTrigger(action, 5000);

        bool isTriggered = false;
        trigger.OnTriggered += (sender, args) =>
        {
            // Assert
            args.ShouldBeNull();

            isTriggered = true;
        };

        bool isFaulted = false;
        trigger.OnFaulted += (sender, context, exception) =>
        {
            // Assert
            context.ShouldBeNull();
            exception.ShouldNotBeNull();

            isFaulted = true;
        };

        bool isCompleted = false;
        trigger.OnCompleted += (sender, context) =>
        {
            // Assert
            context.ShouldBeNull();

            isCompleted = true;
        };

        // Act
        trigger.ActivateTrigger(cancellationToken: TestContext.Current.CancellationToken);
        trigger.DeactivateTrigger("Test deactivation");

        // Assert
        trigger.ShouldNotBeNull();

        isTriggered.ShouldBeFalse();
        isFaulted.ShouldBeFalse();
        isCompleted.ShouldBeTrue();
    }

    [Fact]
    public async Task ActivateTrigger_LimitConcurrences()
    {
        // Arrange
        IFlexiSphereEventTrigger trigger = new FlexiSphereEventTrigger();
        var action = new Func<IFlexiSphereContext?, Task<bool>>(async (context) =>
        {
            await Task.Delay(5000, cancellationToken: TestContext.Current.CancellationToken);
            return true;
        });

        trigger.ConfigureTrigger(action, 250);
        trigger.MaxConcurrents = 2;

        int isTriggered = 0;
        trigger.OnTriggered += (sender, args) =>
        {
            isTriggered++;
        };

        bool isFaulted = false;
        trigger.OnFaulted += (sender, context, exception) =>
        {
            // Assert
            context.ShouldNotBeNull();

            isFaulted = true;
        };

        bool isCompleted = false;
        trigger.OnCompleted += (sender, context) =>
        {
            // Assert
            context.ShouldNotBeNull();

            isCompleted = true;
        };

        // Act
        trigger.ActivateTrigger(cancellationToken: TestContext.Current.CancellationToken);

        await AppsHelper.DelayWhileAsync(5000, () => trigger.PressureCounter != 2, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        trigger.ShouldNotBeNull();
        trigger.Counter.ShouldBe(2);
        isTriggered.ShouldBe(0);
        trigger.PressureCounter.ShouldBe(2);

        isFaulted.ShouldBeFalse();
        isCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task ActivateTrigger_Faulted_OnTriggered()
    {
        // Arrange
        IFlexiSphereEventTrigger trigger = new FlexiSphereEventTrigger();
        var action = new Func<IFlexiSphereContext?, Task<bool>>((context) => Task.FromResult(true));
        trigger.ConfigureTrigger(action, 500);
        trigger.MaxOccurrences = 2;

        int isTriggered = 0;

        // Cannot be async
        trigger.OnTriggered += (sender, context) =>
        {
            Interlocked.Increment(ref isTriggered);
            if (isTriggered == 1)
            {
                throw new Exception("Test exception");
            }

            Task.Delay(5000, cancellationToken: TestContext.Current.CancellationToken).Wait(cancellationToken: TestContext.Current.CancellationToken);
        };

        bool isFaulted = false;
        trigger.OnFaulted += (sender, context, exception) =>
        {
            // Assert
            context.ShouldBeNull();

            isFaulted = true;
        };

        bool isCompleted = false;
        trigger.OnCompleted += (sender, context) =>
        {
            // Assert
            context.ShouldBeNull();

            isCompleted = true;
        };

        // Act
        trigger.ActivateTrigger(cancellationToken: TestContext.Current.CancellationToken);

        await AppsHelper.DelayWhileAsync(5000, () => isTriggered < 2, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        trigger.ShouldNotBeNull();
        trigger.Counter.ShouldBeGreaterThanOrEqualTo(2);
        isTriggered.ShouldBe(2);

        isFaulted.ShouldBeTrue();
        isCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task ActivateTrigger_Faulted_OnCompleted()
    {
        // Arrange
        IFlexiSphereEventTrigger trigger = new FlexiSphereEventTrigger();
        var action = new Func<IFlexiSphereContext?, Task<bool>>((context) => Task.FromResult(true));
        trigger.ConfigureTrigger(action, 500);
        trigger.MaxOccurrences = 2;

        int isTriggered = 0;
        trigger.OnTriggered += (sender, context) =>
        {
            Interlocked.Increment(ref isTriggered);
        };

        bool isFaulted = false;
        trigger.OnFaulted += (sender, context, exception) =>
        {
            isFaulted = true;
        };

        bool isCompleted = false;
        trigger.OnCompleted += (sender, context) =>
        {
            isCompleted = true;

            throw new Exception("Test exception");
        };

        // Act
        trigger.ActivateTrigger(cancellationToken: TestContext.Current.CancellationToken);

        await AppsHelper.DelayWhileAsync(5000, () => !isFaulted, cancellationToken: TestContext.Current.CancellationToken);

        isFaulted.ShouldBeTrue();
        isCompleted.ShouldBeTrue();
    }

    [Fact]
    public async Task ActivateTrigger_Faulted_OnException()
    {
        // Arrange
        IFlexiSphereEventTrigger trigger = new FlexiSphereEventTrigger();
        var action = new Func<IFlexiSphereContext?, Task<bool>>((context) => Task.FromResult(true));
        trigger.ConfigureTrigger(action, 500);
        trigger.MaxOccurrences = 2;

        int isTriggered = 0;
        trigger.OnTriggered += (sender, context) =>
        {
            Interlocked.Increment(ref isTriggered);

            if (isTriggered == 1)
            {
                throw new Exception("Test exception");
            }

            Task.Delay(5000, cancellationToken: TestContext.Current.CancellationToken).Wait(cancellationToken: TestContext.Current.CancellationToken);
        };

        int isFaulted = 0;
        trigger.OnFaulted += (sender, context, exception) =>
        {
            // Assert
            context.ShouldBeNull();

            isFaulted++;
            throw new Exception("Test exception");
        };

        bool isCompleted = false;
        trigger.OnCompleted += (sender, context) =>
        {
            // Assert
            context.ShouldBeNull();

            isCompleted = true;
        };

        // Act
        trigger.ActivateTrigger(cancellationToken: TestContext.Current.CancellationToken);

        await AppsHelper.DelayWhileAsync(5000, () => isTriggered < 2, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        trigger.ShouldNotBeNull();
        trigger.Counter.ShouldBeGreaterThanOrEqualTo(2);
        isTriggered.ShouldBe(2);

        isFaulted.ShouldBe(1);
        isCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task ActivateTrigger_LimitOcurrences()
    {
        // Arrange
        IFlexiSphereEventTrigger trigger = new FlexiSphereEventTrigger();
        var action = new Func<IFlexiSphereContext?, Task<bool>>((context) => Task.FromResult(true));
        trigger.ConfigureTrigger(action, 500, 1, 2);
        trigger.MaxOccurrences.ShouldBe(2);

        int isTriggered = 0;
        trigger.OnTriggered += (sender, args) =>
        {
            Interlocked.Increment(ref isTriggered);
        };

        bool isFaulted = false;
        trigger.OnFaulted += (sender, context, exception) =>
        {
            // Assert
            context.ShouldBeNull();

            isFaulted = true;
        };

        bool isCompleted = false;
        trigger.OnCompleted += (sender, context) =>
        {
            // Assert
            context.ShouldBeNull();

            isCompleted = true;
        };

        // Act
        trigger.ActivateTrigger(cancellationToken: TestContext.Current.CancellationToken);

        await AppsHelper.DelayWhileAsync(5000, () => !isCompleted, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        trigger.ShouldNotBeNull();
        trigger.Counter.ShouldBe(2);
        isTriggered.ShouldBe(2);

        isFaulted.ShouldBeFalse();
        isCompleted.ShouldBeTrue();
    }

    [Fact]
    public async Task ActivateTrigger_Cancel()
    {
        // Arrange
        IFlexiSphereEventTrigger trigger = new FlexiSphereEventTrigger();
        var action = new Func<IFlexiSphereContext?, Task<bool>>((context) => Task.FromResult(true));
        trigger.ConfigureTrigger(action, 500);

        using CancellationTokenSource cts = new();

        bool isTriggered = false;
        trigger.OnTriggered += (sender, args) =>
        {
            isTriggered = true;
            cts.Cancel();
        };

        bool isCanceled = false;
        trigger.OnCanceled += (sender, args) => isCanceled = true;

        bool isFaulted = false;
        trigger.OnFaulted += (sender, context, exception) =>
        {
            isFaulted = true;
        };

        bool isCompleted = false;
        trigger.OnCompleted += (sender, context) =>
        {
            isCompleted = true;
        };

        // Act
        trigger.ActivateTrigger(null, cts.Token);

        await AppsHelper.DelayWhileAsync(5000, () => !isCanceled, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        trigger.ShouldNotBeNull();

        isTriggered.ShouldBeTrue();
        isCanceled.ShouldBeTrue();
        isFaulted.ShouldBeFalse();
        isCompleted.ShouldBeTrue();
    }

    [Fact]
    public async Task ActivateTrigger_LimitOcurrences_FalseCheck()
    {
        // Arrange
        IFlexiSphereEventTrigger trigger = new FlexiSphereEventTrigger();
        var action = new Func<IFlexiSphereContext?, Task<bool>>((context) => Task.FromResult(false));
        trigger.ConfigureTrigger(action, 500);
        trigger.MaxOccurrences = 2;

        int isTriggered = 0;
        trigger.OnTriggered += (sender, args) =>
        {
            Interlocked.Increment(ref isTriggered);
        };

        bool isFaulted = false;
        trigger.OnFaulted += (sender, context, exception) =>
        {
            // Assert
            context.ShouldBeNull();

            isFaulted = true;
        };

        bool isCompleted = false;
        trigger.OnCompleted += (sender, context) =>
        {
            // Assert
            context.ShouldBeNull();

            isCompleted = true;
        };

        // Act
        trigger.ActivateTrigger(cancellationToken: TestContext.Current.CancellationToken);

        await AppsHelper.DelayWhileAsync(5000, () => !isCompleted, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        trigger.ShouldNotBeNull();
        trigger.Counter.ShouldBe(2);
        isTriggered.ShouldBe(0);

        isFaulted.ShouldBeFalse();
        isCompleted.ShouldBeTrue();
    }
}
