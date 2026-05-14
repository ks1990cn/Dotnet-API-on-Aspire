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

// SQL Server
var sql = builder.AddSqlServer("sql")
                 .WithLifetime(ContainerLifetime.Persistent);

var db = sql.AddDatabase("appdb");
// OLLAMA
// Define the volume for persistence
var ollama = builder
    .AddContainer("ollama", "ollama/ollama")
    .WithHttpEndpoint(port: 11434, targetPort: 11434, name: "http")
    .WithBindMount(@"C:\ollama-data", "/root/.ollama")
    .WithEnvironment("OLLAMA_MODELS", "/root/.ollama/models")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEntrypoint("/bin/sh")
    // Use a flat string without any line breaks to avoid \r
    .WithArgs("-c", "ollama serve & sleep 5 && ollama pull qwen2.5:7b && wait");
// docker exec -it ollama-6947438b ollama pull qwen2.5:7b
// WEB API
builder.AddProject<Projects.BambooCards_Assessment>("BambooCards")
    .WithReference(redis)
    .WithReference(keycloak)
    .WaitFor(redis)
    .WaitFor(keycloak);

builder.AddProject<Projects.BambooCards_AI>("bamboocards-ai")
    .WithReference(db); 

builder.Build().Run();
