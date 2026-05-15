using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Agents.AI.Workflows;

namespace BambooCards.AI.Executors
{
    // Executor to sum a list of integers
    public class MathExecutor() : Executor<List<int>, int>("MathExecutor")
    {
        public override ValueTask<int> HandleAsync(List<int> numbers, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(numbers.Sum());
        }
    }

    // Executor to format the result into a bill
    public class InventoryExecutor() : Executor<int, string>("InventoryExecutor")
    {
        public override ValueTask<string> HandleAsync(int totalAmount, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            string bill = $"--- FINAL BILL ---\nTotal Amount Due: ${totalAmount}.00\nThank you for your business!";
            return ValueTask.FromResult(bill);
        }
    }
}
