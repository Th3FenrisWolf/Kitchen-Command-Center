using System.Linq.Expressions;
using System.Reflection;
using CMS.Core;
using CMS.Websites;
using KCC.ResourceStrings;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Extensions;

public static class UrlHelperExtensions
{
    public static string ActionFor<TController>(
        this IUrlHelper urlHelper,
        Expression<Func<TController, object>> actionExpression
    ) where TController : ControllerBase
    {
        var methodCall = GetMethodCall(actionExpression.Body);

        var action = GetActionName(methodCall.Method);
        var controller = GetControllerName(typeof(TController));
        var routeValues = GetRouteValues(methodCall);

        return urlHelper.Action(action, controller, routeValues)
            ?? throw new InvalidOperationException($"Could not generate URL for '{controller}.{action}'.");
    }

    public static string LocalizedActionFor<TController>(
        this IUrlHelper urlHelper,
        Expression<Func<TController, object>> actionExpression
    ) where TController : ControllerBase
    {
        var methodCall = GetMethodCall(actionExpression.Body);

        var action = GetActionName(methodCall.Method);
        var controller = GetControllerName(typeof(TController));
        var routeValues = GetRouteValues(methodCall);

        return urlHelper.LocalizedAction(action, controller, routeValues);
    }

    public static string ActionFor<TController>(
        this IUrlHelper urlHelper,
        Expression<Action<TController>> actionExpression
    ) where TController : ControllerBase
    {
        var methodCall = GetMethodCall(actionExpression.Body);

        var action = GetActionName(methodCall.Method);
        var controller = GetControllerName(typeof(TController));
        var routeValues = GetRouteValues(methodCall);

        return urlHelper.Action(action, controller, routeValues)
            ?? throw new InvalidOperationException($"Could not generate URL for '{controller}.{action}'.");
    }

    public static string HomePage(this IUrlHelper _)
    {
        var contentRetriever = Service.Resolve<IContentRetriever>();

        var page = contentRetriever.RetrievePages<HomePage>(
            new(),
            query => query.TopN(1),
            new($"{nameof(UrlHelperExtensions)}|{nameof(HomePage)}")
        ).GetAwaiter().GetResult().FirstOrDefault();

        return page.GetUrl().RelativePath;
    }

    public static string LocalizedAction(this IUrlHelper urlHelper, string action, string controller)
        => urlHelper.LocalizedAction(action, controller, null);

    public static string LocalizedAction(
        this IUrlHelper urlHelper,
        string action,
        string controller,
        object routeValues
    )
    {
        var lang = Service.Resolve<IPreferredLanguageRetriever>().Get();

        var path = urlHelper.Action(action, controller, routeValues);

        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        // LocalizedAction is intended for page routes; API routes should never be language-prefixed.
        if (IsApiPath(path))
        {
            return path.ToLowerInvariant();
        }

        if (string.IsNullOrEmpty(lang) || lang == DefaultLanguageRetriever.GetName())
        {
            return path.ToLowerInvariant();
        }

        return $"/{lang}{path}".ToLowerInvariant();
    }

    private static bool IsApiPath(string path)
    {
        var normalizedPath = path.StartsWith("~/", StringComparison.Ordinal)
            ? path[1..]
            : path;

        return normalizedPath.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
            || normalizedPath.Equals("api", StringComparison.OrdinalIgnoreCase);
    }

    private static MethodCallExpression GetMethodCall(Expression expression)
    {
        if (expression is MethodCallExpression methodCall)
        {
            return methodCall;
        }

        if (expression is UnaryExpression { Operand: MethodCallExpression unaryMethodCall })
        {
            return unaryMethodCall;
        }

        throw new ArgumentException("Expression must be a method call.", nameof(expression));
    }

    private static string GetActionName(MethodInfo method)
    {
        var actionNameAttribute = method.GetCustomAttribute<ActionNameAttribute>();
        if (!string.IsNullOrWhiteSpace(actionNameAttribute?.Name))
        {
            return actionNameAttribute.Name;
        }

        var methodName = method.Name;
        return methodName.EndsWith("Async", StringComparison.Ordinal)
            ? methodName[..^"Async".Length]
            : methodName;
    }

    private static string GetControllerName(Type controllerType)
    {
        const string suffix = "Controller";
        return controllerType.Name.EndsWith(suffix, StringComparison.Ordinal)
            ? controllerType.Name[..^suffix.Length]
            : controllerType.Name;
    }

    private static RouteValueDictionary GetRouteValues(MethodCallExpression methodCall)
    {
        if (methodCall.Arguments.Count == 0)
        {
            return null;
        }

        var parameters = methodCall.Method.GetParameters();
        var routeValues = new RouteValueDictionary();

        for (var i = 0; i < methodCall.Arguments.Count; i++)
        {
            var argumentValue = EvaluateExpression(methodCall.Arguments[i]);
            routeValues[parameters[i].Name!] = argumentValue;
        }

        return routeValues;
    }

    private static object EvaluateExpression(Expression expression)
    {
        if (expression is ConstantExpression constant)
        {
            return constant.Value;
        }

        var boxed = Expression.Convert(expression, typeof(object));
        var lambda = Expression.Lambda<Func<object>>(boxed);
        return lambda.Compile().Invoke();
    }
}
