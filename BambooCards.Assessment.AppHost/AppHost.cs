var builder = DistributedApplication.CreateBuilder(args);

// REDIS (dynamic port)
var redis = builder.AddRedis("redis");

// KEYCLOAK (dynamic port)
var keycloak = builder.AddKeycloakContainer("keycloak")
    .WithEnvironment("KC_HEALTH_ENABLED", "true")
    .WithEnvironment("KC_HTTP_ENABLED", "true")
    .WithEnvironment("KC_HOSTNAME_STRICT", "false")
    .WithEnvironment("KC_PROXY", "edge")
    .WithHttpHealthCheck("/").WithImport("../BambooCards.Keycloak.Realm/realm-export.json"); 



// WEB API
builder.AddProject<Projects.BambooCards_Assessment>("BambooCards")
    .WithReference(redis)
    .WithReference(keycloak)
    .WaitFor(redis)
    .WaitFor(keycloak);

builder.Build().Run();
