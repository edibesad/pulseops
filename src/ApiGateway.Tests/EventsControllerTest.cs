using System;
using ApiGateway.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ApiGateway.Tests;

public class EventsControllerTest
{
    [Fact]
    public async Task IncidentStarted_Should_Publish_Event_And_Return_Accepted()
    {
        var mockPublisher = new Mock<IEventPublisher>();
        var controller = new EventsController(mockPublisher.Object);
        var evt = new IncidentStarted(Guid.NewGuid().ToString(), "Source", "0", DateTime.UtcNow, "Tr", "0");

        var result = await controller.IncidentStarted(evt);

        mockPublisher.Verify(
            x => x.PublishAsync(It.IsAny<IncidentStarted>()), Times.Once
        );

        var acceptedResult = Assert.IsType<AcceptedResult>(result);

        Assert.True((bool)acceptedResult.Value.GetType().GetProperty("ok").GetValue(acceptedResult.Value));
    }
}
