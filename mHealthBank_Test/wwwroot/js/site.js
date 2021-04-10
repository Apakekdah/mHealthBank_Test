// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(document).ready(function () {
    var msgSuccess = $('#msgSuccess');
    var msgError = $("#msgError");

    var showError = function (msg) {
        if (msgError && (msgError.length > 0)) {
            msgError.html(msg);
            msgError.fadeTo(2000, 500).slideUp(500, function () {
                msgError.slideUp(500);
            });
        }
        else
            alert(msg);
    }

    var showInfo = function (info) {
        if (msgSuccess && (msgSuccess.length > 0)) {
            msgSuccess.html(info);
            msgSuccess.fadeTo(2000, 500).slideUp(500, function () {
                msgSuccess.slideUp(500);
            });
        }
        else
            alert(info);
    }

    jqModalPost = form => {
        var dlg = $('#crudDialog');
        try {
            $.ajax({
                type: 'POST',
                url: form.action,
                data: new FormData(form),
                contentType: false,
                processData: false,
                success: function (res) {
                    if (res.success) {
                        showInfo('Data telah tersimpan');
                    }
                    else {
                        showError(msg);
                    }
                    dlg.modal('hide');
                },
                error: function (err) {
                    showError(err);
                    dlg.modal('hide');
                }
            })
            return false;
        } catch (ex) {
            console.log(ex)
        }
    }
    jqModalDelete = form => {
        if (confirm('Are you sure to delete this record ?')) {
            try {
                $.ajax({
                    type: 'DELETE',
                    url: form.action,
                    data: new FormData(form),
                    contentType: false,
                    processData: false,
                    success: function (res) {
                        if (res.success) {
                            showInfo('Data telah tersimpan');
                        }
                        else {
                            showError(msg);
                        }
                    },
                    error: function (err) {
                        showError(err);
                    }
                })
            } catch (ex) {
                console.log(ex)
            }
        }
        return false;
    }
    jqDelete = (url, onsuccess) => {
        if (confirm('Are you sure to delete this record ?')) {
            try {
                $.ajax({
                    type: 'DELETE',
                    url: url,
                    contentType: false,
                    processData: false,
                    success: function (res) {
                        //$('#viewAll').html(res.html);
                        if (res.success) {
                            if (onsuccess && $.isFunction(onsuccess))
                                onsuccess();
                        }
                        else {
                            showError(msg);
                        }
                    },
                    error: function (err) {
                        showError(err);
                    }
                })
            } catch (ex) {
                console.log(ex)
            }
        }
        return false;
    }
});