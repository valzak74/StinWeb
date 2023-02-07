using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NonFactors.Mvc.Lookup;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;
using System.Reflection;
using System.Text;
using NonFactors.Mvc.Grid;

namespace StinWeb.Models.DataManager.Extensions
{
    internal static class LookupExpressionMetadata
    {
        public static String GetName(LambdaExpression expression)
        {
            Int32 length = 0;
            Int32 segmentCount = 0;
            Boolean lastIsModel = false;
            Int32 trailingMemberExpressions = 0;

            Expression? part = expression.Body;
            while (part != null)
            {
                switch (part.NodeType)
                {
                    case ExpressionType.Call:
                        lastIsModel = false;

                        MethodCallExpression methodExpression = (MethodCallExpression)part;

                        if (IsSingleArgumentIndexer(methodExpression))
                        {
                            part = methodExpression.Object;
                            trailingMemberExpressions = 0;
                            length += "[99]".Length;
                            segmentCount++;
                        }
                        else
                        {
                            part = null;
                        }

                        break;
                    case ExpressionType.ArrayIndex:
                        part = ((BinaryExpression)part).Left;
                        trailingMemberExpressions = 0;
                        length += "[99]".Length;
                        lastIsModel = false;
                        segmentCount++;

                        break;
                    case ExpressionType.MemberAccess:
                        MemberExpression memberExpressionPart = (MemberExpression)part;
                        String name = memberExpressionPart.Member.Name;

                        if (name.Contains("__"))
                        {
                            part = null;
                        }
                        else
                        {
                            lastIsModel = String.Equals("model", name, StringComparison.OrdinalIgnoreCase);
                            part = memberExpressionPart.Expression;
                            trailingMemberExpressions++;
                            length += name.Length + 1;
                            segmentCount++;
                        }

                        break;
                    case ExpressionType.Parameter:
                        lastIsModel = false;
                        part = null;

                        break;
                    default:
                        part = null;

                        break;
                }
            }

            if (lastIsModel)
            {
                segmentCount--;
                length -= ".model".Length;
                trailingMemberExpressions--;
            }

            if (trailingMemberExpressions > 0)
                length--;

            if (segmentCount == 0)
                return String.Empty;

            StringBuilder builder = new StringBuilder(length);
            part = expression.Body;
            while (part != null && segmentCount > 0)
            {
                segmentCount--;
                switch (part.NodeType)
                {
                    case ExpressionType.Call:
                        MethodCallExpression methodExpression = (MethodCallExpression)part;

                        InsertIndexerInvocationText(builder, methodExpression.Arguments.Single());

                        part = methodExpression.Object;

                        break;
                    case ExpressionType.ArrayIndex:
                        BinaryExpression binaryExpression = (BinaryExpression)part;

                        InsertIndexerInvocationText(builder, binaryExpression.Right);

                        part = binaryExpression.Left;

                        break;
                    case ExpressionType.MemberAccess:
                        MemberExpression memberExpression = (MemberExpression)part;
                        String name = memberExpression.Member.Name;

                        builder.Insert(0, name);

                        if (segmentCount > 0)
                            builder.Insert(0, '.');

                        part = memberExpression.Expression;

                        break;
                }
            }

            return builder.ToString();
        }
        public static ModelExplorer GetValue<TModel, TResult>(IHtmlHelper html, Expression<Func<TModel, TResult>> expression)
        {
            Type? containerType = null;
            String? propertyName = null;
            ModelMetadata? metadata = null;
            Boolean legalExpression = false;

            switch (expression.Body.NodeType)
            {
                case ExpressionType.ArrayIndex:
                    legalExpression = true;

                    break;
                case ExpressionType.Call:
                    legalExpression = IsSingleArgumentIndexer(expression.Body);

                    break;
                case ExpressionType.MemberAccess:
                    MemberExpression member = (MemberExpression)expression.Body;
                    propertyName = member.Member is PropertyInfo ? member.Member.Name : null;

                    if (String.Equals(propertyName, "Model", StringComparison.Ordinal) &&
                        member.Type == typeof(TModel) && member.Expression.NodeType == ExpressionType.Constant)
                        return FromModel(html.ViewData, html.MetadataProvider);

                    containerType = member.Expression?.Type;

                    legalExpression = true;

                    break;
                case ExpressionType.Parameter:
                    return FromModel(html.ViewData, html.MetadataProvider);
            }

            if (!legalExpression)
                throw new InvalidOperationException("Resources.TemplateHelpers_TemplateLimitations");

            Object? ModelAccessor(Object container)
            {
                try
                {
                    return expression.Compile()((TModel)container);
                }
                catch (NullReferenceException)
                {
                    return null;
                }
            }

            if (containerType != null && propertyName != null)
                metadata = html.MetadataProvider.GetMetadataForType(containerType).Properties[propertyName];

            if (metadata == null)
                metadata = html.MetadataProvider.GetMetadataForType(typeof(TResult));

            return html.ViewData.ModelExplorer.GetExplorerForExpression(metadata, ModelAccessor);
        }

