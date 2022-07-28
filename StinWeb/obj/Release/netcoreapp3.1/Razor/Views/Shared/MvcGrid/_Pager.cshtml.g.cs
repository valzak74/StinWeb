#pragma checksum "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "61137cc95270adb7dd63e526c7c8e5bfca41056c"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Views_Shared_MvcGrid__Pager), @"mvc.1.0.view", @"/Views/Shared/MvcGrid/_Pager.cshtml")]
namespace AspNetCore
{
    #line hidden
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
#nullable restore
#line 1 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\_ViewImports.cshtml"
using StinWeb;

#line default
#line hidden
#nullable disable
#nullable restore
#line 3 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\_ViewImports.cshtml"
using NonFactors.Mvc.Lookup;

#line default
#line hidden
#nullable disable
#nullable restore
#line 4 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\_ViewImports.cshtml"
using StinWeb.Models;

#line default
#line hidden
#nullable disable
#nullable restore
#line 1 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
using NonFactors.Mvc.Grid;

#line default
#line hidden
#nullable disable
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"61137cc95270adb7dd63e526c7c8e5bfca41056c", @"/Views/Shared/MvcGrid/_Pager.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"303b915066d1efa5c78241ff8833664880cca93b", @"/Views/_ViewImports.cshtml")]
    public class Views_Shared_MvcGrid__Pager : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<IGridPager>
    {
        #line hidden
        #pragma warning disable 0649
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext;
        #pragma warning restore 0649
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner();
        #pragma warning disable 0169
        private string __tagHelperStringValueBuffer;
        #pragma warning restore 0169
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __backed__tagHelperScopeManager = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager
        {
            get
            {
                if (__backed__tagHelperScopeManager == null)
                {
                    __backed__tagHelperScopeManager = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
                }
                return __backed__tagHelperScopeManager;
            }
        }
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.OptionTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
#nullable restore
#line 3 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
  
