// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
const grid = document.querySelector(".mvc-grid");

// Triggered when grid's row is clicked. It's recommended to use event delegation in ajax scenarios.
document.addEventListener("rowclick", e => {
    //alert(e.detail.data['НомерКвитанции']);
    //alert(e.detail.data['ДатаКвитанции']);
    //alert(e.detail.originalEvent);
    var baseUrl = window.location.origin;
    switch (e.detail.grid.name)
    {
        case "GridDiagnostics":
            {
                window.location.href = baseUrl + '/DocRezDiagnostics?IzdelieId=' + e.detail.data['ИзделиеId'] + '&Kvit=' + e.detail.data['Квитанция'];
                break;
            }
        case "GridНоменклатура":
            {
                if (e.detail.data['Id'] != null)
                    $.ajax({
                        url: baseUrl + "/BasketНоменклатураViewComponent/AddBasket",
                        type: "post",
                        dataType: "xml",
                        data: { id: e.detail.data['Id'] },
                        async: true,
                        //processData: false,
                        cache: false,
                        success: function (data) {
                            //$.ajax({
                            //    url: baseUrl + "/BasketНоменклатура",
                            //    type: "get"
                            //})
                            //$('#divOne')//.get("BasketНоменклатура")
                            //    .html(data);
                            e.detail.grid.reload()
                        },
                        error: function (jqXHR, exception) {
                            var msg = '';
                            if (jqXHR.status === 0) {
                                msg = 'Not connect.\n Verify Network.';
                            } else if (jqXHR.status == 404) {
                                msg = 'Requested page not found. [404]';
                            } else if (jqXHR.status == 500) {
                                msg = 'Internal Server Error [500].';
                            } else if (exception === 'parsererror') {
                                msg = 'Requested JSON parse failed.';
                            } else if (exception === 'timeout') {
                                msg = 'Time out error.';
                            } else if (exception === 'abort') {
                                msg = 'Ajax request aborted.';
                            } else {
                                msg = 'Uncaught Error.\n' + jqXHR.responseText;
                            }
                            $('#divOne').html(msg);
                        },
                    });
                break;
            }
        case "GridBasketНоменклатура":
            {
                if (e.detail.data['Номенклатура.Id'] != null)
                    $.ajax({
                        url: baseUrl + "/BasketНоменклатураViewComponent/Remove",
                        type: "post",
                        dataType: "xml",
                        data: { id: e.detail.data['Номенклатура.Id'] },
                        complete: function (data) { e.detail.grid.reload() }
                    });
                break;
            }
    }
    //if (e.detail.grid.name == 'GridDiagnostics') {
    //    var parКвитанция = e.detail.data['Квитанция'];
    //    var parИзделие = e.detail.data['ИзделиеId'];
    //    //$.ajax({
    //    //    url: 'Details',
    //    //    data: { id: parameter },
    //    //    type: "GET",
    //    //    success: function () {
    //    //        alert('Added');
    //    //    }
    //    //});
    //    window.location.href = baseUrl + '/DocRezDiagnostics?IzdelieId=' + parИзделие + '&Kvit=' + parКвитанция;
    //}
    //e.detail.data - clicked row's data from columns.
    //e.detail.grid - grid's instance.
    //e.detail.originalEvent - original tr click event which triggered the rowclick.
});

