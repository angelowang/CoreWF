using System.Linq.Expressions;

namespace System.Activities.Validation
{
    internal interface IValidationExtension
    {
        IEnumerable<ValidationError> PostValidate(Activity activity);

        // Enable precompiling VB expressions in a single pass to improve runtime execution performance.
        void PreCompileLambdaExpressions(Activity activity);
        LambdaExpression GetPreCompiledLambdaExpression(string returnTypeName, string expressionText);
    }
}
