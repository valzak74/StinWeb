#pragma checksum "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\ИнтернетЗаказы\Orders.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "00e7596737eb705c7dfc9416f0b5f5d753ecd4bf"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Views_ИнтернетЗаказы_Orders), @"mvc.1.0.view", @"/Views/ИнтернетЗаказы/Orders.cshtml")]
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
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"00e7596737eb705c7dfc9416f0b5f5d753ecd4bf", @"/Views/ИнтернетЗаказы/Orders.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"303b915066d1efa5c78241ff8833664880cca93b", @"/Views/_ViewImports.cshtml")]
    public class Views_ИнтернетЗаказы_Orders : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<IQueryable<StinWeb.Models.DataManager.Отчеты.MarketplaceOrder>>
    {
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
#nullable restore
#line 2 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\ИнтернетЗаказы\Orders.cshtml"
Write(Html
    .Grid(Model)
    .Build(columns =>
    {
    columns.Add(model => model.Id).Named("Id").Hidden();
    columns.Add(model => model.MarketplaceType).Titled("Тип");
    columns.Add(model => model.MarketplaceId).Titled("#");
    columns.Add(model => model.ПредварительнаяЗаявкаНомер).Titled("№");
    columns.Add(model => model.ShipmentDate).Titled("Дата отгрузки").Formatted("{0:d}");
    columns.Add(model => model.StatusDescription).Titled("Статус");
    columns.Add(model => model.ИнформацияAPI).Titled("Информация");
    columns.Add(model => ((model.Labels != null) && (model.Labels.Length > 0)) ? $"<i class=\"fa fa-file-pdf-o\" style=\"font-size:18px;color:red\" onclick='GetLabels(\"" + model.Id + "\")'></i>" : "")
        .Titled("Этикетка")
        .Css("text-center column-10")
        .Encoded(false);
        //columns.Add(model => model.ЗаявкаНаСогласование).Titled("На Согласование").Filterable(false).Sortable(false).Css("text-right  column-10").Formatted(StinWeb.Models.DataManager.Common.ФорматЦены);
        //columns.Add(model => model.ЗаявкаСогласованная).Titled("Согласовано").Filterable(false).Sortable(false).Css("text-right  column-10").Formatted(StinWeb.Models.DataManager.Common.ФорматЦены);
        //columns.Add(model => model.ЗаявкаОдобренная).Titled("Одобрено").Filterable(false).Sortable(false).Css("text-right  column-10").Formatted(StinWeb.Models.DataManager.Common.ФорматЦены);
        //columns.Add(model => model.СчетНаОплату).Titled("Счет").Filterable(false).Sortable(false).Css("text-right  column-10").Formatted(StinWeb.Models.DataManager.Common.ФорматЦены);
        //columns.Add(model => model.ОтменаЗаявки ? $"<img src=\"/images/Fail.svg\" />" : "")
        //        .Titled("Отмена")
        //        .Css("text-center column-10")
        //        .Encoded(false);
        //columns.Add(model => model.ОплатаОжидание).Titled("Ожидание оплаты").Filterable(false).Sortable(false).Css("text-right  column-10").Formatted(StinWeb.Models.DataManager.Common.ФорматЦены);
        ////columns.Add(model => model.ОплатаОтменено > 0 && model.ОплатаВыполнено == 0).Titled("оплата отменена").Filterable(false).Sortable(false);
        //columns.Add(model => model.ОплатаОтменено > 0 && model.ОплатаВыполнено == 0 ? $"<img src=\"/images/Fail.svg\" />" : "")
        //        .Titled("Оплата отменена")
        //        .Css("text-center column-10")
        //        .Encoded(false);
        //columns.Add(model => model.ОплатаВыполнено).Titled("Оплачено").Filterable(false).Sortable(false).Css("text-right  column-10").Formatted(StinWeb.Models.DataManager.Common.ФорматЦены);
        //columns.Add(model => model.Набор).Titled("Набор").Filterable(false).Sortable(false).Css("text-right  column-10").Formatted(StinWeb.Models.DataManager.Common.ФорматЦены);
        //columns.Add(model => model.ОтменаНабора).Titled("Отмена набора").Filterable(false).Sortable(false).Css("text-right  column-10").Formatted(StinWeb.Models.DataManager.Common.ФорматЦены);
        //columns.Add(model => model.Продажа).Titled("Продажа").Filterable(false).Sortable(false).Css("text-right  column-10").Formatted(StinWeb.Models.DataManager.Common.ФорматЦены);
    })
    .Named("gridConsole")
    .AppendCss("table-hover")
    //.RowAttributed(model => new { id = model.Корень.IdDoc, @class = model.ЗаявкаИсполненная ? "голубая" : model.ОтменаЗаявки ? "снят_с_производства" : model.ОплатаВыполнено > 0 ? "под_заказ" : null })
    .Using(GridFilterMode.Header)
    .Empty("No data found")
    .Pageable(pager =>
    {
        pager.RowsPerPage = 25;
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
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<IQueryable<StinWeb.Models.DataManager.Отчеты.MarketplaceOrder>> Html { get; private set; }
    }
}
#pragma warning restore 1591
