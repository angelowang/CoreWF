﻿using CustomTestObjects;
using Microsoft.CSharp.Activities;
using Microsoft.VisualBasic.Activities;
using Newtonsoft.Json;
using Shouldly;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Activities.Validation;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace TestCases.Workflows;

public class ExpressionTests
{
    private readonly ValidationSettings _skipCompilation = new() { SkipExpressionCompilation = true };
    private readonly ValidationSettings _forceCache = new() { ForceExpressionCache = true };
    private readonly ValidationSettings _useValidator = new() { ForceExpressionCache = false };

    public static IEnumerable<object[]> ValidVbExpressions
    {
        get
        {
            yield return new object[] { @$"String.Concat(""alpha "", ""beta "", 1)" };
            yield return new object[] { @$"String.Concat(""alpha "", v, ""beta "", 1)" };
            yield return new object[] { @$"String.Concat(""alpha "", l(0), ""beta "", 1)" };
            yield return new object[] { @$"String.Concat(""alpha "", d(""gamma""), ""beta "", 1)" };
            yield return new object[] { @"[Enum].Parse(GetType(ArgumentDirection), ""In"").ToString()" };
            yield return new object[] { "GetType(Activities.VisualBasicSettings).Name" };
        }
    }

    public static IEnumerable<object[]> ValidCsExpressions
    {
        get
        {
            yield return new object[] { $@"string.Join(',', ""alpha"", 1, ""beta"")" };
            yield return new object[] { $@"string.Join(',', ""alpha"", v, 1, ""beta"")" };
            yield return new object[] { $@"string.Join(',', ""alpha"", l[0], 1, ""beta"")" };
            yield return new object[] { $@"string.Join(',', ""alpha"", d[""gamma""], 1, ""beta"")" };
        }
    }

    static ExpressionTests()
    {
        // There's no programmatic way (that I know of) to add assembly references when creating workflows like in these tests.
        // Adding the custom assembly directly to the expression validator to simulate XAML reference.
        // The null is for testing purposes.
        VbExpressionValidator.Instance = new VbExpressionValidator(new() { typeof(ClassWithCollectionProperties).Assembly, null });
    }

