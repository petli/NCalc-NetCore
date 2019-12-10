using System.Threading.Tasks;

namespace NCalc
{
    using System.Collections.Generic;
    using System.Collections;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System;

    using Antlr.Runtime;

    using NCalc.Domain;

    public class Expression
    {
        public EvaluateOptions Options { get; set; }

        private bool IgnoreCase { get { return (Options & EvaluateOptions.IgnoreCase) == EvaluateOptions.IgnoreCase; } }

        /// <summary>
        /// Textual representation of the expression to evaluate.
        /// </summary>
        protected string OriginalExpression;

        private readonly Func<string, ParameterArgs, Task> parameterEvaluator;
        private readonly Func<string, FunctionArgs, Task> functionEvaluator;

        /// <summary>
        /// Construct a new expression from a string.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="options"></param>
        /// <param name="parameterEvaluator">Async parameter evaluator, which can be overridden when calling <see cref="EvaluateAsync"/>.</param>
        /// <param name="functionEvaluator">Async function evaluator, which can be overridden when calling <see cref="EvaluateAsync"/>.</param>
        public Expression(string expression, EvaluateOptions options = EvaluateOptions.None, Func<string, ParameterArgs, Task> parameterEvaluator = null, Func<string, FunctionArgs, Task> functionEvaluator = null)
        {
            if (String.IsNullOrEmpty(expression))
                throw new
            ArgumentException("Expression can't be empty", "expression");

            OriginalExpression = expression;
            Options = options;
            this.parameterEvaluator = parameterEvaluator;
            this.functionEvaluator = functionEvaluator;
        }

        /// <summary>
        /// Construct a new expression from an already parsed <see cref="LogicalExpression"/>.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="options"></param>
        /// <param name="parameterEvaluator">Async parameter evaluator, which can be overridden when calling <see cref="EvaluateAsync"/>.</param>
        /// <param name="functionEvaluator">Async function evaluator, which can be overridden when calling <see cref="EvaluateAsync"/>.</param>
        public Expression(LogicalExpression expression, EvaluateOptions options = EvaluateOptions.None, Func<string, ParameterArgs, Task> parameterEvaluator = null, Func<string, FunctionArgs, Task> functionEvaluator = null)
        {
            if (expression == null)
                throw new
            ArgumentException("Expression can't be null", "expression");
            
            ParsedExpression = expression;
            Options = options;
            this.parameterEvaluator = parameterEvaluator;
            this.functionEvaluator = functionEvaluator;
        }

        #region Cache management

        private static bool cacheEnabled = true;
        private static Dictionary<string, WeakReference> compiledExpressions = new Dictionary<string, WeakReference>();
        private static readonly ReaderWriterLockSlim Rwl = new ReaderWriterLockSlim();

        public static bool CacheEnabled
        {
            get { return cacheEnabled; }
            set
            {
                cacheEnabled = value;

                if (!CacheEnabled)
                {
                    // Clears cache
                    compiledExpressions = new Dictionary<string, WeakReference>();
                }
            }
        }

        /// <summary>
        /// Removed unused entries from cached compiled expression
        /// </summary>
        private static void CleanCache()
        {
            var keysToRemove = new List<string>();

            try
            {
                Rwl.TryEnterReadLock(Timeout.Infinite);
                keysToRemove.AddRange(compiledExpressions.Where(de => !de.Value.IsAlive).Select(de => de.Key));

                foreach (string key in keysToRemove)
                {
                    compiledExpressions.Remove(key);
                    //Trace.TraceInformation("Cache entry released: " + key);
                }
            }
            finally
            {
                Rwl.ExitReadLock();
            }
        }

        #endregion Cache management

        public static LogicalExpression Compile(string expression, bool nocache)
        {
            LogicalExpression logicalExpression = null;

            if (cacheEnabled && !nocache)
            {
                try
                {
                    Rwl.TryEnterReadLock(Timeout.Infinite);

                    if (compiledExpressions.ContainsKey(expression))
                    {
                        //Trace.TraceInformation("Expression retrieved from cache: " + expression);
                        var wr = compiledExpressions[expression];
                        logicalExpression = wr.Target as LogicalExpression;

                        if (wr.IsAlive && logicalExpression != null)
                        {
                            return logicalExpression;
                        }
                    }
                }
                finally
                {
                    Rwl.ExitReadLock();
                }
            }

            if (logicalExpression == null)
            {
                var lexer = new NCalcLexer(new ANTLRStringStream(expression));
                var parser = new NCalcParser(new CommonTokenStream(lexer));

                logicalExpression = parser.ncalcExpression().value;

                if (parser.Errors != null && parser.Errors.Count > 0)
                {
                    throw new EvaluationException(String.Join(Environment.NewLine, parser.Errors.ToArray()));
                }

                if (cacheEnabled && !nocache)
                {
                    try
                    {
                        Rwl.TryEnterReadLock(Timeout.Infinite);
                        compiledExpressions[expression] = new WeakReference(logicalExpression);
                    }
                    finally
                    {
                        Rwl.ExitReadLock();
                    }

                    CleanCache();

                    //Trace.TraceInformation("Expression added to cache: " + expression);
                }
            }

            return logicalExpression;
        }