    Int32 totalPages = Model.TotalPages;
    Int32 currentPage = Model.CurrentPage;
    Int32 firstDisplayPage = Model.FirstDisplayPage;

#line default
#line hidden
#nullable disable
            WriteLiteral("\r\n    <div");
            BeginWriteAttribute("class", " class=\"", 205, "\"", 259, 1);
#nullable restore
#line 9 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
WriteAttributeValue("", 213, $"mvc-grid-pager {Model.CssClasses}".Trim(), 213, 46, false);

#line default
#line hidden
#nullable disable
            EndWriteAttribute();
            WriteLiteral(" data-show-page-sizes=\"");
#nullable restore
#line 9 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
                                                                                 Write(Model.ShowPageSizes);

#line default
#line hidden
#nullable disable
            WriteLiteral("\" data-total-rows=\"");
#nullable restore
#line 9 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
                                                                                                                        Write(Model.TotalRows);

#line default
#line hidden
#nullable disable
            WriteLiteral("\">\r\n");
#nullable restore
#line 10 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
     if (totalPages > 0)
    {
        if (currentPage > 1)
        {

#line default
#line hidden
#nullable disable
            WriteLiteral("        <button type=\"button\" data-page=\"1\">&#171;</button>\r\n        <button type=\"button\" data-page=\"");
#nullable restore
#line 15 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
                                     Write(currentPage - 1);

#line default
#line hidden
#nullable disable
            WriteLiteral("\">&#8249;</button>\r\n");
#nullable restore
#line 16 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
        }
        else
        {

#line default
#line hidden
#nullable disable
            WriteLiteral("        <button type=\"button\" class=\"disabled\" tabindex=\"-1\">&#171;</button>\r\n        <button type=\"button\" class=\"disabled\" tabindex=\"-1\">&#8249;</button>\r\n");
#nullable restore
#line 21 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
        }
        for (Int32 page = firstDisplayPage; page <= totalPages && page < firstDisplayPage + Model.PagesToDisplay; page++)
        {
            if (page == currentPage)
            {

#line default
#line hidden
#nullable disable
            WriteLiteral("        <button type=\"button\" class=\"active\" data-page=\"");
#nullable restore
#line 26 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
                                                    Write(page);

#line default
#line hidden
#nullable disable
            WriteLiteral("\">");
#nullable restore
#line 26 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
                                                             Write(page);

#line default
#line hidden
#nullable disable
            WriteLiteral("</button>\r\n");
#nullable restore
#line 27 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
            }
            else
            {

#line default
#line hidden
#nullable disable
            WriteLiteral("        <button type=\"button\" data-page=\"");
#nullable restore
#line 30 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
                                     Write(page);

#line default
#line hidden
#nullable disable
            WriteLiteral("\">");
#nullable restore
#line 30 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
                                              Write(page);

#line default
#line hidden
#nullable disable
            WriteLiteral("</button>\r\n");
#nullable restore
#line 31 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
            }
        }
        if (currentPage < totalPages)
        {

#line default
#line hidden
#nullable disable
            WriteLiteral("        <button type=\"button\" data-page=\"");
#nullable restore
#line 35 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
                                     Write(currentPage + 1);

#line default
#line hidden
#nullable disable
            WriteLiteral("\">&#8250;</button>\r\n        <button type=\"button\" data-page=\"");
#nullable restore
#line 36 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
                                    Write(totalPages);

#line default
#line hidden
#nullable disable
            WriteLiteral("\">&#187;</button>\r\n");
#nullable restore
#line 37 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
        }
        else
        {

#line default
#line hidden
#nullable disable
            WriteLiteral("        <button type=\"button\" class=\"disabled\" tabindex=\"-1\">&#8250;</button>\r\n        <button type=\"button\" class=\"disabled\" tabindex=\"-1\">&#187;</button>\r\n");
#nullable restore
#line 42 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
        }
        if (Model.ShowPageSizes)
        {

#line default
#line hidden
#nullable disable
            WriteLiteral("        <div class=\"mvc-grid-page-sizes\">\r\n");
#nullable restore
#line 46 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
         if (Model.PageSizes.Count > 0)
        {

#line default
#line hidden
#nullable disable
            WriteLiteral("            <select class=\"mvc-grid-pager-rows\">\r\n");
#nullable restore
#line 49 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
             foreach (KeyValuePair<Int32, String> size in Model.PageSizes)
            {
                if (Model.RowsPerPage == size.Key)
                {

#line default
#line hidden
#nullable disable
            WriteLiteral("                ");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("option", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "61137cc95270adb7dd63e526c7c8e5bfca41056c10007", async() => {
#nullable restore
#line 53 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
                                              Write(size.Value);

#line default
#line hidden
#nullable disable
            }
            );
            __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.OptionTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper);
            BeginWriteTagHelperAttribute();
#nullable restore
#line 53 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
                   WriteLiteral(size.Key);

#line default
#line hidden
#nullable disable
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper.Value = __tagHelperStringValueBuffer;
            __tagHelperExecutionContext.AddTagHelperAttribute("value", __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper.Value, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            BeginWriteTagHelperAttribute();
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __tagHelperExecutionContext.AddHtmlAttribute("selected", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.Minimized);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\r\n");
#nullable restore
#line 54 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
                }
                else
                {

#line default
#line hidden
#nullable disable
            WriteLiteral("                ");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("option", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "61137cc95270adb7dd63e526c7c8e5bfca41056c12384", async() => {
#nullable restore
#line 57 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
                                     Write(size.Value);

#line default
#line hidden
#nullable disable
            }
            );
            __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.OptionTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper);
            BeginWriteTagHelperAttribute();
#nullable restore
#line 57 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
                   WriteLiteral(size.Key);

#line default
#line hidden
#nullable disable
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper.Value = __tagHelperStringValueBuffer;
            __tagHelperExecutionContext.AddTagHelperAttribute("value", __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper.Value, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\r\n");
#nullable restore
#line 58 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
                }
            }

#line default
#line hidden
#nullable disable
            WriteLiteral("            </select>\r\n");
#nullable restore
#line 61 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
        }
        else
        {

#line default
#line hidden
#nullable disable
            WriteLiteral("            <input class=\"mvc-grid-pager-rows\"");
            BeginWriteAttribute("value", " value=\"", 2264, "\"", 2290, 1);
#nullable restore
#line 64 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
WriteAttributeValue("", 2272, Model.RowsPerPage, 2272, 18, false);

#line default
#line hidden
#nullable disable
            EndWriteAttribute();
            WriteLiteral(" />\r\n");
#nullable restore
#line 65 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
        }

#line default
#line hidden
#nullable disable
            WriteLiteral("        </div>\r\n");
#nullable restore
#line 67 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
        }
        else
        {

#line default
#line hidden
#nullable disable
            WriteLiteral("        <input class=\"mvc-grid-pager-rows\" type=\"hidden\"");
            BeginWriteAttribute("value", " value=\"", 2415, "\"", 2441, 1);
#nullable restore
#line 70 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
WriteAttributeValue("", 2423, Model.RowsPerPage, 2423, 18, false);

#line default
#line hidden
#nullable disable
            EndWriteAttribute();
            WriteLiteral(" />\r\n");
#nullable restore
#line 71 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
        }
    }
    else
    {

#line default
#line hidden
#nullable disable
            WriteLiteral("        <input class=\"mvc-grid-pager-rows\" type=\"hidden\"");
            BeginWriteAttribute("value", " value=\"", 2538, "\"", 2564, 1);
#nullable restore
#line 75 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
WriteAttributeValue("", 2546, Model.RowsPerPage, 2546, 18, false);

#line default
#line hidden
#nullable disable
            EndWriteAttribute();
            WriteLiteral(" />\r\n");
#nullable restore
#line 76 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\MvcGrid\_Pager.cshtml"
    }

#line default
#line hidden
#nullable disable
            WriteLiteral("    </div>\r\n");
        }
        #pragma warning restore 1998
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<IGridPager> Html { get; private set; }
    }
}
#pragma warning restore 1591
