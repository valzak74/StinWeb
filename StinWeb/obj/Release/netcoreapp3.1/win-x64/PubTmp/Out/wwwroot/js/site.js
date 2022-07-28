// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function EnableBusyScreen(obj) {
    const loading = document.createElement("div");
    loading.id = 'loading';
    const loader = document.createElement("template");
    loader.innerHTML = `<div class="mvc-grid-loader"><div><div></div><div></div><div></div></div></div>`;
    const firstElement = loader.content.firstElementChild;
    loading.appendChild(loader.content.firstElementChild);
    if (obj) {
        obj.appendChild(loading);
    } else {
        document.getElementsByTagName('body')[0].appendChild(loading);
    }
    firstElement.classList.add("mvc-grid-loading");
}
function DisableBusyScreen(obj) {
    const loading = document.getElementById("loading");
    if (loading) {
        if (obj) {
            obj.removeChild(loading);
        } else {
            document.getElementsByTagName('body')[0].removeChild(loading);
        }
    }
}

var refreshTimeout = 20000;

var formatter = new Intl.NumberFormat('ru-RU', {
    style: 'currency',
    currency: 'RUB',

    // These options are needed to round to whole numbers if that's what you want.
    //minimumFractionDigits: 0, // (this suffices for whole numbers, but will print 2500.10 as $2,500.1)
    //maximumFractionDigits: 0, // (causes 2500.99 to be printed as $2,501)
});
//use formatter.format(2500);
var minDate = new Date('0001-01-01');
var minDate1C = new Date('1753-01-01');
var formatterDate = new Intl.DateTimeFormat("ru", {
    //weekday: "long",
    year: "numeric",
    month: "numeric",
    day: "numeric",
    //hour: "numeric",
    //minute: "numeric",
    //second: "numeric"
});
var formatterTime = new Intl.DateTimeFormat("ru", {
    hour: "numeric",
    minute: "numeric",
    second: "numeric"
});

var afterPrint = function (e) {
    $(window).off('mousemove', afterPrint);
    $('iframe').css('pointer-events', 'auto');
    var element = document.getElementById("printFrame");
    document.body.removeChild(element);
};

function setPrint() {
    this.contentWindow.focus(); // Required for IE
    this.contentWindow.print();
    setTimeout(function () {
        $('iframe').css('pointer-events', 'none');
        $(window).one('mousemove', afterPrint);
    }, 1000);

}

function printPage(sURL, isSRC) {
    var oHiddFrame = document.createElement("iframe");
    oHiddFrame.onload = setPrint;
    oHiddFrame.style.position = "fixed";
    oHiddFrame.style.right = "0";
    oHiddFrame.style.bottom = "0";
    oHiddFrame.style.width = "0";
    oHiddFrame.style.height = "0";
    oHiddFrame.style.border = "0";
    oHiddFrame.id = "printFrame";
    if (isSRC)
        oHiddFrame.src = sURL;
    else
        oHiddFrame.srcdoc = sURL;
    document.body.appendChild(oHiddFrame);
}

const grid = document.querySelector(".mvc-grid");

