/*!
  * Powered by 小羊实验室
  */

function ajaxget(geturl, dom) {
    return $.ajax({
        type: "GET",
        url: geturl + "&inajax=1",
        dataType: "html",
        complete: function (jqXHR) {
            if (jqXHR.status == 200) {
                $(dom).html(jqXHR.responseText);
            }
        },
        error: function (jqXHR) {
            notice("请联系管理员。" + jqXHR.status + ' ' + jqXHR.statusText + "<br><pre>" + jqXHR.responseText, "danger", "内部错误");
            $(dom).html('<span color="red">内部错误，无法显示</span>');
        },
        beforeSend: function (jqXHR) {
            $(dom).html('<span class="octicons-sync"></span>加载中');
        }
    });
}

function showWindow(handlekey, geturl) {
    return $.ajax({
        type: "GET",
        url: geturl + "?handlekey=" + handlekey + "&inajax=1",
        dataType: "html",
        complete: function (jqXHR) {
            if (jqXHR.status == 200) {
                $("#append-parent").append('<div id="ajax_result_' + handlekey + '">' + jqXHR.responseText + '</div>');
                $("#modal-" + handlekey).on(
                    "hidden.bs.modal",
                    function (e) {
                        $('#ajax_result_' + handlekey).remove()
                    }
                ).modal("show");
            }
        },
        error: function (jqXHR) {
            notice("请联系管理员。" + jqXHR.status + ' ' + jqXHR.statusText + "<br><pre>" + jqXHR.responseText, "danger", "内部错误");
        }
    });
}

function notice(text, type, header) {
    type = type || "info";
    header = header || "小羊实验室";

    $('#notification-box').append(
        '<div class="toast new-add" data-autohide="false" role="alert" aria-live="assertive" aria-atomic="true">'
        + '<div class="toast-header"><div class="rounded mr-2 text-' + type + '">•</div>'
        + '<strong class="mr-auto">' + header + '</strong>'
        + '<small class="text-muted">just now</small>'
        + '<button type="button" class="ml-2 mb-1 close" data-dismiss="toast" aria-label="Close">'
        + '<span aria-hidden="true">&times;</span></button>'
        + '</div><div class="toast-body">' + text + '</div></div>');
    $('#notification-box .new-add').removeClass('new-add').toast('show');
}

function ajaxpost(Form, handlekey, parent, arg2) {
    var form = new FormData(Form);
    form.append("inajax", "1");
    form.append("handlekey", handlekey);
    $.ajax({
        url: $(Form).prop("action"),
        type: "post",
        data: form,
        processData: false,
        contentType: false,
        xhr: function () {
            var xhr = new XMLHttpRequest();
            xhr.upload.addEventListener('progress', function (e) {
                var progressRate = (e.loaded / e.total) * 100 + '%';
                $('#modal-' + parent + ' .upload-progress > div').css('width', progressRate);
            });
            return xhr;
        },
        success: function (data) {
            parent && $("#modal-" + parent).modal("hide");
            $("#append-parent").append('<div id="ajax_result_' + handlekey + '">' + data + '</div>');
            $("#modal-" + handlekey).on(
                "hidden.bs.modal",
                function (e) {
                    $('#ajax_result_' + handlekey).remove()
                }
            ).modal("show");
        },
        error: function (jqXHR) {
            notice("请联系管理员。" + jqXHR.status + ' ' + jqXHR.statusText + "<br><pre>" + jqXHR.responseText, "danger", "内部错误");
        }
    });
    return false;
}

function initXylabFunctions() {
	var $body = $('body');

	// ajaxWindow
	$body.on('click', 'a[data-toggle="ajaxWindow"]', function () {
		showWindow($(this).data("target"), $(this).prop("href"));
		return false;
	});

	// ajaxWindow
	$body.on('click', 'tr[data-toggle="ajaxWindow"]', function () {
		showWindow($(this).data("target"), $(this).data("url"));
		return false;
	});

	// gotoUrl
	$body.on('click', 'tr[data-toggle="gotoUrl"]', function () {
		window.location = $(this).data("url");
		return false;
	});

	// extendable-area
	$body.on("dblclick", '.extendable-area', function () {
		$(this).toggleClass('extended');
	});

	// custom-file-input
	$body.on('change', '.custom-file-input', function () {
		var files = this.files;
		var fileNames = [];
		for (var i = 0; i < files.length; i++) {
			fileNames.push(files.item(i).name);
		}
		$(this).next('.custom-file-label').html(fileNames.join(", "));
		$(this).next('.custom-file-label').removeClass('text-muted');
    });

    // toasts
    $body.on('hidden.bs.toast', '.toast', function () {
        $(this).remove();
    });

	// katex-src inline
	$('span.katex-src').each(function() {
		$(this).hide().after(katex.renderToString($(this).text(), {
			throwOnError: false
		}));
	});

	// katex-src
	$('pre.katex-src').each(function() {
		$(this).hide().after(katex.renderToString($(this).text(), {
			throwOnError: false,
			displayMode: true
		}));
	});
}

$(function () {
	initXylabFunctions();
});