        /// <summary>
        /// Pre-compiles the expression in order to check syntax errors.
        /// If errors are detected, the Error property contains the message.
        /// </summary>
        /// <returns>True if the expression syntax is correct, otherwiser False</returns>
        public bool HasErrors()
        {
            try
            {
                if (ParsedExpression == null)
                {
                    ParsedExpression = Compile(OriginalExpression, Options.NoCache());
                }

                // In case HasErrors() is called multiple times for the same expression
                return ParsedExpression != null && Error != null;
            }
            catch (Exception e)
            {
                Error = e.Message;
                return true;
            }
        }

        public string Error { get; private set; }

        public LogicalExpression ParsedExpression { get; private set; }

        protected Dictionary<string, IEnumerator> ParameterEnumerators;
        protected Dictionary<string, object> ParametersBackup;

        /// <summary>
        /// Evaluate an expression synchronously (legacy interface).
        /// </summary>
        /// <remarks>
        /// The event handlers on <see cref="EvaluateFunction"/> and <see cref="EvaluateParameter"/>
        /// are invoked before any async evaluator functions provided when constructing the object.
        /// </remarks>
        /// <returns></returns>
        public object Evaluate()
        {
            Task EvaluateFunctionAsync(string name, FunctionArgs args)
            {
                EvaluateFunction?.Invoke(name, args);
                return functionEvaluator?.Invoke(name, args) ?? Task.CompletedTask;
            }

            Task EvaluateParameterAsync(string name, ParameterArgs args)
            {
                EvaluateParameter?.Invoke(name, args);
                return parameterEvaluator?.Invoke(name, args) ?? Task.CompletedTask;
            }

            try
            {
                return EvaluateAsync(parameterEvaluator:EvaluateParameterAsync, functionEvaluator:EvaluateFunctionAsync).Result;
            }
            catch (AggregateException e)
            {
                if (e.InnerException != null)
                {
                    throw e.InnerException;
                }

                throw;
            }
        }

        /// <summary>
        /// Evaluate an expression asynchronously.
        /// </summary>
        /// <param name="parameterEvaluator">Override any async parameter evaluator provided when constructing the Expression.</param>
        /// <param name="functionEvaluator">Override any async function evaluator provided when constructing the Expression.</param>
        /// <returns>A result task that resolves to the result of the expression</returns>
        public async Task<object> EvaluateAsync(Func<string, ParameterArgs, Task> parameterEvaluator = null,
            Func<string, FunctionArgs, Task> functionEvaluator = null)
        {
            if (HasErrors())
            {
                throw new EvaluationException(Error);
            }

            if (ParsedExpression == null)
            {
                ParsedExpression = Compile(OriginalExpression, Options.NoCache());
            }

            var visitor = new EvaluationVisitor(Options, parameterEvaluator ?? this.parameterEvaluator, functionEvaluator ?? this.functionEvaluator);
            visitor.Parameters = Parameters;

            // if array evaluation, execute the same expression multiple times
            if (Options.IterateParameters())
            {
                int size = -1;
                ParametersBackup = new Dictionary<string, object>();
                foreach (string key in Parameters.Keys)
                {
                    ParametersBackup.Add(key, Parameters[key]);
                }

                ParameterEnumerators = new Dictionary<string, IEnumerator>();

                foreach (object parameter in Parameters.Values)
                {
                    var enumerable = parameter as IEnumerable;
                    if (enumerable != null)
                    {
                        int localsize = enumerable.Cast<object>().Count();

                        if (size == -1)
                        {
                            size = localsize;
                        }
                        else if (localsize != size)
                        {
                            throw new EvaluationException("When IterateParameters option is used, IEnumerable parameters must have the same number of items");
                        }
                    }
                }

                foreach (string key in Parameters.Keys)
                {
                    var parameter = Parameters[key] as IEnumerable;
                    if (parameter != null)
                    {
                        ParameterEnumerators.Add(key, parameter.GetEnumerator());
                    }
                }

                var results = new List<object>();
                for (int i = 0; i < size; i++)
                {
                    foreach (string key in ParameterEnumerators.Keys)
                    {
                        IEnumerator enumerator = ParameterEnumerators[key];
                        enumerator.MoveNext();
                        Parameters[key] = enumerator.Current;
                    }

                    await ParsedExpression.AcceptAsync(visitor);
                    results.Add(visitor.Result);
                }

                return results;
            }

            await ParsedExpression.AcceptAsync(visitor);
            return visitor.Result;
        }

        /// <summary>
        /// Function evaluator event handlers, only supported when calling <see cref="Evaluate"/>.
        /// </summary>
        /// <remarks>
        /// The event handlers will be called before any async function evaluator provided when constructing the <see cref="Expression"/>.
        /// </remarks>
        public event EvaluateFunctionHandler EvaluateFunction;

        /// <summary>
        /// Parameter evaluator event handlers, only supported when calling <see cref="Evaluate"/>.
        /// </summary>
        /// <remarks>
        /// The event handlers will be called before any async parameter evaluator provided when constructing the <see cref="Expression"/>.
        /// </remarks>
        public event EvaluateParameterHandler EvaluateParameter;

        private Dictionary<string, object> parameters;

        public Dictionary<string, object> Parameters
        {
            get { return parameters ?? (parameters = new Dictionary<string, object>(IgnoreCase ? StringComparer.CurrentCultureIgnoreCase : StringComparer.CurrentCulture)); }
            set { parameters = value; }
        }
    }
}