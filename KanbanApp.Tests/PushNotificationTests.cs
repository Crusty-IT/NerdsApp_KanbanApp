using KanbanApp.Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KanbanApp.Tests;

public class PushNotificationTests : TestBase
{
    public PushNotificationTests(KanbanWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task PushConfig_ReturnsNotConfiguredWhenVapidKeysAreMissing()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/push/config");

        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        data.GetProperty("isConfigured").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task CanSaveAndDisablePushSubscription()
    {
        var client = await CreateAuthenticatedClientAsync($"{Guid.NewGuid()}@test.com");
        var endpoint = $"https://push.example.test/{Guid.NewGuid()}";

        var saveResponse = await client.PostAsJsonAsync("/api/push/subscriptions", new
        {
            endpoint,
            keys = new
            {
                p256dh = "public-key",
                auth = "auth-secret"
            },
            userAgent = "Test Browser"
        });

        saveResponse.EnsureSuccessStatusCode();

        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/push/subscriptions")
        {
            Content = JsonContent.Create(new { endpoint })
        };
        var deleteResponse = await client.SendAsync(deleteRequest);

        deleteResponse.EnsureSuccessStatusCode();
        var data = await deleteResponse.Content.ReadFromJsonAsync<JsonElement>();
        data.GetProperty("removed").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task CanUpdateNotificationPreferences()
    {
        var client = await CreateAuthenticatedClientAsync($"{Guid.NewGuid()}@test.com");

        var updateResponse = await client.PutAsJsonAsync("/api/notifications/preferences", new
        {
            webPushEnabled = false,
            notifyCardMoved = false,
            includeCardDetails = false
        });

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<JsonElement>();
        updated.GetProperty("webPushEnabled").GetBoolean().Should().BeFalse();
        updated.GetProperty("notifyCardMoved").GetBoolean().Should().BeFalse();
        updated.GetProperty("includeCardDetails").GetBoolean().Should().BeFalse();
        updated.GetProperty("notifyCardCreated").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task LogoutDoesNotDeactivateOtherDevicePushSubscriptions()
    {
        var client = Factory.CreateClient();
        var email = $"{Guid.NewGuid()}@test.com";
        const string password = "Test123!";
        await client.PostAsJsonAsync("/register", new { email, password });

        var loginResponse = await client.PostAsJsonAsync("/login", new { email, password });
        loginResponse.EnsureSuccessStatusCode();
        var loginData = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginData.GetProperty("accessToken").GetString()!;
        var refreshToken = loginData.GetProperty("refreshToken").GetString()!;
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var endpoint = $"https://push.example.test/{Guid.NewGuid()}";
        var saveResponse = await client.PostAsJsonAsync("/api/push/subscriptions", new
        {
            endpoint,
            keys = new
            {
                p256dh = "public-key",
                auth = "auth-secret"
            }
        });
        saveResponse.EnsureSuccessStatusCode();

        var logoutResponse = await client.PostAsJsonAsync("/api/auth/logout", new { refreshToken });
        logoutResponse.EnsureSuccessStatusCode();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var subscription = await db.PushSubscriptions.FirstAsync(s => s.Endpoint == endpoint);
        subscription.IsActive.Should().BeTrue();
        subscription.RevokedAt.Should().BeNull();
    }
}
