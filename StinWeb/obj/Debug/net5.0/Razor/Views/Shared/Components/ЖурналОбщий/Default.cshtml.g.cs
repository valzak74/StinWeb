#pragma checksum "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\Components\ЖурналОбщий\Default.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "a0be06e1ba6c38a341814b86ff96bfa8d6079156"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Views_Shared_Components_ЖурналОбщий_Default), @"mvc.1.0.view", @"/Views/Shared/Components/ЖурналОбщий/Default.cshtml")]
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
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"a0be06e1ba6c38a341814b86ff96bfa8d6079156", @"/Views/Shared/Components/ЖурналОбщий/Default.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"303b915066d1efa5c78241ff8833664880cca93b", @"/Views/_ViewImports.cshtml")]
    public class Views_Shared_Components_ЖурналОбщий_Default : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<IQueryable<StinWeb.Models.DataManager.Документы.ОбщиеРеквизиты>>
    {
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
#nullable restore
#line 2 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\Components\ЖурналОбщий\Default.cshtml"
Write(Html
    .Grid(Model)
    .Build(columns =>
    {
        columns.Add(model => $"<img src=\"/images/" + (model.Удален ? "blog-post-delete-icon-24.png" : (model.Проведен ? "page-accept-icon-24.png" : "page-icon-24.png")) + "\" />")
        .Css("text-center column-10")
        .Encoded(false);
        columns.Add(model => model.IdDoc).Named("IdDoc").Titled("IdDoc").Hidden();
        columns.Add(model => model.ВидДокумента10).Titled("ВидДокумента10").Hidden();
        columns.Add(model => model.ВидДокумента36).Titled("ВидДокумента36").Hidden();
        columns.Add(model => model.Наименование).Titled("Наименование").Hidden();
        columns.Add(model => model.НазваниеВЖурнале).Titled("Наименование");
        columns.Add(model => model.НомерДок).Titled("Номер");
        columns.Add(model => model.ДатаДок).Titled("Дата");
        columns.Add(model => model.Информация).Titled("Информация");
        columns.Add(model => model.Фирма.Наименование).Titled("Фирма");
        columns.Add(model => model.Автор.Name).Titled("Автор");
        columns.Add(model => model.Комментарий).Titled("Комментарий").Filterable(false).Sortable(false);
    })
    .Named("gridЖурналОбщий")
    .AppendCss("table-hover")
    .RowAttributed(model => new { id = model.IdDoc, @class = model.Удален ? "снят_с_производства" : !model.Проведен ? "под_заказ" : null })
    .Using(GridFilterMode.Header)
    .Empty("No data found")
    .Pageable(pager =>
    {
        pager.RowsPerPage = 15;
    })
    .Filterable()
    .Sortable()
);

#line default
#line hidden
#nullable disable
            WriteLiteral("\r\n");
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
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<IQueryable<StinWeb.Models.DataManager.Документы.ОбщиеРеквизиты>> Html { get; private set; }
    }
}
#pragma warning restore 1591
