#pragma checksum "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\Components\СтруктураПодчиненности\Default.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "828c6036d27be38392769f06c3cdba45d26303a5"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Views_Shared_Components_СтруктураПодчиненности_Default), @"mvc.1.0.view", @"/Views/Shared/Components/СтруктураПодчиненности/Default.cshtml")]
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
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"828c6036d27be38392769f06c3cdba45d26303a5", @"/Views/Shared/Components/СтруктураПодчиненности/Default.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"303b915066d1efa5c78241ff8833664880cca93b", @"/Views/_ViewImports.cshtml")]
    public class Views_Shared_Components_СтруктураПодчиненности_Default : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral(@"
<div class=""modal fade"" id=""структураПодчиненности"" tabindex=""-1"" role=""dialog"" aria-hidden=""true"">
    <div class=""modal-dialog"" role=""document"" style=""position: center;display:table;overflow: auto;width: auto;min-width:300px;"">
        <div class=""modal-content"">
            <div class=""modal-header"">
                <h4 id=""общиеРеквизитыДокумента"" style=""color:darkred;font-weight:bold;"">Структура подчиненности</h4>
                <button type=""button"" class=""close"" data-dismiss=""modal"" aria-label=""Close"">
                    <span aria-hidden=""true"">&times;</span>
                </button>
            </div>
            <div class=""modal-body"" style=""overflow-x:auto !important;max-width:90vw !important;"">
                ");
#nullable restore
#line 12 "C:\Users\Валентин\source\repos\StinWeb\StinWeb\Views\Shared\Components\СтруктураПодчиненности\Default.cshtml"
           Write(Html.Hidden("структураПодчиненностиIdDoc", null, new { id = "структураПодчиненностиIdDoc" }));

#line default
#line hidden
#nullable disable
            WriteLiteral(@"
                <div id='treeСтруктураПодчиненности'></div>
            </div>
        </div>
    </div>
</div>

<style>
    .jstree-default-large .jstree-ok,
    .jstree-default-large .jstree-er {
        background-image: url(""vakata-jstree/dist/themes/default/32px.png"");
        background-repeat: no-repeat;
        background-color: transparent;
    }
    .jstree-default-large .jstree-ok {
        background-position: 0px -64px;
    }
    .jstree-default-large .jstree-er {
        background-position: -32px -64px;
    }
    .jstree-default-large .jstree-icon:empty {
        height: 60px;
        line-height: 60px;
    }
    .jstree-default-large .jstree-open > .jstree-ocl {
        background-position: -221px -62px;
    }
    .jstree-default-large .jstree-closed > .jstree-ocl {
        background-position: -176px -64px;
    }
    .jstree-default-large .jstree-leaf > .jstree-ocl {
        background-position: -129px -62px;
    }
</style>

<script>
    $('#структураПодчи");
            WriteLiteral(@"ненности').on('show.bs.modal', function (e) {
        $('#treeСтруктураПодчиненности').jstree({
            ""plugins"": [""core"", ""themes"", ""search"", ""types""], //, ""grid""],
            ""search"": {
                ""case_insensitive"": true,
                ""show_only_matches"": true
            },
            //""grid"": {
            //    ""columns"": [
            //        { header: ""Работы"" },
            //        {
            //            header: ""Цена"", value: function (node) {
            //                if (node.data) {
            //                    return node.data.price;
            //                }
            //            }
            //        }
            //    ],
            //},
            types: {
                ""root"": {
                    ""icon"": ""glyphicon glyphicon-plus""
                },
                ""ok"": {
                    ""icon"": ""jstree-ok""
                },
                ""er"": {
                    ""icon"": ""jstree-er""
                }");
            WriteLiteral(@",
                ""child"": {
                    ""icon"": ""fa fa-wrench fa-lg""
                },
                ""default"": {
                    ""icon"": ""jstree-file""
                }
            },
            'core': {
                'animation': 1,
                'open': true,
                'themes': {
                    //""name"": 'default-dark',
                    ""variant"": ""large"",
                    ""dots"": true,
                    ""responsive"": true,
                    ""stripes"": false,
                    //""ellipsis"": true
                //    'name': ""dark"",
                //    'icons': this._data.core.themes.icons,
                //    'dots': this._data.core.themes.dots
                },
                'data': {
                    'url': function (node) {
                        return node.id === '#' ?
                            'СтруктураПодчиненности?idDoc=' + $(""#структураПодчиненностиIdDoc"").val().replaceAll("" "",""_"") + ""&findRoot=true"" :
         ");
            WriteLiteral(@"                   'СтруктураПодчиненности?idDoc=' + node.id;
                    },
                    'data': function (node) {
                        return {
                            'id': node.id,
                        };
                    }
                }
            }
        });
    });
    $('#структураПодчиненности').on('hidden.bs.modal', function (e) {
        $('#treeСтруктураПодчиненности').jstree().destroy();
    });
    $('#структураПодчиненности').on(""dblclick.jstree"", function (e) {
        var instance = $('#treeСтруктураПодчиненности').jstree();
        var node = instance.get_node(e.target);
        if (node != undefined) {
            //console.log(node.id.replaceAll(""_"", "" ""));
        }
    //    if (node.icon == 'jstree-file') {
    //        $.ajax({
    //            url: ""Корзина/ДобавитьВПодбор"",
    //            type: ""post"",
    //            dataType: ""xml"",
    //            data: { sessionKey: ""ПодборРабот"", id: node.id, наименование: node");
            WriteLiteral(@".text, цена: node.data.price, количество: 1 },
    //            async: true,
    //            cache: false,
    //            success: function () {
    //                var gridКорзина = new MvcGrid(document.querySelector(""#ПодборРаботm""));
    //                gridКорзина.set({
    //                    isAjax: true,
    //                    url: ""Корзина/ПолучитьДанные"",
    //                    query: ""key=ПодборРабот""
    //                        + ""&modalVersion=true""
    //                        + ""&showЦены=true""
    //                });
    //                gridКорзина.reload();
    //            },
    //        });

    //    }
    });
</script>");
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
