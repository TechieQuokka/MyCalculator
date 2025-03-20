using System.Text;
using System.Text.RegularExpressions;

namespace ShuntingYardCalculator
{
    public class StringCalculator : ICalculator
    {
        protected readonly IDictionary<char, int> Table = new Dictionary<char, int>
        { { '^', 4 }, { '*', 3 }, { '/', 3 }, { '+', 2 }, { '-', 2 }, { '(', 1 }, };

        protected readonly string ERROR_STRING = " !@#$%&_=`~<>?:;\'\"\\";

        public ICalculator.CustomStringHandler CustomString { get => this._customString; set => this._customString = value; }
        private ICalculator.CustomStringHandler _customString;

        public StringCalculator()
        {
            this._customString = delegate { return string.Empty; };
            return;
        }

        public StringCalculator(ICalculator.CustomStringHandler handler)
        {
            if (handler is null) throw new ArgumentNullException(nameof(handler));

            this._customString = handler;
            return;
        }

        public decimal Compute(string expression)
        {
            if (expression is null) throw new ArgumentNullException(nameof(expression));
            if (this.HasInvalidCharacters(expression, this.ERROR_STRING)) throw new ArgumentException("The expression contains invalid characters.");
            if (!this.ParenthesesValidation(expression)) throw new InvalidOperationException("Cannot process expression due to mismatched parentheses.");
            if (Regex.IsMatch(expression, "---")) throw new InvalidOperationException("The operation is not allowed when the input contains consecutive '-' characters.");
            if (!this.CheckParenthesisRules(expression)) throw new FormatException("Invalid expression format: Parentheses are not allowed immediately after a digit.");

            expression = this.NormalizeExpression(expression);
            expression = this.AdjustExpression(expression);

            var infix = this.ExpressionVarMapper(expression);
            IList<int> postfix = this.ToPostfix(infix.Expression, infix.Expression.Count);
            decimal result = this.CalculatePostfix(postfix, infix.Values);

            return result;
        }

        public IEnumerable<string> GenerateTransformedArray(string expression)
        {
            if (expression is null) throw new ArgumentNullException(nameof(expression));
            if (this.HasInvalidCharacters(expression, this.ERROR_STRING)) throw new ArgumentException("The expression contains invalid characters.");
            if (!this.ParenthesesValidation(expression)) throw new InvalidOperationException("Cannot process expression due to mismatched parentheses.");
            if (Regex.IsMatch(expression, "---")) throw new InvalidOperationException("The operation is not allowed when the input contains consecutive '-' characters.");
            if (!this.CheckParenthesisRules(expression)) throw new FormatException("Invalid expression format: Parentheses are not allowed immediately after a digit.");

            return this.GenerateTransformedArrayRecursive(expression);
        }

        private IEnumerable<string> GenerateTransformedArrayRecursive(string expression)
        {
            if (!this.CheckForBracket(expression))
            {
                yield return expression;
                yield break;
            }

            IList<string> data = new List<string>();
            IList<int> buffer = new List<int>();
            for (int index = 0, pivot = 128; index < expression.Length; index++)
            {
                char element = expression[index];
                if (element == '[')
                {
                    int end = this.FindClosingBracket(expression, index, expression.Length, '[', ']');
                    data.Add(expression.Substring(index, end + 1 - index));
                    index += end - index;

                    buffer.Add(pivot++);
                    continue;
                }

                buffer.Add((int)element);
            }

            foreach (string[] operand in this.GetCombination(data))
            {
                IList<int> format = buffer;
                var builder = new StringBuilder();

                for (int index = 0, pivot = 0; index < format.Count; index++)
                {
                    int element = format[index];
                    if (!this.IsVariable(element))
                    {
                        builder.Append((char)element);
                        continue;
                    }

                    builder.Append(operand[pivot++]);
                }

                string temp = builder.ToString();
                temp = Regex.Replace(temp, @"\+\+|--", "+");
                temp = Regex.Replace(temp, @"\+-", "-");
                temp = Regex.Replace(temp, @"-\+", "-");
                foreach (var result in this.GenerateTransformedArrayRecursive(temp))
                {
                    yield return result;
                }
            }
        }