function InputGroupCheckDisable(id, isChecked) {
    $("#" + id).prop("disabled", !isChecked);
    if (isChecked)
        $("#" + id).val($("#" + id + " option:first").val());
    else
        $("#" + id).val("");
};
function SetControlActive(controlType, divId, controlId) {
    $('#' + divId).on('shown.bs.' + controlType, function () {
        $('#' + controlId).focus();
    });
    $("#" + controlId).focus(function () {
        var save_this = $(this);
        window.setTimeout(function () {
            save_this.select();
        }, 10);
    });
}
// Triggered when grid's row is clicked. It's recommended to use event delegation in ajax scenarios.
document.addEventListener("rowclick", e => {
    //console.log('data: ', e.detail.data);
    //console.log('grid: ', e.detail.grid);
    //console.log('original event: ', e.detail.originalEvent);
    //alert(e.detail.data['НомерКвитанции']);
    //alert(e.detail.data['ДатаКвитанции']);
    //alert(e.detail.originalEvent);
    var baseUrl = window.location.origin;
    switch (e.detail.grid.name)
    {
        case "GridDiagnostics":
            {
                $.ajax({
                    url: baseUrl + "/DocRezDiagnostics/ClearAndLoad",
                    type: "post",
                    dataType: "xml",
                    data: { Квитанция: e.detail.data['Квитанция'] },
                    async: true,
                    cache: false,
                    complete: function (data) { document.location = baseUrl + '/DocRezDiagnostics?IzdelieId=' + e.detail.data['ИзделиеId'] + '&Garantia=' + e.detail.data['Гарантия'] + '&Kvit=' + e.detail.data['Квитанция']; }
                });
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
                            //[].forEach.call(document.getElementsByClassName('mvc-grid'), function (element) {
                            //    if (element.id === "basketНоменклатура")
                            //    new MvcGrid(element).reload();
                            //});
                            new MvcGrid(document.querySelector('#basketНоменклатура')).reload();
                            //e.detail.grid.reload();
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
});

function CheckDateYear(e) {
    if (e.target.value != "") {
        var inputDate = e.target.valueAsDate;
        if (inputDate.getFullYear() < 100) {
            var year = inputDate.getFullYear();
            var month = inputDate.getMonth();
            var day = inputDate.getDate();
            if (year == 1) {
                var today = new Date();
                year = today.getFullYear() - 2000;
                if (month == 0)
                    month = today.getMonth();
                if (day == 1)
                    day = today.getDate();
            }
            var newDate = new Date(year + 2000, month, day);
            var tzoffset = (new Date()).getTimezoneOffset() * 60000; //offset in milliseconds
            e.target.value = (new Date(newDate - tzoffset)).toISOString().slice(0, 10);
        }
    }
}
function MakeКвитанцияId(e, defPref) {
    if (e.target.value != "") {
        var result = e.target.value;
        if (result.length < 13) {
            var RegExTemplate = /(?<pref>[A-Za-z]{0,2})(?<body>[0-9]{1,5})(?<minus>-{0,1})(?<year>[0-9]{0,4})/g;
            var match = RegExTemplate.exec(result);
            if (match != null) {
                var pref = match.groups.pref.length == 2 ? match.groups.pref.toUpperCase() : defPref.toUpperCase();
                var body = match.groups.body;
                while (body.length < 5) {
                    body = "0" + body;
                }
                var year = match.groups.year;
                if (year.length == 2)
                    year = "20" + year;
                else if (year.length != 4)
                    year = new Date().getFullYear().toString();
                e.target.value = pref + body + "-" + year;
            }
        }
    }
}
function GetКвитанцияID(e) {
    var RegExTemplate = /(?<body>[A-Za-z0-9]{7})-(?<year>[0-9]{4})/g;
    var match = RegExTemplate.exec(e);
    if (match != null) {
        return [match.groups.body, match.groups.year];
    }
    return null;
}
function DateTimeToDDMMYYYY(e) {
    var result = "";
    if (e != undefined) {
        var mm = e.getMonth() + 1; //zero based
        var dd = e.getDate();
        result = (dd > 9 ? "" : "0") + dd + "." + (mm > 9 ? "" : "0") + mm + "." + e.getFullYear();
    }
    return result;
}
function DateTimeToDDMMYYYY_hmmss(e) {
    var result = DateTimeToDDMMYYYY(e);
    if (e != undefined) {
        var h = e.getHours();
        var mm = e.getMinutes();
        var ss = e.getSeconds();
        result += " " + h + ":" + (mm > 9 ? "" : "0") + mm + ":" + (ss > 9 ? "" : "0") + ss;
    }
    return result;
}
function ReadOnlyFlag(element, isReadOnly) {
    $(element).attr("readonly", isReadOnly);
    if (isReadOnly)
        $(element).css("pointer-events", "none");
    else
        $(element).css("pointer-events", "");
}
function LookupEmpty(element) {
    element.search.value = "";
    element.values[0].value = "";
}
function LookupUpdateValue(lookup, object) {
    if (object != undefined) {
        lookup.values[0].value = object.id;
        lookup.reload(true);
    } else
        LookupEmpty(lookup);
}
document.addEventListener("wellidate-error", e => {
    if (event.target.classList.contains("mvc-lookup-value")) {
        const { wellidate } = e.detail.validatable;
        const { control } = new MvcLookup(event.target);


        control.classList.add(wellidate.inputErrorClass);
        control.classList.remove(wellidate.inputValidClass);
        control.classList.remove(wellidate.inputPendingClass);
        for (var node of control.querySelectorAll('.mvc-lookup-input')) {
            node.classList.add("input-validation-error");
            node.classList.remove("input-validation-valid");
        }
    }
});
document.addEventListener("wellidate-success", e => {
    if (event.target.classList.contains("mvc-lookup-value")) {
        const { wellidate } = e.detail.validatable;
        const { control } = new MvcLookup(event.target);


        control.classList.add(wellidate.inputValidClass);
        control.classList.remove(wellidate.inputErrorClass);
        control.classList.remove(wellidate.inputPendingClass);
        for (var node of control.querySelectorAll('.mvc-lookup-input')) {
            node.classList.add("input-validation-valid");
            node.classList.remove("input-validation-error");
        }
    }
});

function RefreshSummary(summaryName, summaryFields) {
    var errors = $(".input-validation-error");
    const summary = document.getElementById(summaryName);
    if (summary) {
        summary.innerHTML = "";
        if (errors.length == 0) {
            summary.classList.add("validation-summary-valid");
            summary.classList.remove("validation-summary-errors");
        } else {
            summary.classList.add("validation-summary-errors");
            summary.classList.remove("validation-summary-valid");

            const list = document.createElement("ul");

            for (const invalid of errors) {
                if (summaryFields.indexOf(invalid.name) >= 0) {
                    const item = document.createElement("li");

                    item.innerHTML = invalid.validationMessage;

                    list.appendChild(item);
                }
            }

            summary.appendChild(list);
        }
    }
};
function CreateManualSummary(result, summaryName, summaryFields) {
    const summary = document.getElementById(summaryName);
    if (summary) {
        summary.innerHTML = "";

        if (result.isValid) {
            summary.classList.add("validation-summary-valid");
            summary.classList.remove("validation-summary-errors");
        } else {
            var SummaryIsValid = true;
            for (const invalid of result.invalid) {
                if (summaryFields.indexOf(invalid.validatable.element.name) >= 0) {
                    SummaryIsValid = false;
                    break;
                }
            }
            if (SummaryIsValid) {
                summary.classList.add("validation-summary-valid");
                summary.classList.remove("validation-summary-errors");
            }
            else {
                summary.classList.add("validation-summary-errors");
                summary.classList.remove("validation-summary-valid");

                const list = document.createElement("ul");

                for (const invalid of result.invalid) {
                    if (summaryFields.indexOf(invalid.validatable.element.name) >= 0) {
                        const item = document.createElement("li");

                        item.innerHTML = invalid.message;

                        list.appendChild(item);
                    }
                }

                summary.appendChild(list);
            }
        }
    }
};

$(function () {
    var divider = $('<option/>')
        .addClass('divider')
        .data('divider', true);

    var content = "<input type='text'class='bss-input' onKeyDown='event.stopPropagation();' onKeyPress='addSelectInpKeyPress(this,event)' onClick='event.stopPropagation()' placeholder='Add item'> <span class='fa fa-plus addnewicon' onClick='addSelectItem(this,event,1);'></span>";
    var addoption = $('<option/>', { class: 'addItem' })
        .data('content', content);

    $('.selectpicker')
        .append(divider)
        .append(addoption)
        .selectpicker();

});
function addSelectItem(t, ev) {
    ev.stopPropagation();

    var bs = $(t).closest('.bootstrap-select')
    var txt = bs.find('.bss-input').val().replace(/[|]/g, "");
    var txt = $(t).prev().val().replace(/[|]/g, "");
    if ($.trim(txt) == '') return;

    // Changed from previous version to cater to new
    // layout used by bootstrap-select.
    var p = bs.find('select');
    var o = $('option', p).eq(-2);
    o.before($("<option>", { "selected": true, "text": txt }));
    p.selectpicker('refresh');
}
function addSelectInpKeyPress(t, ev) {
    ev.stopPropagation();

    // do not allow pipe character
    if (ev.which == 124) ev.preventDefault();

    // enter character adds the option
    if (ev.which == 13) {
        ev.preventDefault();
        addSelectItem($(t).next(), ev);
    }
}

function DeleteRow(e, k, d) {
    $.ajax({
        url: window.location.origin + "/Корзина/УдалитьИзПодбора",
        type: "post",
        dataType: "xml",
        data: { key: k, id: d },
        complete: function () { new MvcGrid(e.closest(".mvc-grid")).reload(); }
    });
};
function AddOrUpdateRow(key) {
    $.ajax({
        url: window.location.origin + "/Корзина/AddUpdateSessionContext",
        type: "post",
        data: { sessionKey: key },
        datatype: "json",
        async: true,
        cache: false,
        success: function (data) {
            new MvcGrid(document.querySelector('#' + key)).reload();
        },
        error: function (e, v, f) {
            alert("ошибка выполнения!");
        }
    });
};
