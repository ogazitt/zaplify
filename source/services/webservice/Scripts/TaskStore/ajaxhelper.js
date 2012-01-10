/* ajaxhelper.js */

var ajaxhelper = ajaxhelper || {};
ajaxhelper.validate = function (response) {
    if (response.StatusCode != undefined && response.StatusCode < 400)
        return response.Value;

    response = $.parseJSON(response.Value);
    return response;
};