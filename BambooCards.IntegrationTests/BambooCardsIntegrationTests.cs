using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace BambooCards.IntegrationTests
{
    public class BambooCardsIntegrationTests
    {
        [Fact]
        public async Task AppHost_Starts_And_ResourcesAreHealthy()
        {
            // 1. Arrange: Create the testing builder
            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BambooCards_Assessment_AppHost>();

            // 2. Act: Build and Start the application
            await using var app = await appHost.BuildAsync();
            await app.StartAsync();

            // 3. Assert: Verify the Web API is reachable
            var httpClient = app.CreateHttpClient("BambooCards");

            // Wait for health checks to pass if necessary
            var response = await httpClient.GetAsync("/health"); // Or your specific endpoint

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Redis_Resource_Reaches_Running_State()
        {
            // Arrange
            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BambooCards_Assessment_AppHost>();
            await using var app = await appHost.BuildAsync();
            await app.StartAsync();

            // 1. Get the ResourceNotificationService from the app's services
            var notificationService = app.Services.GetRequiredService<ResourceNotificationService>();

            // 2. Wait for the specific resource to reach the "Running" state
            // This will throw an exception if it doesn't reach the state within the default timeout
            await notificationService.WaitForResourceHealthyAsync("redis")
                            .WaitAsync(TimeSpan.FromSeconds(30));

            // 3. If we reached here, the resource is confirmed running
            Assert.True(true);
        }
    }
}
