using FluentAssertions;
using MassTransit;
using Moq;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Application.Vehicles.Commands.AddVehicleLog;
using ProhvatApp.Domain.Entities;
using ProhvatApp.Domain.Events;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using System.Reflection;

namespace ProhvatApp.Application.Tests.Vehicles.Commands;

public class AddVehicleLogCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IVehicleLogRepository> _repositoryMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly AddVehicleLogCommandHandler _handler;

    public AddVehicleLogCommandHandlerTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _repositoryMock = new Mock<IVehicleLogRepository>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();

        _handler = new AddVehicleLogCommandHandler(
            _contextMock.Object,
            _repositoryMock.Object,
            _publishEndpointMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldPublishEvent_WhenMaintenanceIntervalIsCrossed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var vehicle = new Vehicle
        {
            Id = vehicleId,
            UserId = userId,
            Brand = "Honda",
            Model = "CRF",
            CurrentMetricsValue = 100, // already at 100
            MaintenanceInterval = 100, // interval is 100
            OdometerType = Domain.Enums.OdometerType.MotoHours
        };

        var vehiclesData = new List<Vehicle> { vehicle }.AsQueryable();
        var mockDbSet = vehiclesData.BuildMockDbSet();

        _contextMock.Setup(c => c.Vehicles).Returns(mockDbSet.Object);

        var command = new AddVehicleLogCommand(
            vehicleId,
            userId,
            "Oil Change",
            "Changed oil",
            250, // jump to 250 (crosses 200 interval)
            new List<string>()
        );

        // Act
        var resultId = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultId.Should().NotBeEmpty();
        
        _publishEndpointMock.Verify(p => p.Publish(
            It.Is<MaintenanceExceededEvent>(e => 
                e.VehicleId == vehicleId && 
                e.UserId == userId &&
                e.CurrentMetricsValue == 250),
            It.IsAny<CancellationToken>()), Times.Once);
            
        vehicle.CurrentMetricsValue.Should().Be(250);
        
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ProhvatApp.Domain.Entities.VehicleLog>(), It.IsAny<CancellationToken>()), Times.Once);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