    [Theory]
    [MemberData(nameof(ValidVbExpressions))]
    public void Vb_Valid(string expr)
    {
        VisualBasicValue<string> vbv = new(expr);
        WriteLine writeLine = new();
        writeLine.Text = new InArgument<string>(vbv);
        Sequence workflow = new();
        workflow.Activities.Add(writeLine);
        workflow.Variables.Add(new Variable<string>("v", "I'm a variable"));
        workflow.Variables.Add(new Variable<List<string>>("l"));
        workflow.Variables.Add(new Variable<Dictionary<string, List<string>>>("d"));

        ValidationResults validationResults = ActivityValidationServices.Validate(workflow, _useValidator);
        validationResults.Errors.Count.ShouldBe(0, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
    }

    [Theory]
    [MemberData(nameof(ValidCsExpressions))]
    public void Cs_Valid(string expr)
    {
        CSharpValue<string> csv = new(expr);
        WriteLine writeLine = new();
        writeLine.Text = new InArgument<string>(csv);
        Sequence workflow = new();
        workflow.Activities.Add(writeLine);
        workflow.Variables.Add(new Variable<string>("v", "I'm a variable"));
        workflow.Variables.Add(new Variable<List<string>>("l"));
        workflow.Variables.Add(new Variable<Dictionary<string, List<string>>>("d"));

        ValidationResults validationResults = ActivityValidationServices.Validate(workflow, _useValidator);
        validationResults.Errors.Count.ShouldBe(0, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
    }

    [Fact]
    public void Vb_InvalidExpression_Basic()
    {
        VisualBasicValue<string> vbv = new(@$"String.Concat(""alpha "", b, ""beta "", 1)");
        WriteLine writeLine = new();
        writeLine.Text = new InArgument<string>(vbv);
        Sequence workflow = new();
        workflow.Activities.Add(writeLine);

        ValidationResults validationResults = ActivityValidationServices.Validate(workflow, _useValidator);
        validationResults.Errors.Count.ShouldBe(1, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
    }

    [Fact]
    public void Cs_InvalidExpression_Basic()
    {
        CSharpValue<string> csv = new(@$"string.Concat(""alpha "", b, ""beta "", 1)");
        WriteLine writeLine = new();
        writeLine.Text = new InArgument<string>(csv);
        Sequence workflow = new();
        workflow.Activities.Add(writeLine);

        ValidationResults validationResults = ActivityValidationServices.Validate(workflow, _useValidator);
        validationResults.Errors.Count.ShouldBe(1, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
        validationResults.Errors[0].Message.ShouldContain("The name 'b' does not exist in the current context");
    }

    [Fact]
    public void Vb_InvalidExpression_SkipCompilation()
    {
        VisualBasicValue<string> vbv = new(@$"String.Concat(""alpha "", b, ""beta "", 1)");
        WriteLine writeLine = new();
        writeLine.Text = new InArgument<string>(vbv);
        Sequence workflow = new();
        workflow.Activities.Add(writeLine);

        ValidationResults validationResults = ActivityValidationServices.Validate(workflow, _skipCompilation);
        validationResults.Errors.Count.ShouldBe(0, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
    }

    [Fact]
    public void Cs_InvalidExpression_SkipCompilation()
    {
        CSharpValue<string> csv = new(@$"string.Concat(""alpha "", b, ""beta "", 1)");
        WriteLine writeLine = new();
        writeLine.Text = new InArgument<string>(csv);
        Sequence workflow = new();
        workflow.Activities.Add(writeLine);

        ValidationResults validationResults = ActivityValidationServices.Validate(workflow, _skipCompilation);
        validationResults.Errors.Count.ShouldBe(0, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
    }

    [Fact]
    public void Vb_CompareLambdas()
    {
        VisualBasicValue<string> vbv = new(@$"String.Concat(""alpha "", b?.Substring(0, 10), ""beta "", 1)");
        WriteLine writeLine = new();
        writeLine.Text = new InArgument<string>(vbv);
        Sequence workflow = new();
        workflow.Activities.Add(writeLine);
        workflow.Variables.Add(new Variable<string>("b", "I'm a variable"));

        ValidationResults validationResults = ActivityValidationServices.Validate(workflow, _forceCache);
        validationResults.Errors.Count.ShouldBe(1, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
        validationResults.Errors[0].Message.ShouldContain("A null propagating operator cannot be converted into an expression tree.");

        validationResults = ActivityValidationServices.Validate(workflow, _useValidator);
        validationResults.Errors.Count.ShouldBe(1, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
        validationResults.Errors[0].Message.ShouldContain("A null propagating operator cannot be converted into an expression tree.");
    }

    [Fact]
    public void Vb_LambdaExtension()
    {
        VisualBasicValue<string> vbv = new("list.First()");
        WriteLine writeLine = new();
        writeLine.Text = new InArgument<string>(vbv);
        Sequence workflow = new();
        workflow.Activities.Add(writeLine);
        workflow.Variables.Add(new Variable<List<string>>("list"));

        ValidationResults validationResults = ActivityValidationServices.Validate(workflow, _useValidator);
        validationResults.Errors.Count.ShouldBe(0, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
    }

    [Fact]
    public void Vb_Dictionary()
    {
        VisualBasicValue<string> vbv = new("something.FooDictionary(\"key\").ToString()");
        WriteLine writeLine = new();
        writeLine.Text = new InArgument<string>(vbv);
        Sequence workflow = new();
        workflow.Activities.Add(writeLine);
        workflow.Variables.Add(new Variable<ClassWithCollectionProperties>("something"));

        ValidationResults validationResults = ActivityValidationServices.Validate(workflow, _useValidator);
        validationResults.Errors.Count.ShouldBe(0, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
    }

    [Fact]
    public void Vb_IntOverflow()
    {
        VisualBasicValue<int> vbv = new("2147483648");
        Sequence workflow = new();
        workflow.Variables.Add(new Variable<int>("someint"));
        Assign assign = new() { To = new OutArgument<int>(workflow.Variables[0]), Value = new InArgument<int>(vbv) };
        workflow.Activities.Add(assign);

        ValidationResults validationResults = ActivityValidationServices.Validate(workflow, _useValidator);
        validationResults.Errors.Count.ShouldBe(1, string.Join("\n", validationResults.Errors.Select(e => e.Message)));
        validationResults.Errors[0].Message.ShouldContain("error BC30439: Constant expression not representable in type 'Integer'");
    }

    [Fact]
    public void VBValidator_StrictOn()
    {
        var activity = new WriteLine { Text = new InArgument<string>(new VisualBasicValue<string>("(\"3\" + 3).ToString")) };
        var result = ActivityValidationServices.Validate(activity, _useValidator);

        result.Errors.Count.ShouldBe(1);
        result.Errors.First().Message.ShouldContain("error BC30512: Option Strict On");
    }

    [Fact]
    public void VBValue_ShowsValidationError()
    {
        var activity = new WriteLine { Text = new InArgument<string>(new VisualBasicValue<string>("var1")) };
        var result = ActivityValidationServices.Validate(activity, _useValidator);

        result.Errors.Count.ShouldBe(1);
        result.Errors.First().Message.ShouldContain("error BC30451: 'var1' is not declared");
    }

    [Fact]
    public void VBReference_ShowsValidationError()
    {
        var activity = new Assign
        {
            To = new OutArgument<string>(new VisualBasicReference<string>("var1")),
            Value = new InArgument<string>("\"abc\"")
        };
        var result = ActivityValidationServices.Validate(activity, _useValidator);

        result.Errors.Count.ShouldBe(1);
        result.Errors.First().Message.ShouldContain("error BC30451: 'var1' is not declared");
    }

    [Fact]
    public void VbValue_IdentifiersComparerOrdinalIgnoreCase()
    {
        var root = new Sequence();
        root.Variables.Add(new Variable<List<object>>("count"));
        root.Activities.Add(new WriteLine { Text = new InArgument<string>(new VisualBasicValue<string>("count.Count.ToString")) });

        var result = ActivityValidationServices.Validate(root, _useValidator);

        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void CSValue_ShowsValidationError()
    {
        var activity = new WriteLine { Text = new InArgument<string>(new CSharpValue<string>("var1")) };
        var result = ActivityValidationServices.Validate(activity, _useValidator);

        result.Errors.Count.ShouldBe(1);
        result.Errors.First().Message.ShouldContain("The name 'var1' does not exist in the current context");
    }

    [Fact]
    public void CSReference_ShowsValidationError()
    {
        var activity = new Assign
        {
            To = new OutArgument<string>(new CSharpReference<string>("var1")),
            Value = new InArgument<string>("\"abc\"")
        };
        var result = ActivityValidationServices.Validate(activity, _useValidator);

        result.Errors.Count.ShouldBe(1);
        result.Errors.First().Message.ShouldContain("The name 'var1' does not exist in the current context");
    }

    [Fact]
    public void VBValue_Validate_AddsRequiredAssembliesPerExpressionValidated()
    {
        var simpleActivity = new WriteLine() { Text = new InArgument<string>(new VisualBasicValue<string>("1.ToString")) };

        var requiresNewtonsoftActivity = new WriteLine { Text = new InArgument<string>(new VisualBasicValue<string>("GetType(JsonConvert).ToString")) };
        var dy = new DynamicActivity() { Implementation = () => requiresNewtonsoftActivity };
        TextExpression.SetReferencesForImplementation(dy, new[] { new AssemblyReference("Newtonsoft.Json") });
        TextExpression.SetNamespacesForImplementation(dy, new[] { "Newtonsoft.Json" });

        // first validate an activity without needing Newtonsoft.Json assembly
        ActivityValidationServices.Validate(simpleActivity, _useValidator);

        // then validate with a new assembly required (Newtonsoft.Json)
        var result = ActivityValidationServices.Validate(dy, _useValidator);
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void CSValue_Validate_AddsRequiredAssembliesPerExpressionValidated()
    {
        
        var simpleActivity = new WriteLine() { Text = new InArgument<string>(new CSharpValue<string>("1.ToString()"))};

        var requiresNewtonsoftActivity = new WriteLine { Text = new InArgument<string>(new CSharpValue<string>("typeof(JsonConvert).Name")) };
        var dy = new DynamicActivity() { Implementation = () => requiresNewtonsoftActivity };
        TextExpression.SetReferencesForImplementation(dy, new[] { new AssemblyReference("Newtonsoft.Json") });
        TextExpression.SetNamespacesForImplementation(dy, new[] { "Newtonsoft.Json" });

        // first validate an activity without needing Newtonsoft.Json assembly
        ActivityValidationServices.Validate(simpleActivity, _useValidator);

        // then validate with a new assembly required (Newtonsoft.Json)
        var result = ActivityValidationServices.Validate(dy, _useValidator);
        result.Errors.ShouldBeEmpty();
    }
}