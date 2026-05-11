using ModelContextProtocol.Server;
namespace BambooCards.AI.MCPServer
{
    [McpServerToolType]
    public class TestTools
    {
        [McpServerTool]
        public string Hello(string name)
        {
            return $"Hello {name}";
        }
    }
}