        public string Normalize(string expression)
        {
            if (expression is null) throw new ArgumentNullException(nameof(expression));
            if (!this.ParenthesesValidation(expression)) throw new InvalidOperationException("Cannot process expression due to mismatched parentheses.");

            expression = expression.Replace(" ", "");

            for (int index = 0; index < expression.Length; index++)
            {
                char character = expression[index];
                if (!char.IsLetter(character)) continue;

                int pivot = index;
                while (pivot < expression.Length && expression[pivot] != '(') pivot++;

                int position = this.FindClosingBracket(expression, pivot, expression.Length, '(', ')');
                if (position == -1) throw new FormatException("An error occurred while parsing the format string: '{formatString}'. Verify its structure and syntax.");

                string command = expression.Substring(index, position - index + 1);
                string[] data = command.Split('(', 2);

                data[1] = data[1].Substring(0, data[1].Length - 1);

                bool flag = false;
                foreach (ICalculator.CustomStringHandler function in this.CustomString.GetInvocationList())
                {
                    string result = function(this, expression, data[0], data[1]);
                    if (string.IsNullOrWhiteSpace(result)) continue;

                    expression = expression.Replace(command, result);
                    flag = true;
                }
                if (flag) index--;
            }

            var builder = new StringBuilder();
            for (int index = 0; index < expression.Length; index++)
            {
                char character = expression[index];
                if (character == '(' && index > 0 && (char.IsDigit(expression[index - 1]) || expression[index - 1] == '.')) builder.Append('*');

                builder.Append(character);
            }
            return builder.ToString();
        }

        protected decimal Calculate(decimal lvalue, decimal rvalue, char @operator)
        {
            switch (@operator)
            {
                case '+':
                    return lvalue + rvalue;
                case '-':
                    return lvalue - rvalue;
                case '*':
                    return lvalue * rvalue;
                case '/':
                    return lvalue / rvalue;
                case '^':
                    return (decimal)Math.Pow((double)lvalue, (double)rvalue);
                default:
                    throw new ArgumentException($"Unsupported operator: {@operator}", nameof(@operator));
            }
        }

        protected ExpressionType ExpressionVarMapper (string expression)
        {
            int length = expression.Length;
            var builder = new List<int>();

            ExpressionType result = new ExpressionType { Expression = new List<int>(), Values = new List<decimal>() };

            int pivot = 128;
            int sign = 1;
            for (int index = 0; index < length; index++)
            {
                char element = expression[index];
                if (!char.IsDigit(element))
                {
                    if (element == '-' && (index == 0 || !char.IsDigit(expression[index - 1]) && expression[index - 1] != 41)) sign = -sign;
                    else builder.Add((int)element);
                    continue;
                }

                int end = this.GetLastNumberIndex(expression, index, length);
                bool isSuccess = decimal.TryParse(expression.Substring(index, end + 1 - index), out var value);
                if (!isSuccess) throw new FormatException("The expression is invalid.");

                result.Values.Add(value * sign);
                builder.Add(pivot++);

                index += end - index;
                sign = 1;
            }

            result.Expression = builder;
            return result;
        }

        private int GetLastNumberIndex(string expression, int start, int end)
        {
            int index = 0;
            for (index = start; index < end; index++)
            {
                char element = expression[index];
                if (!char.IsDigit(element) && element != '.') break;
            }
            return index - 1;
        }

        private bool HasInvalidCharacters(string source, string error_string)
        {
            foreach (char character in source)
            {
                if (char.IsLetter(character) || (int)character > 127 ||
                    error_string.Contains(character)) return true;
            }
            return false;
        }

        protected IList<int> ToPostfix (IList<int> infix, int length)
        {
            var builder = new List<int>();
            var stack = new Stack<int>();

            for (int index = 0; index < length; index++)
            {
                int element = infix[index];
                if (this.IsVariable(element)) builder.Add(element);
                else if (!this.Table.ContainsKey((char)element))
                {
                    while (stack.Peek() != 40) builder.Add(stack.Pop());
                    stack.Pop();
                }
                else if (stack.Count == 0 || element == 40) stack.Push(element);
                else
                {
                    while (stack.Count > 0 && this.Table[(char)element] <= this.Table[(char)stack.Peek()]) builder.Add(stack.Pop());
                    stack.Push(element);
                }
            }

            while (stack.Count > 0) builder.Add(stack.Pop());
            return builder;
        }

        protected bool IsVariable(int element)
        {
            return element > 127;
        }

        protected decimal CalculatePostfix(IList<int> expression, IList<decimal> values)
        {
            var stack = new Stack<decimal>();
            if (expression.Count <= 2)
            {
                if (expression.Count == 1) return values[0];
                return 0;
            }

            int length = expression.Count;
            for (int index = 0; index < length; index++)
            {
                int element = expression[index];
                if (this.IsVariable(element))
                {
                    stack.Push(values[element - 128]);
                    continue;
                }

                decimal rvalue = stack.Pop();
                decimal lvalue = stack.Pop();
                stack.Push(this.Calculate(lvalue, rvalue, (char)element));
            }
            return stack.Pop();
        }

