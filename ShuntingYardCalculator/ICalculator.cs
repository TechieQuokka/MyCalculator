namespace ShuntingYardCalculator
{
    /// <summary>
    /// Interface for a calculator that provides functionality to process and compute infix expressions.
    /// </summary>
    public interface ICalculator
    {
        /// <summary>
        /// A delegate that processes a custom string function embedded within an infix expression,
        /// allowing dynamic transformation or handling of the expression based on the custom function's behavior.
        /// </summary>
        /// <param name="calculator">The current calculator instance that is performing the operation.</param>
        /// <param name="expression">The complete infix expression containing the custom function to process.</param>
        /// <param name="name">The name of the custom function (e.g., ABS, SQRT, etc.).</param>
        /// <param name="value">The argument or value provided to the custom function within the expression.</param>
        /// <returns>A string representing the processed result or the transformed expression.</returns>
        public delegate string CustomStringHandler(ICalculator calculator, string expression, string name, string value);

        /// <summary>
        /// A property that holds a delegate of type CustomStringHandler. 
        /// This delegate is used to process custom string functions embedded in infix expressions.
        /// The setter is publicly accessible, allowing external assignment, 
        /// but the getter is protected, restricting access to the defining class and its inheritors.
        /// </summary>
        CustomStringHandler CustomString { protected get; set; }

        /// <summary>
        /// Adds a custom string handler delegate to handle and process embedded functions or operations
        /// within an infix expression. This allows the calculator to dynamically manage custom functionality.
        /// </summary>
        /// <param name="handler">The delegate instance of type CustomStringHandler to be added for processing custom functions.</param>
        void AddCustomStringHandler(CustomStringHandler handler);

        /// <summary>
        /// Removes a previously added custom string handler delegate that processes embedded functions 
        /// or operations within an infix expression. This prevents the calculator from using the specified handler.
        /// </summary>
        /// <param name="handler">The delegate instance of type CustomStringHandler to be removed from processing custom functions.</param>
        void RemoveCustomStringHandler(CustomStringHandler handler);

        /// <summary>
        /// Computes the result of the given infix expression and returns the output.
        /// </summary>
        /// <param name="expression">The infix expression string to compute.</param>
        /// <returns>A decimal value representing the computation result.</returns>
        decimal Compute(string expression);

        /// <summary>
        /// Generates an array of transformed expressions based on the given infix expression.
        /// If the expression contains special symbols such as [ ], they are handled and transformed according to specific rules.
        /// </summary>
        /// <param name="expression">The infix expression string to transform.</param>
        /// <returns>An enumerable collection of strings representing the transformed expressions.</returns>
        IEnumerable<string> GenerateTransformedArray(string expression);

        /// <summary>
        /// Normalizes the given infix expression by correcting omitted or ambiguous parts
        /// and returns a refined expression in string format.
        /// </summary>
        /// <param name="expression">The infix expression string to normalize.</param>
        /// <returns>A normalized string representation of the infix expression.</returns>
        string Normalize(string expression);
    }
}
