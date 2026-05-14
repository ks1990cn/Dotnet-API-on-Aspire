using System.ComponentModel;

namespace BambooCards.AI.Tools
{
    public class MathTools
    {
        [Description("Add two numbers")]
        public int Add(int a, int b)
        {
            return a + b;
        }

        [Description("Generate invoice")]
        public string CreateInvoice(string customer, int amount)
        {
            return $"Invoice created for {customer} of {amount}";
        }
    }
}