        protected bool ParenthesesValidation(string expression)
        {
            int length = expression.Length;
            var stack = new Stack<char>();

            for (int index = 0; index < length; index++)
            {
                char element = expression[index];
                switch (element)
                {
                    case '(':
                    case '{':
                    case '[':
                        stack.Push(element);
                        break;
                    case ')':
                        if (stack.Count == 0 || stack.Peek() != '(') return false;
                        stack.Pop();
                        break;
                    case '}':
                        if (stack.Count == 0 || stack.Peek() != '{') return false;
                        stack.Pop();
                        break;
                    case ']':
                        if (stack.Count == 0 || stack.Peek() != '[') return false;
                        stack.Pop();
                        break;
                }
            }
            return stack.Count == 0;
        }

        protected string NormalizeExpression(string expression)
        {
            StringBuilder builder = new StringBuilder(expression.Length);

            foreach (char character in expression)
            {
                switch (character)
                {
                    case ' ':
                    case '=':
                        continue;
                    case '{':
                    case '[':
                        builder.Append('(');
                        break;
                    case '}':
                    case ']':
                        builder.Append(')');
                        break;
                    default:
                        builder.Append(character);
                        break;
                }
            }

            return builder.ToString();
        }

        protected string AdjustExpression(string expression)
        {
            StringBuilder builder = new StringBuilder();

            int index = 0;
            foreach (char character in expression)
            {
                switch (character)
                {
                    case '-':
                        if (index == 0 || expression[index - 1] == '(')
                        {
                            builder.Append('0');
                        }
                        else if (expression[index - 1] == '*' || expression[index - 1] == '/' || expression[index - 1] == '^')
                        {
                            builder.Append(string.Format("-1{0}", expression[index - 1]));
                            break;
                        }
                        builder.Append(character);
                        break;
                    default:
                        if (index == 0 && character == '+') break;
                        builder.Append(character);
                        break;
                }
                index++;
            }

            return builder.ToString();
        }

        private int FindClosingBracket(string expression, int start, int end, char openBracket, char closeBracket)
        {
            var stack = new Stack<char>();
            for (int index = start; index < end; index++)
            {
                char element = expression[index];
                if (element == openBracket) stack.Push(element);
                else if (element == closeBracket) stack.Pop();

                if (stack.Count == 0) return index;
            }
            return -1;
        }

        private bool CheckForBracket(string expression)
        {
            for (int index = 0; index < expression.Length; index++)
            {
                if (expression[index] == '[') return true;
            }
            return false;
        }

        private IEnumerable<string[]> GetCombinationRecursive(IList<string[]> source, int length, int depth, string[] buffer)
        {
            if (depth == length)
            {
                yield return buffer;
                yield break;
            }

            foreach (string element in source[depth])
            {
                buffer[depth] = element;
                foreach (var result in this.GetCombinationRecursive(source, length, depth + 1, buffer))
                {
                    yield return result;
                }
            }
        }

        protected IEnumerable<string[]> GetCombination(IList<string> data)
        {
            int length = data.Count;
            string[] buffer = new string[length];

            IList<string[]> source = new List<string[]>();
            foreach (string element in data)
            {
                string temp = element.Substring(1, element.Length - 2);
                source.Add(this.Split(temp, ',', '[', ']').ToArray<string>());
            }
            return this.GetCombinationRecursive(source, length, 0, buffer);
        }

        private IEnumerable<string> Split (string source, char separator, char openBracket, char closeBracket)
        {
            var builder = new StringBuilder();
            var stack = new Stack<char>();
            foreach (var element in source)
            {
                if (element == openBracket) stack.Push(element);
                else if (element == closeBracket) stack.Pop();

                if (element == separator && stack.Count == 0)
                {
                    yield return builder.ToString();
                    builder = new StringBuilder();
                    continue;
                }
                builder.Append(element);
            }
            yield return builder.ToString();
        }

        private bool CheckParenthesisRules(string expression)
        {
            for (int index = 1; index < expression.Length; index++)
            {
                char character = expression[index];
                if (character == '(' && char.IsDigit(expression[index - 1])) return false;
            }
            return true;
        }

        public void AddCustomStringHandler(ICalculator.CustomStringHandler handler)
        {
            if (handler is null) throw new ArgumentNullException(nameof(handler));
            this._customString += handler;
            return;
        }

        public void RemoveCustomStringHandler(ICalculator.CustomStringHandler handler)
        {
            if (handler is null) throw new ArgumentNullException(nameof(handler));

#pragma warning disable CS8601
            this._customString -= handler;
#pragma warning restore CS8601
            return;
        }
    }
}
