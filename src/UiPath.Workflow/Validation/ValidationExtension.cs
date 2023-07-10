using Microsoft.CSharp.Activities;
using Microsoft.VisualBasic.Activities;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace System.Activities.Validation
{
    internal sealed class ValidationExtension : IValidationExtension
    {
        public IEnumerable<ValidationError> PostValidate(Activity activity)
        {
            var validator = GetValidator(Scope.Language);
            return validator.Validate(activity, Scope);
        }

        public void PreCompileLambdaExpressions(Activity activity)
        {
            // clear for every xaml.
            Scope.ClearPreCompiledLambdaExpressions();

            var validator = GetValidator(Scope.Language);
            validator.PreCompileExpressions(activity, Scope,
                (returnTypeName, expressionText, expression) => Scope.SetPreCompiledLambdaExpression(returnTypeName, expressionText, expression));
        }

        public LambdaExpression GetPreCompiledLambdaExpression(string returnTypeName, string expressionText)
        {
            return Scope.GetPreCompiledLambdaExpression(returnTypeName, expressionText);
        }

        private static RoslynExpressionValidator GetValidator(string language)
        {
            return language switch
            {
                CSharpHelper.Language => CSharpExpressionValidator.Instance,
                VisualBasicHelper.Language => VbExpressionValidator.Instance,
                _ => throw new ArgumentException(language, nameof(language))
            };
        }

        public void QueueExpressionForValidation<T>(ExpressionToValidate expressionToValidate, string language)
        {
            Scope.AddExpression<T>(expressionToValidate, language);
        }

        public ValidationScope Scope { get; } = new();
    }
}
