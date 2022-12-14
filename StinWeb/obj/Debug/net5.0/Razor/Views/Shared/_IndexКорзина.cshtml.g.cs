#pragma checksum "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\_IndexКорзина.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "9fd9af97771bfcef1358c26dce5005628f1b67bf"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Views_Shared__IndexКорзина), @"mvc.1.0.view", @"/Views/Shared/_IndexКорзина.cshtml")]
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
#line 2 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\_ViewImports.cshtml"
using NonFactors.Mvc.Grid;

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
#line 2 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\_IndexКорзина.cshtml"
using StinWeb.Models.DataManager.Extensions;

#line default
#line hidden
#nullable disable
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"9fd9af97771bfcef1358c26dce5005628f1b67bf", @"/Views/Shared/_IndexКорзина.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"303b915066d1efa5c78241ff8833664880cca93b", @"/Views/_ViewImports.cshtml")]
    public class Views_Shared__IndexКорзина : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<StinWeb.Models.DataManager.ДанныеКорзины>
    {
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral("\r\n");
#nullable restore
#line 4 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\_IndexКорзина.cshtml"
Write(Html
    .Grid(Model.Данные)
    .Build(columns =>
    {
        if (Model.modalVersion)
            columns.Add(model => $"<img onclick=\"DeleteRow(this, '{Model.Key}','{model.Id}');\" src=\"/images/Fail.svg\" />")
            .Css("text-center column-10")
            .Encoded(false);
        else
            columns.Add(model => $"<img onclick=\"DeleteRow(this, '{Model.Key}','{model.Id}');\" src=\"/images/Fail.svg\" />")
            .Titled(Html.Button("+", new { @type = "button", @class = "btn btn-sm btn-outline-success", onclick = "btnClickПодбор('" + @Model.Key + "');" }))
            .Css("text-center column-10")
            .Encoded(false);
        columns.Add().RenderedAs((model, row) => row + 1).Titled("#").Css("text-center  column-10");
        columns.Add(model => model.Id).Hidden();
        if (Model.ShowАртикул)
            columns.Add(model => model.Артикул).Titled("Артикул");
        columns.Add(model => model.Наименование).Titled("Наименование");
        if (Model.ShowПроизводитель)
            columns.Add(model => model.Производитель).Titled("Производитель");
        if (Model.ShowЕдиницы)
        {
            columns.Add(model => model.ЕдиницаId).Hidden();
            columns.Add(model => model.ЕдиницаНаименование).Titled("Ед.");
        }
        columns.Add(model => model.Quantity).Titled("Кол-во").Css("column-10p").Css("text-right");
        if (Model.ShowЦены)
        {
            columns.Add(model => model.Цена).Titled("Цена").Css("column-10p").Css("text-right").Formatted("{0:C}");
            columns.Add(model => model.Сумма).Titled("Сумма").Css("column-10p").Css("text-right").Formatted("{0:C}");
        }
    })
    .Id(Model.Key + (Model.modalVersion ? "m" : ""))
    .AppendCss("table-hover")
    .Empty("No data found")
    .UsingFooter(Model.ShowЦены ? "_IndexКорзинаFooter" : "")
);

#line default
#line hidden
#nullable disable
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
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<StinWeb.Models.DataManager.ДанныеКорзины> Html { get; private set; }
    }
}
#pragma warning restore 1591