        private static Boolean IsSingleArgumentIndexer(Expression expression)
        {
            if (expression is MethodCallExpression method && method.Arguments.Count == 1)
            {
                Type? type = method.Method.DeclaringType;

                if (type?.GetTypeInfo().GetCustomAttribute<DefaultMemberAttribute>(true) is DefaultMemberAttribute member)
                    foreach (PropertyInfo property in type.GetRuntimeProperties())
                        if (String.Equals(member.MemberName, property.Name, StringComparison.Ordinal) && property.GetMethod == method.Method)
                            return true;
            }

            return false;
        }
        private static void InsertIndexerInvocationText(StringBuilder builder, Expression index)
        {
            UnaryExpression converted = Expression.Convert(index, typeof(Object));
            ParameterExpression parameter = Expression.Parameter(typeof(Object), null);
            Expression<Func<Object?, Object>> lambda = Expression.Lambda<Func<Object?, Object>>(converted, parameter);

            builder.Insert(0, ']');
            builder.Insert(0, Convert.ToString(lambda.Compile()(null), CultureInfo.InvariantCulture));
            builder.Insert(0, '[');
        }
        private static ModelExplorer FromModel(ViewDataDictionary data, IModelMetadataProvider provider)
        {
            if (data.ModelMetadata.ModelType == typeof(Object))
                return provider.GetModelExplorerForType(typeof(String), data.Model ?? Convert.ToString(data.Model, CultureInfo.CurrentCulture));

            return data.ModelExplorer;
        }
    }
    public static class StinLookupExtensions
    {
        public static TagBuilder StinAutoComplete<TModel>(this IHtmlHelper<TModel> html,
            String name, ALookup model, Object? value = null, Object? htmlAttributes = null)
        {
            TagBuilder lookup = CreateLookup(model, name, htmlAttributes);
            lookup.AddCssClass("mvc-lookup-browseless");

            lookup.InnerHtml.AppendHtml(CreateLookupValues(html, model, name, value));
            lookup.InnerHtml.AppendHtml(CreateLookupControl(html, model, name));

            return lookup;
        }
        public static TagBuilder StinAutoCompleteFor<TModel, TProperty>(this IHtmlHelper<TModel> html,
            Expression<Func<TModel, TProperty>> expression, ALookup model, Object? htmlAttributes = null)
        {
            String name = LookupExpressionMetadata.GetName(expression);
            TagBuilder lookup = CreateLookup(model, name, htmlAttributes);
            lookup.AddCssClass("mvc-lookup-browseless");

            lookup.InnerHtml.AppendHtml(CreateLookupValues(html, model, expression));
            lookup.InnerHtml.AppendHtml(CreateLookupControl(html, model, name));

            return lookup;
        }

        public static TagBuilder StinLookup(this IHtmlHelper html,
            String name, ALookup model, Object? value = null, Object? htmlAttributes = null)
        {
            TagBuilder lookup = CreateLookup(model, name, htmlAttributes);

            lookup.InnerHtml.AppendHtml(CreateLookupValues(html, model, name, value));
            lookup.InnerHtml.AppendHtml(CreateLookupControl(html, model, name));
            lookup.InnerHtml.AppendHtml(CreateLookupBrowser(name));

            return lookup;
        }
        public static TagBuilder StinLookupFor<TModel, TProperty>(this IHtmlHelper<TModel> html,
            Expression<Func<TModel, TProperty>> expression, ALookup model, Object? htmlAttributes = null)
        {
            String name = LookupExpressionMetadata.GetName(expression);
            TagBuilder lookup = CreateLookup(model, name, htmlAttributes);

            lookup.InnerHtml.AppendHtml(CreateLookupValues(html, model, expression));
            lookup.InnerHtml.AppendHtml(CreateLookupControl(html, model, name));
            lookup.InnerHtml.AppendHtml(CreateLookupBrowser(name));

            return lookup;
        }

