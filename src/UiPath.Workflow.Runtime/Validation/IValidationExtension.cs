using System.Linq.Expressions;

namespace System.Activities.Validation
{
    internal interface IValidationExtension
    {
        IList<ValidationError> Validate(Activity activity);

        // Enable precompiling VB expressions in a single pass to improve runtime exeution performance.
        void PreCompileLambdaExpressions(Activity activity);
        LambdaExpression GetPreCompiledLambdaExpression(string returnTypeName, string expressionText);

        void QueueExpressionForValidation<T>(ExpressionToValidate expressionToValidate, string language);
    }
}
