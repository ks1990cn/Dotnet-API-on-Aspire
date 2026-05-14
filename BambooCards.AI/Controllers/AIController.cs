using System.ClientModel;
using BambooCards.AI.Tools;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using OpenAI;

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
                new OpenAIClientOptions { Endpoint = new Uri("http://localhost:11434/v1") });

            // 1. Get the underlying client and cast it to IChatClient
            IChatClient chatClient = openAiClient
                .GetChatClient("qwen2.5:7b")
                .AsIChatClient(); // This bridges OpenAI -> Microsoft.Extensions.AI

            var mathTools = new MathTools();
            var tools = new List<AITool>
    {
        AIFunctionFactory.Create(mathTools.Add),
        AIFunctionFactory.Create(mathTools.CreateInvoice)
    };

            // 2. Now AsAIAgent will recognize the IChatClient receiver
            AIAgent agent = chatClient.AsAIAgent(
                instructions: "You are a helpful assistant. Use tools whenever required.",
                tools: tools);

            // 3. Use agent.RunAsync to handle the tool-calling loop automatically
            var response = await agent.RunAsync(prompt);

            return Ok(response.Text);
        }
    }
}