        private static TagBuilder CreateLookup(ALookup lookup, String name, Object? htmlAttributes)
        {
            IDictionary<String, Object?> attributes = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            attributes["data-filters"] = String.Join(",", lookup.AdditionalFilters);
            attributes["data-search"] = lookup.Filter.Search;
            attributes["data-offset"] = lookup.Filter.Offset;
            attributes["data-order"] = lookup.Filter.Order;
            attributes["data-readonly"] = lookup.ReadOnly;
            attributes["data-sort"] = lookup.Filter.Sort;
            attributes["data-rows"] = lookup.Filter.Rows;
            attributes["data-dialog"] = lookup.Dialog;
            attributes["data-title"] = lookup.Title;
            attributes["data-multi"] = lookup.Multi;
            attributes["data-url"] = lookup.Url;
            attributes["data-for"] = name;

            object objAddHandler = null;
            bool needAddHandler = false;
            attributes.TryGetValue("addHandler", out objAddHandler);

            if (needAddHandler != null)
            {
                if (!Boolean.TryParse((string)objAddHandler, out needAddHandler))
                    needAddHandler = false;
            }
            if (needAddHandler)
            {
                attributes["data-add-handler"] = true;
                attributes.Remove("addHandler");
            }

            TagBuilder group = new TagBuilder("div");
            group.MergeAttributes(attributes);
            group.AddCssClass("mvc-lookup");
            group.AddCssClass("mvc-lookup-sm");

            if (lookup.ReadOnly)
                group.AddCssClass("mvc-lookup-readonly");

            return group;
        }

        private static string GenerateHtmlId(string name)
        {
            string result = name.Replace(".", "").ToLower();
            if (result.EndsWith("id"))
                result = result.Substring(0, result.LastIndexOf("id"));
            return result + "Id";
        }

        private static IHtmlContent CreateLookupValues<TModel, TProperty>(IHtmlHelper<TModel> html, ALookup lookup, Expression<Func<TModel, TProperty>> expression)
        {
            Object value = LookupExpressionMetadata.GetValue(html, expression).Model;
            String name = LookupExpressionMetadata.GetName(expression);

            if (lookup.Multi)
                return CreateLookupValues(html, lookup, name, value);

            IDictionary<String, Object> attributes = new Dictionary<String, Object>
            {
                ["class"] = "mvc-lookup-value",
                ["autocomplete"] = "off",
                ["id"] = GenerateHtmlId(name)
            };

            TagBuilder container = new TagBuilder("div");
            container.AddCssClass("mvc-lookup-values");
            container.Attributes["data-for"] = name;

            container.InnerHtml.AppendHtml(html.HiddenFor(expression, attributes));

            return container;
        }
        private static IHtmlContent CreateLookupValues(IHtmlHelper html, ALookup lookup, String name, Object? value)
        {
            IDictionary<String, Object> attributes = new Dictionary<String, Object>
            {
                ["class"] = "mvc-lookup-value",
                ["autocomplete"] = "off",
                ["id"] = GenerateHtmlId(name)
            };

            TagBuilder container = new TagBuilder("div");
            container.AddCssClass("mvc-lookup-values");
            container.Attributes["data-for"] = name;

            if (lookup.Multi)
            {
                IEnumerable<Object>? values = (value as IEnumerable)?.Cast<Object>();

                if (values == null)
                    return container;

                IHtmlContentBuilder inputs = new HtmlContentBuilder();

                foreach (Object val in values)
                {
                    TagBuilder input = new TagBuilder("input");
                    input.Attributes["value"] = html.FormatValue(val, null);
                    input.TagRenderMode = TagRenderMode.SelfClosing;
                    input.Attributes["type"] = "hidden";
                    input.MergeAttributes(attributes);
                    input.Attributes["name"] = name;

                    inputs.AppendHtml(input);
                }

                container.InnerHtml.AppendHtml(inputs);
            }
            else
            {
                container.InnerHtml.AppendHtml(html.Hidden(name, value, attributes));
            }

            return container;
        }
        private static IHtmlContent CreateLookupControl(IHtmlHelper html, ALookup lookup, String name)
        {
            TagBuilder search = new TagBuilder("input") { TagRenderMode = TagRenderMode.SelfClosing };
            TagBuilder control = new TagBuilder("div");
            TagBuilder loader = new TagBuilder("div");
            TagBuilder error = new TagBuilder("div");

            Dictionary<String, Object> attributes = new Dictionary<String, Object>
            {
                ["class"] = "mvc-lookup-input",
                ["autocomplete"] = "off"
            };

            if (lookup.Placeholder != null)
                attributes["placeholder"] = lookup.Placeholder;

            if (lookup.Name != null)
                attributes["id"] = GenerateHtmlId(lookup.Name); 
                //attributes["id"] = html.Id(lookup.Name);

                if (lookup.Name != null)
                attributes["name"] = lookup.Name;

            if (lookup.ReadOnly)
                attributes["readonly"] = "readonly";

            loader.AddCssClass("mvc-lookup-control-loader");
            error.AddCssClass("mvc-lookup-control-error");
            control.AddCssClass("mvc-lookup-control");
            control.Attributes["data-for"] = name;
            search.MergeAttributes(attributes);
            error.InnerHtml.Append("!");

            control.InnerHtml.AppendHtml(lookup.Name == null ? search : html.TextBox(lookup.Name, null, attributes));
            control.InnerHtml.AppendHtml(loader);
            control.InnerHtml.AppendHtml(error);

            return control;
        }
        private static IHtmlContent CreateLookupBrowser(String name)
        {
            TagBuilder browser = new TagBuilder("button");
            browser.AddCssClass("mvc-lookup-browser");
            browser.Attributes["data-for"] = name;
            browser.Attributes["type"] = "button";

            TagBuilder icon = new TagBuilder("span");
            icon.AddCssClass("mvc-lookup-icon");

            browser.InnerHtml.AppendHtml(icon);

            return browser;
        }
    }
    public static class HtmlButtonExtension
    {

