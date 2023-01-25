using System.Activities.Runtime;
using System.Activities.Runtime.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Markup;
namespace System.Activities.Statements;
[ContentProperty("Cases")]
public sealed class BpmSwitch<T> : BpmNode
{
    internal IDictionary<T, BpmNode> _cases  = new NullableKeyDictionary<T, BpmNode>();
    private CompletionCallback<T> _onCompleted;
    [DefaultValue(null)]
    public Activity<T> Expression { get; set; }
    [DefaultValue(null)]
    public BpmNode Default { get; set; }
    public IDictionary<T, BpmNode> Cases => _cases;
    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        if (Expression == null)
        {
            metadata.AddValidationError(SR.FlowSwitchRequiresExpression(Owner.DisplayName));
        }
        else
        {
            metadata.AddChild(Expression);
        }
    }
    internal override void GetConnectedNodes(IList<BpmNode> connections)
    {
        foreach (KeyValuePair<T, BpmNode> item in Cases)
        {
            connections.Add(item.Value);
        }
        if (Default != null)
        {
            connections.Add(Default);
        }
    }
    protected override void Execute(NativeActivityContext context)
    {
        _onCompleted ??= new(OnCompleted);
        context.ScheduleActivity(Expression, _onCompleted);
    }
    BpmNode GetNextNode(T value) => Cases.TryGetValue(value, out BpmNode result) ? result : Default;
    internal void OnCompleted(NativeActivityContext context, ActivityInstance completedInstance, T result) => TryExecute(GetNextNode(result), context, completedInstance);
}