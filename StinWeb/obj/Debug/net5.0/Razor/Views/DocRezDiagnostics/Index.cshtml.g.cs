#pragma checksum "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\DocRezDiagnostics\Index.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "34d4cae0b83f58ff46d9b9966d13237927d4d230"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Views_DocRezDiagnostics_Index), @"mvc.1.0.view", @"/Views/DocRezDiagnostics/Index.cshtml")]
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
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"34d4cae0b83f58ff46d9b9966d13237927d4d230", @"/Views/DocRezDiagnostics/Index.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"303b915066d1efa5c78241ff8833664880cca93b", @"/Views/_ViewImports.cshtml")]
    public class Views_DocRezDiagnostics_Index : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
#nullable restore
#line 1 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\DocRezDiagnostics\Index.cshtml"
  
    ViewBag.Title = "Диагностика";

#line default
#line hidden
#nullable disable
            WriteLiteral("    <div id=\"mainPane\" class=\"row splitter_container\">\r\n        <div>\r\n            <div class=\"row\">\r\n                <div class=\"col-md-6 border-round\">\r\n                    ");
#nullable restore
#line 7 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\DocRezDiagnostics\Index.cshtml"
               Write(await Component.InvokeAsync("Rg9972Details", new { id = ViewBag.Квитанция }));

#line default
#line hidden
#nullable disable
            WriteLiteral("\r\n                </div>\r\n                <div class=\"col-6 border-round\">\r\n                    ");
#nullable restore
#line 10 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\DocRezDiagnostics\Index.cshtml"
               Write(await Component.InvokeAsync("ВыборТипаРемонта"));

#line default
#line hidden
#nullable disable
            WriteLiteral("\r\n                </div>\r\n            </div>\r\n            <div class=\"row\">\r\n                <div class=\"col-md-6 border-round\">\r\n                    ");
#nullable restore
#line 15 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\DocRezDiagnostics\Index.cshtml"
               Write(await Component.InvokeAsync("Номенклатура"));

#line default
#line hidden
#nullable disable
            WriteLiteral("\r\n                </div>\r\n                <div id=\"divOne\" class=\"col-md-6 border-round\">\r\n                    ");
#nullable restore
#line 18 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\DocRezDiagnostics\Index.cshtml"
               Write(await Component.InvokeAsync("BasketНоменклатура"));

#line default
#line hidden
#nullable disable
            WriteLiteral("\r\n                </div>\r\n            </div>\r\n            <div class=\"row\">\r\n                <div class=\"col-md-6 border-round\">\r\n                    ");
#nullable restore
#line 23 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\DocRezDiagnostics\Index.cshtml"
               Write(await Component.InvokeAsync("Работы"));

#line default
#line hidden
#nullable disable
            WriteLiteral("\r\n                </div>\r\n                <div class=\"col-md-6 border-round\">\r\n                    ");
#nullable restore
#line 26 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\DocRezDiagnostics\Index.cshtml"
               Write(await Component.InvokeAsync("КорзинаРабот"));

#line default
#line hidden
#nullable disable
            WriteLiteral("\r\n                </div>\r\n            </div>\r\n        </div>\r\n        <div id=\"rightPane\" class=\"image-embed-container border-round\">\r\n");
            WriteLiteral("            <iframe");
            BeginWriteAttribute("src", " src=\"", 1385, "\"", 1468, 2);
#nullable restore
#line 32 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\DocRezDiagnostics\Index.cshtml"
WriteAttributeValue("", 1391, Url.Action("GetImage","Раскладки", new { id = ViewBag.ИзделиеId }), 1391, 67, false);

#line default
#line hidden
#nullable disable
            WriteAttributeValue("", 1458, "#view=fitH", 1458, 10, true);
            EndWriteAttribute();
            WriteLiteral(@" frameborder=""0"" allowfullscreen></iframe>
        </div>
    </div>
    <div class=""row border-round"">
        <button id=""btnOk"" class=""btn btn-primary"" type=""button"" onclick=""btnOkClick()"">Ok</button>
    </div>
    <script>
        $(""#rightPane"").slimScroll({ height: '100%' });
        var splitter = $('#mainPane').split({
            orientation: 'vertical',
            limit: 10,
            position: '50%',
        });

    function btnOkClick() {
        $.ajax({
            url: window.location.origin + ""/DocRezDiagnostics/OnBtnOk"",
            type: ""post"",
            datatype: ""json"",
            data: { Квитанция: '");
#nullable restore
#line 51 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\DocRezDiagnostics\Index.cshtml"
                           Write(ViewBag.Квитанция);

#line default
#line hidden
#nullable disable
            WriteLiteral(@"', Гарантийный: $('input:radio[name=ТипРемонта]:checked').val() == ""Гарантийный"", Требуется: $.trim($('textarea[name=Требуется]').val()) },
            async: true,
            cache: false,
            success: function (data) {
                document.location = window.location.origin + ""/Rg9972/IndexDiagnostics"";
            },
            error: function (e, v, f) {
                alert(""ошибка выполнения!"");
            }
        });
    };
    </script>
");
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
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
#pragma warning restore 1591
