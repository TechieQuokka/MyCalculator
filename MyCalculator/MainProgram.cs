using ShuntingYardCalculator;

namespace MyCalculator
{
    internal class MainProgram
    {
        private static void Main (string[] args)
        {
            ICalculator calculator = new StringCalculator();
            calculator.CustomString = (cal, expression, name, value) =>
            {
                if (name != "cal") return string.Empty;

                return cal.Compute(value).ToString();
            };

            calculator.AddCustomStringHandler((cal, expression, name, value) =>
            {
                if (name != "func") return string.Empty;

                return value;
            });

            Console.WriteLine(calculator.Compute("30+-4*(10)"));
            return;

            string expression = "(10 + 20) * cal (10 + 20) + [100, 200, func (func(cal(10 + 11)))] + cal (1 + 2 + 3) * [99, 98, 97]";
            //expression = "cal (10 + 20)";
            Console.WriteLine(expression);
            expression = calculator.Normalize(expression);
            foreach (string element in calculator.GenerateTransformedArray(expression))
            {
                Console.WriteLine(element);
            }
            //return;

            IList<decimal> buffer = new List<decimal>();
            using (StreamReader reader = new StreamReader("input.txt"))
            {
                string? expression1 = string.Empty;
                while ((expression1 = reader.ReadLine()) != null)
                {
                    decimal result = calculator.Compute(expression1.Replace(" ", ""));
                    buffer.Add(result);

                    Console.WriteLine(string.Format("{0} = {1}", expression1, result));
                }
            }

            using (StreamWriter writer = new StreamWriter("output.txt"))
            {
                foreach (decimal result in buffer)
                {
                    writer.WriteLine(result);
                }
            }
            return;
        }
    }
}
