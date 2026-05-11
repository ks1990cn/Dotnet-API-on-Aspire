using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BambooCards.AI.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.SemanticKernel;
    using Microsoft.SemanticKernel.ChatCompletion;
    using Microsoft.SemanticKernel.Connectors.Google;
    using Microsoft.SemanticKernel.Connectors.OpenAI;

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
    }
}
