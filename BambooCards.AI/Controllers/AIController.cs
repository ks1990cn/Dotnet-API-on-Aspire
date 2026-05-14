using System.ClientModel;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using OpenAI;
using OpenAI.Chat;
namespace BambooCards.AI.Controllers
{
    

    [ApiController]
    [Route("api/ai")]
    public class AIController : ControllerBase
    {
        private readonly Kernel _kernel;

        public AIController(Kernel kernel)
        {
            _kernel = kernel;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] string prompt)
        {
            var chatService = _kernel.GetRequiredService<IChatCompletionService>();

            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var executionSettings = new GeminiPromptExecutionSettings
            {
                ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions
            };
            var result = await chatService.GetChatMessageContentAsync(
                history,
                executionSettings: executionSettings,
                kernel: _kernel
            );

            return Ok(result.Content);
        }

        [HttpPost("agent")]
        public async Task<IActionResult> OpenAIAgentChat([FromBody] string prompt)
        {
            var openAiClient = new OpenAIClient(
                new ApiKeyCredential("ollama"),
                new OpenAIClientOptions
                {
                    Endpoint = new Uri("http://localhost:11434/v1")
                });

            var chatClient = openAiClient.GetChatClient("llama3");

            IChatClient client = chatClient.AsIChatClient();

            var agent = new ChatClientAgent(
                client,
                name: "Assistant",
                description: "Helpful assistant",
                instructions: "You are a helpful C# assistant.");

            var result = await agent.RunAsync(prompt);

            return Ok(result);
        }
    }
}
