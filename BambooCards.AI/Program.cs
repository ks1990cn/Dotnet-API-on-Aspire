using BambooCards.AI.MCPServer;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

#pragma warning disable SKEXP0070

builder.Services.AddSingleton<Kernel>(_ =>
{
    var kernelBuilder = Kernel.CreateBuilder();

    kernelBuilder.AddGoogleAIGeminiChatCompletion(
     modelId: "gemini-2.5-flash",
     apiKey: ""
 );
    var kernel = kernelBuilder.Build();

    // Register your tools (SK plugins)
    kernel.Plugins.AddFromObject(new TestTools(), "TestTools");

    return kernel;
});

#pragma warning restore SKEXP0070

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();