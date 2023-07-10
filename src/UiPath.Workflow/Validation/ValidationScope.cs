using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace System.Activities.Validation
{
    internal sealed class ValidationScope
    {
        private readonly Dictionary<string, ExpressionToValidate> _expressionsToValidate = new();
        private readonly Dictionary<KeyValuePair<string, string>, LambdaExpression> _lambdaExpressions = new();
        private string _language;

        internal void AddExpression<T>(ExpressionToValidate expressionToValidate, string language)
        {
            _language ??= language;
            if (_language != language)
            {
                expressionToValidate.Activity.AddTempValidationError(new ValidationError(SR.DynamicActivityMultipleExpressionLanguages(language), expressionToValidate.Activity));
                return;
            }

            if (expressionToValidate.Environment.IsPreCompilingExpressions)
            {
                var key = new KeyValuePair<string, string>(expressionToValidate.ResultType.Name, expressionToValidate.ExpressionText);
                if (_lambdaExpressions.ContainsKey(key))
                {
                    return;
                }

                _lambdaExpressions.Add(key, null);
            }

            _expressionsToValidate.Add(expressionToValidate.Activity.Id, expressionToValidate);
        }

        internal void ClearPreCompiledLambdaExpressions()
        {
            _lambdaExpressions.Clear();
        }

        internal void SetPreCompiledLambdaExpression(string returnTypeName, string expressionText, LambdaExpression expression)
        {
            _lambdaExpressions[new(returnTypeName, expressionText)] = expression;
        }

        internal LambdaExpression GetPreCompiledLambdaExpression(string returnTypeName, string expressionText)
        {
            LambdaExpression expression = null;
            _lambdaExpressions.TryGetValue(new(returnTypeName, expressionText), out expression);
            return expression;
        }

        internal string Language => _language;

        internal ExpressionToValidate GetExpression(string activityId) => _expressionsToValidate[activityId];

        internal ImmutableArray<ExpressionToValidate> GetAllExpressions() => _expressionsToValidate.Values.ToImmutableArray();

        internal void Clear() => _expressionsToValidate.Clear();
    }
}