        public static HtmlString Button(this IHtmlHelper helper,
                                           string innerHtml,
                                           object htmlAttributes)
        {
            return Button(helper, innerHtml,
                          HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes)
            );
        }

        public static HtmlString Button(this IHtmlHelper helper,
                                           string innerHtml,
                                           IDictionary<string, object> htmlAttributes)
        {
            var builder = new TagBuilder("button");
            builder.InnerHtml.AppendHtml(innerHtml);
            builder.MergeAttributes(htmlAttributes);
            using var writer = new System.IO.StringWriter();
            builder.WriteTo(writer, HtmlEncoder.Default);

            return new HtmlString(writer.ToString());
        }
    }
    public static class HtmlHelpers
    {
        public static IDictionary<string,object> ConditionalDisable(bool disabled, object htmlAttributes = null)
        {
            var dictionary = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);

            if (disabled)
                dictionary.Add("disabled", "disabled");

            return dictionary;
        }
        public static IDictionary<string, object> ConditionalReadOnly(bool readOnly, object htmlAttributes = null)
        {
            var dictionary = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);

            if (readOnly)
            {
                dictionary.Add("readonly", "readonly");
                if (dictionary.ContainsKey("style"))
                {
                    string styleValue = (string)dictionary["style"];
                    if (!styleValue.EndsWith(";"))
                        styleValue = styleValue + ";";
                    dictionary["style"] = styleValue + "pointer-events: none;";
                }
                else
                    dictionary.Add("style", "pointer-events: none;");
            }

            return dictionary;
        }
        public static bool NeedDateFormat(object htmlAttributes = null)
        {
            var dictionary = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            bool result = false;
            if (dictionary.ContainsKey("type"))
                result = dictionary["type"].ToString() == "date";
            return result;
        }
        public static HtmlString Label(this IHtmlHelper helper, string label, object htmlAttributes)
        {
            var builder = new TagBuilder("label");
            builder.MergeAttributes(HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
            builder.InnerHtml.Append(label);
            using var writer = new System.IO.StringWriter();
            builder.WriteTo(writer, HtmlEncoder.Default);

            return new HtmlString(writer.ToString());
        }
        public static HtmlString LabelWithValue(this IHtmlHelper helper, string label, string value, object htmlAttributes = null, object htmlValueAttributes = null)
        {
            var mainDiv = new TagBuilder("div");
            var spanTag = new TagBuilder("span");
            spanTag.InnerHtml.Append(label);
            if (htmlAttributes != null)
            {
                var dictionary = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
                if (dictionary.ContainsKey("class"))
                    spanTag.AddCssClass((string)dictionary["class"]);
                spanTag.MergeAttributes(HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
            }
            var valueTag = new TagBuilder("span");
            valueTag.InnerHtml.Append(value);
            if (htmlValueAttributes != null)
            {
                var dictionary = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlValueAttributes);
                if (dictionary.ContainsKey("class"))
                    valueTag.AddCssClass((string)dictionary["class"]);
                valueTag.MergeAttributes(HtmlHelper.AnonymousObjectToHtmlAttributes(htmlValueAttributes));
            }
            else if (htmlAttributes != null)
            {
                var dictionary = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
                if (dictionary.ContainsKey("class"))
                    valueTag.AddCssClass((string)dictionary["class"]);
                valueTag.MergeAttributes(HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
            }
            mainDiv.InnerHtml.AppendHtml(spanTag);
            mainDiv.InnerHtml.AppendHtml(valueTag);
            using var writer = new System.IO.StringWriter();
            mainDiv.WriteTo(writer, HtmlEncoder.Default);

            return new HtmlString(writer.ToString());
        }
        public static HtmlString InputGroup(this IHtmlHelper html, string label, string valueType, string valueName, object valueText, object htmlAttributes, bool readOnly = false)
        {
            TagBuilder mainDiv = new TagBuilder("div");
            mainDiv.AddCssClass("input-group input-group-sm mb-2");
            TagBuilder labelDiv = new TagBuilder("div");
            labelDiv.AddCssClass("input-group-prepend");
            TagBuilder spanLabel = new TagBuilder("span");
            spanLabel.AddCssClass("input-group-text");
            spanLabel.InnerHtml.Append(label);
            labelDiv.InnerHtml.AppendHtml(spanLabel);
            mainDiv.InnerHtml.AppendHtml(labelDiv);
            switch (valueType)
            {
                case "input":
                    mainDiv.InnerHtml.AppendHtml(html.TextBox(valueName, (string)valueText, ConditionalReadOnly(readOnly, htmlAttributes)));
                    break;
                case "select":
                    mainDiv.InnerHtml.AppendHtml(html.DropDownList(valueName, (SelectList)valueText, ConditionalReadOnly(readOnly, htmlAttributes)));
                    break;
            }

            using var writer = new System.IO.StringWriter();
            mainDiv.WriteTo(writer, HtmlEncoder.Default);

            return new HtmlString(writer.ToString());
        }
        public static HtmlString InputGroup(this IHtmlHelper html, string inputGroupSuffix, string label, string append, string valueType, string valueName, object valueText, object htmlAttributes, bool readOnly = false)
        {
            TagBuilder mainDiv = new TagBuilder("div");
            mainDiv.AddCssClass("input-group input-group-sm " + inputGroupSuffix);
            TagBuilder labelDiv = new TagBuilder("div");
            labelDiv.AddCssClass("input-group-prepend");
            TagBuilder spanLabel = new TagBuilder("span");
            spanLabel.AddCssClass("input-group-text");
            spanLabel.InnerHtml.Append(label);
            labelDiv.InnerHtml.AppendHtml(spanLabel);
            mainDiv.InnerHtml.AppendHtml(labelDiv);
            switch (valueType)
            {
                case "input":
                    mainDiv.InnerHtml.AppendHtml(html.TextBox(valueName, (string)valueText, ConditionalReadOnly(readOnly, htmlAttributes)));
                    break;
                case "select":
                    mainDiv.InnerHtml.AppendHtml(html.DropDownList(valueName, (SelectList)valueText, ConditionalReadOnly(readOnly, htmlAttributes)));
                    break;
                case "lookup":
                    (valueText as ALookup).ReadOnly = readOnly;
                    mainDiv.InnerHtml.AppendHtml(html.StinLookup(valueName, (ALookup)valueText, null, htmlAttributes));
                    break;
            }
            if (!string.IsNullOrEmpty(append))
            {
                TagBuilder appendDiv = new TagBuilder("div");
                appendDiv.AddCssClass("input-group-append");
                appendDiv.InnerHtml.AppendHtml(append);
                mainDiv.InnerHtml.AppendHtml(appendDiv);
            }

            using var writer = new System.IO.StringWriter();
            mainDiv.WriteTo(writer, HtmlEncoder.Default);

            return new HtmlString(writer.ToString());
        }
        public static HtmlString InputGroupFor<TModel, TProperty>(
                this IHtmlHelper<TModel> htmlHelper,
                Expression<Func<TModel, TProperty>> expression,
                string inputGroupSuffix,
                string label,
                string valueType,
                object valueData,
                object htmlAttributes,
                bool readOnly = false
            )
        {
            TagBuilder mainDiv = new TagBuilder("div");
            mainDiv.AddCssClass("input-group input-group-sm " + inputGroupSuffix);
            TagBuilder labelDiv = new TagBuilder("div");
            labelDiv.AddCssClass("input-group-prepend");
            TagBuilder spanLabel = new TagBuilder("span");
            spanLabel.AddCssClass("input-group-text");
            spanLabel.InnerHtml.Append(label);
            labelDiv.InnerHtml.AppendHtml(spanLabel);
            mainDiv.InnerHtml.AppendHtml(labelDiv);
            switch (valueType)
            {
                case "input":
                    if (NeedDateFormat(htmlAttributes))
                        mainDiv.InnerHtml.AppendHtml(htmlHelper.TextBoxFor(expression, "{0:yyyy-MM-dd}", ConditionalReadOnly(readOnly, htmlAttributes)));
                    else
                        mainDiv.InnerHtml.AppendHtml(htmlHelper.TextBoxFor(expression, ConditionalReadOnly(readOnly, htmlAttributes)));
                    break;
                case "textarea":
                    mainDiv.InnerHtml.AppendHtml(htmlHelper.TextAreaFor(expression, ConditionalReadOnly(readOnly, htmlAttributes)));
                    break;
                case "select":
                    mainDiv.InnerHtml.AppendHtml(htmlHelper.DropDownListFor(expression, (SelectList)valueData, ConditionalReadOnly(readOnly, htmlAttributes)));
                    break;
                case "lookup":
                    (valueData as ALookup).ReadOnly = readOnly;
                    mainDiv.InnerHtml.AppendHtml(htmlHelper.StinLookupFor(expression, (ALookup)valueData, htmlAttributes));
                    break;
            }

            using var writer = new System.IO.StringWriter();
            mainDiv.WriteTo(writer, HtmlEncoder.Default);

            return new HtmlString(writer.ToString());
        }
        public static HtmlString InputGroupFor<TModel, TProperty>(
                this IHtmlHelper<TModel> htmlHelper,
                Expression<Func<TModel, TProperty>> expression,
                object inputGroupHtmlAttibutes,
                string label,
                string valueType,
                object valueData,
                object htmlAttributes,
                bool readOnly = false
            )
        {
            TagBuilder mainDiv = new TagBuilder("div");
            mainDiv.AddCssClass("input-group input-group-sm");
            if (inputGroupHtmlAttibutes != null)
            {
                var dictionary = HtmlHelper.AnonymousObjectToHtmlAttributes(inputGroupHtmlAttibutes);
                if (dictionary.ContainsKey("class"))
                    mainDiv.AddCssClass((string)dictionary["class"]);
                mainDiv.MergeAttributes(HtmlHelper.AnonymousObjectToHtmlAttributes(inputGroupHtmlAttibutes));
            }
            TagBuilder labelDiv = new TagBuilder("div");
            labelDiv.AddCssClass("input-group-prepend");
            TagBuilder spanLabel = new TagBuilder("span");
            spanLabel.AddCssClass("input-group-text");
            spanLabel.InnerHtml.Append(label);
            labelDiv.InnerHtml.AppendHtml(spanLabel);
            mainDiv.InnerHtml.AppendHtml(labelDiv);
            switch (valueType)
            {
                case "input":
                    if (NeedDateFormat(htmlAttributes))
                        mainDiv.InnerHtml.AppendHtml(htmlHelper.TextBoxFor(expression, "{0:yyyy-MM-dd}", ConditionalReadOnly(readOnly, htmlAttributes)));
                    else
                        mainDiv.InnerHtml.AppendHtml(htmlHelper.TextBoxFor(expression, ConditionalReadOnly(readOnly, htmlAttributes)));
                    break;
                case "textarea":
                    mainDiv.InnerHtml.AppendHtml(htmlHelper.TextAreaFor(expression, ConditionalReadOnly(readOnly, htmlAttributes)));
                    break;
                case "select":
                    mainDiv.InnerHtml.AppendHtml(htmlHelper.DropDownListFor(expression, (SelectList)valueData, ConditionalReadOnly(readOnly, htmlAttributes)));
                    break;
                case "lookup":
                    (valueData as ALookup).ReadOnly = readOnly;
                    mainDiv.InnerHtml.AppendHtml(htmlHelper.StinLookupFor(expression, (ALookup)valueData, htmlAttributes));
                    break;
            }

            using var writer = new System.IO.StringWriter();
            mainDiv.WriteTo(writer, HtmlEncoder.Default);

            return new HtmlString(writer.ToString());
        }
        public static IHtmlGrid<T> Empty<T>(this IHtmlGrid<T> html, string text, int v)
        {
            html.Grid.EmptyText = text;
            return html;
        }
    }
}
