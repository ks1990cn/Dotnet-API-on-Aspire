using System.ComponentModel;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;
namespace BambooCards.AI.MCPServer
{
    public class TestTools
    {
        [KernelFunction]
        [Description("Sends a friendly greeting to a specific person by name.")] // ADD THIS
        public string Hello([Description("The name of the person to greet")] string name) // ADD THIS
        {
            return $"Hello {name}";
        }
    }
}
