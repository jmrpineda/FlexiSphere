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
// FileName: FlexiSphereFactoryTest.cs
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

using Microsoft.Extensions.DependencyInjection;
using Moq;

using Shouldly;

namespace ark.FlexiSphere.test;

public class FlexiSphereFactoryTest : IClassFixture<TestFixture>
{
    private readonly TestFixture _testFixture;

    public FlexiSphereFactoryTest(TestFixture testFixture)
    {
        _testFixture = testFixture;
    }

    [Fact]
    public void CreateFactoryInstance()
    {
        // Arrange
        var Factory = FlexiSphereFactory.Create();

        // Act
        Factory.ShouldNotBeNull();
    }

    [Fact]
    public void Build()
    {
        // Arrange
        var Factory = FlexiSphereFactory.Create();

        // Act
        var tsphere = Factory.Build();

        // Assert
        tsphere.ShouldNotBeNull();
    }

    [Fact]
    public void AddJob()
    {
        // Arrange
        var Factory = FlexiSphereFactory.Create();
        var job = new Mock<IFlexiSphereJob>();

        // Act
        Factory.AddJob(job.Object);

        // Assert
        Factory.Build().Jobs.Count.ShouldBe(1);
    }

    [Fact]
    public void AddJobWithAction()
    {
        // Arrange
        var Factory = FlexiSphereFactory.Create();

        // Act
        Factory.AddJob(b => b.WithJobName("Test", "").SetJobAction((context) => { return Task.FromResult(true); }));

        // Assert
        Factory.Build().Jobs.Count.ShouldBe(1);
    }

    [Fact]
    public void AddTrigger()
    {
        // Arrange
        var Factory = FlexiSphereFactory.Create();
        var trigger = new Mock<IFlexiSphereTrigger>();

        // Act
        Factory.AddTrigger(trigger.Object);

        // Assert
        Factory.Build().Triggers.Count.ShouldBe(1);
    }

    [Fact]
    public void AddTriggerWithAction()
    {
        // Arrange
        var Factory = FlexiSphereFactory.Create();

        // Act
        Factory.AddTrigger(b => b.WithTriggerName("Test", "").StartOn("5 0 * 8 *"));

        // Assert
        Factory.Build().Triggers.Count.ShouldBe(1);
    }

    [Fact]
    public void Factory_Setup_WithDI()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFlexiSphere(options =>
        {
            options.JobFactoryOptions = new();
            options.JobFactoryOptions.MaxConcurrents = 5;

            options.TriggerFactoryOptions = new();
            options.TriggerFactoryOptions.MaxConcurrents = 5;
        });

        var serviceProvider = services.BuildServiceProvider();
        var sphereFactory = serviceProvider.GetRequiredService<IFlexiSphereComponentFactory>();

        // Act
        var sphere = sphereFactory
             .AddTrigger(b => b.WithTriggerName("Test", "").StartOn("5 0 * 8 *"))
             .AddJob(b => b.WithJobName("Test", "").SetJobAction((context) => Task.FromResult(true)))
             .Build();

        // Assert
        sphere.ShouldNotBeNull();
        sphere.Triggers.Count.ShouldBe(1);
        sphere.Jobs.Count.ShouldBe(1);

        sphere.Triggers.First().MaxConcurrents.ShouldBe(5);
        sphere.Jobs.First().MaxConcurrents.ShouldBe(5);
    }

    [Fact]
    public void Factory_Setup_WithDI_JobFactory_CustomOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFlexiSphere<FakeClass_FlexiSphereFactory>(options =>
        {
            options.JobFactoryOptions = new();
            options.JobFactoryOptions.MaxConcurrents = 5;
        });

        var serviceProvider = services.BuildServiceProvider();
        var sphereFactory = serviceProvider.GetRequiredService<IFlexiSphereComponentFactory>();

        // Act
        var sphere = sphereFactory
            .AddTrigger(b => b.WithTriggerName("Test", "").StartOn("5 0 * 8 *"))
            .AddJob(b => b.WithJobName("Test", "").SetJobAction((context) => Task.FromResult(true)))
            .Build();

        // Assert
        sphere.ShouldNotBeNull();
        sphere.ShouldBeOfType<FakeClass_FlexiSphere>();
    }

    [Fact]
    public void Factory_Setup_WithDI_JobFactory_NoOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFlexiSphere<FakeClass_FlexiSphereFactory>();

        var serviceProvider = services.BuildServiceProvider();
        var sphereFactory = serviceProvider.GetRequiredService<IFlexiSphereComponentFactory>();

        // Act
        var sphere = sphereFactory
            .AddTrigger(b => b.WithTriggerName("Test", "").StartOn("5 0 * 8 *").SetMaxConcurrents(5).SetMaxOccurences(5))
            .AddJob(b => b.WithJobName("Test", "").SetJobAction((context) => Task.FromResult(true)))
            .Build();

        // Assert
        sphere.ShouldNotBeNull();
        sphere.ShouldBeOfType<FakeClass_FlexiSphere>();
    }
}
