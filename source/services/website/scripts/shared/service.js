//----------------------------------------------------------
// Copyright (C) BuiltSteady Inc. All rights reserved.
//----------------------------------------------------------
// Service.js

// Service object
var Service = function Service$() { }

// ---------------------------------------------------------
// public members

Service.ControllerResource = 'Controller';
Service.UsersResource = 'users';
Service.ConstantsResource = 'contants';
Service.FoldersResource = 'folders';
Service.ItemsResource = 'items';
Service.SuggestionsResource = 'suggestions';

// ---------------------------------------------------------
// private members

Service.siteUrl = null;
Service.resourceUrl = null;
Service.domainUrl = null;
Service.requestQueue = [];

Service.fbConsentUri = 'https://www.facebook.com/dialog/oauth';
Service.fbRedirectPath = 'oauthconsent/facebook';
Service.fbScopes = 'user_birthday,friends_likes,friends_birthday';
Service.googleConsentUri = 'oauthconsent/google';
Service.cloudADConsentUri = 'oauthconsent/cloudAD';

Service.invokeAsync = true;
Service.signOutUri = 'account/signout';
Service.signingOut = false;
Service.busyIcon = 'busy';
Service.refreshIcon = 'refresh';

Service.cssTheme = '#cssTheme';
Service.cssPath = 'content/themes/';
Service.cssThemeFile = 'jquery-ui-1.8.18.css';

// ---------------------------------------------------------
// public methods

Service.Init = function Service$Init(siteUrl, resourceUrl, domainUrl, fbAppID) {
    this.siteUrl = siteUrl;
    this.resourceUrl = resourceUrl;
    this.domainUrl = domainUrl;
    this.fbAppID = fbAppID;

    Service.invokeAsync = true;
}

Service.Close = function Service$Close() {
    Service.invokeAsync = false;
}

Service.InvokeController = function Service$InvokeController(controller, action, data, successHandler, errorHandler) {
    Service.invokeResource(Service.ControllerResource, controller, action, data, successHandler, errorHandler);
}

Service.GetResource = function Service$GetResource(resource, id, successHandler, errorHandler) {
    Service.invokeResource(resource, id, "GET", null, successHandler, errorHandler);
}

Service.InsertResource = function Service$InsertResource(resource, data, successHandler, errorHandler) {
    Service.invokeResource(resource, null, "POST", data, successHandler, errorHandler);
}

Service.UpdateResource = function Service$UpdateResource(resource, id, data, successHandler, errorHandler) {
    Service.invokeResource(resource, id, "PUT", data, successHandler, errorHandler);
}

Service.DeleteResource = function Service$DeleteResource(resource, id, data, successHandler, errorHandler) {
    Service.invokeResource(resource, id, "DELETE", data, successHandler, errorHandler);
}

Service.NavigateToDashboard = function Service$NavigateToDashboard() {
    window.navigate(Service.siteUrl);
}

Service.ChangeTheme = function Service$ChangeTheme(theme) {
    var cssThemeUri = Service.siteUrl.concat(Service.cssPath, theme, '/', Service.cssThemeFile);
    $(Service.cssTheme).attr('href', cssThemeUri);
}

Service.SignOut = function Service$SignOut() {
    Service.signingOut = true;
}

Service.GetFacebookConsent = function Service$GetFacebookConsent() {
    var uri = Service.fbConsentUri + "?client_id=" + Service.fbAppID + "&redirect_uri=" + encodeURI(Service.domainUrl + Service.fbRedirectPath) + "&scope=" + Service.fbScopes;
    window.navigate(uri);
}

Service.GetGoogleConsent = function Service$GetGoogleConsent() {
    var uri = Service.googleConsentUri;
    window.navigate(uri);
}

Service.GetCloudADConsent = function Service$GetCloudADConsent() {
    var uri = Service.cloudADConsentUri;
    window.navigate(uri);
}

Service.Geocoder = function Service$Geocoder() {
    if (Service.geocoder == null) {
        Service.geocoder = new google.maps.Geocoder();
    }
    return Service.geocoder;
}

// ---------------------------------------------------------
// private methods

// helper to invoke a rest-based resource OR a Controller method (using POST)
// address resource type and optional id using httpMethod
// converts data to json in request, expects json response
// errorHandler is optional
Service.invokeResource = function Service$invokeResource(resource, id, httpMethod, data,
    successHandler, errorHandler) {

    // wrap success handler to check for json errors
    var jsonSuccessHandler = function (response, status, jqXHR) {
        if (response == null || HttpStatusCode.IsError(status) ||
            response.StatusCode == null || HttpStatusCode.IsError(response.StatusCode)) {
            jsonErrorHandler(jqXHR);
            return;
        }
        var responseState = new ResponseState(response.StatusCode);
        if (resource == Service.ControllerResource) {
            responseState.result = response;
        } else {
            responseState.result = response.Value;
        }
        if (successHandler != null) {
            successHandler(responseState);
        }
    };

    // wrap error handler to create jsonError object
    var jsonErrorHandler = function (jqXHR) {
        var responseState = Service.getResponseState(jqXHR);

        if (responseState.status == HttpStatusCode.Redirect) {
            window.onbeforeunload = null;
            $(window).unload(null);
            window.location.href = responseState.message;   // message contains redirect url
        } else {
            if (errorHandler != null) {
                errorHandler(responseState);
            } else {
                Service.displayError(responseState);
            }
        }
    };

    var invokeUrl;
    var jsonData = (data == null) ? '' : JSON.stringify(data);
    if (resource == Service.ControllerResource) {
        var controllerAction = id + '/' + httpMethod;
        httpMethod = 'POST';
        invokeUrl = Service.siteUrl + controllerAction;
    } else {
        id = (id == null) ? '' : ('/' + id);
        invokeUrl = Service.resourceUrl + resource + id;
    }

    request = {
        url: invokeUrl,
        type: httpMethod,
        contentType: "application/json",
        dataType: "json",
        data: jsonData,
        async: Service.invokeAsync
    };

    if (!Service.signingOut) {
        Service.beginRequest(request, jsonSuccessHandler, jsonErrorHandler);
    }
}

Service.isRequestPending = function Service$isRequestPending() {
    return Service.requestQueue.length > 0;
}

// queue an ajax request
Service.beginRequest = function Service$beginRequest(request, successHandler, errorHandler) {
    request.success = function (data, status, jqXHR) {
        Service.endRequest();
        successHandler(data, status, jqXHR);
    };
    request.error = function (jqXHR) {
        Service.endRequest();
        errorHandler(jqXHR);
    };
    $('.' + Service.refreshIcon).addClass(Service.busyIcon);
    Service.requestQueue.push(request);
    if (Service.requestQueue.length == 1) {
        Service.nextRequest();
    }
}

// current ajax request has completed, invoke next queued request
Service.endRequest = function Service$endRequest() {
    Service.requestQueue.shift();
    if (Service.requestQueue.length > 0) {
        Service.nextRequest();
    } else {
        $('.' + Service.refreshIcon).removeClass(Service.busyIcon);
    }
}

// invoke next request in queue
Service.nextRequest = function Service$nextRequest() {
    var request = Service.requestQueue[0];
    $.ajax(request);
}

Service.getResponseState = function Service$getResponseState(jqXHR) {
    var responseState = new ResponseState(jqXHR.status);

    // check for json in responseText
    var contentType = jqXHR.getResponseHeader("Content-Type");
    if ((jqXHR.responseText != null) && (contentType != null) &&
        (contentType.search(/application\/json/i) >= 0)) {
        response = jQuery.parseJSON(jqXHR.responseText);
        if (response.StatusCode != undefined && HttpStatusCode.IsError(response.StatusCode)) {
            responseState.status = response.StatusCode;
            if (response.Message != null) {
                responseState.message = response.Message;
            }
        }
    }

    if (responseState.message == null) {
        responseState.message = "ERROR: " + responseState.status;
    }    
    return responseState;
}

Service.displayError = function Service$displayError(responseState) {
    if (responseState.IsUnexpected()) {
        alert("Unexpected Error!\n\n" + responseState.message + "\n\n");
    }
    else {  
        alert(responseState.message + "\n\n");
    }
}

// ResponseState is object that is passed into successHandlers and errorHandlers
var ResponseState = function ResponseState$(statusCode) {
    // data members used
    this.status = statusCode;
    this.result = null;
    this.message = null;
    this.OK = function() { return HttpStatusCode.IsOK(this.status); };
    this.IsUnexpected = function() { return (HttpStatusCode.IsServerError(this.status)); };
};


// HttpStatusCodes
var HttpStatusCode = new function HttpStatusCode$() { 

    this.IsOK = function(statusCode) { return (statusCode < HttpStatusCode.BadRequest); };
    this.IsError = function(statusCode) { return (statusCode >= HttpStatusCode.BadRequest); };
    this.IsServerError = function(statusCode) { return (statusCode >= HttpStatusCode.InternalServerError); };

    // OK 
    this.OK = 200;
    this.Created = 201;
    this.Accepted = 202;
    this.NonAuthoritativeInformation = 203;
    this.NoContent = 204;
    this.ResetContent = 205;
    this.PartialContent = 206;

    this.Ambiguous = 300;
    this.Moved = 301;
    this.Redirect = 302;
    this.RedirectMethod = 303;
    this.NotModified = 304;
    this.UseProxy = 305;
    this.Unused = 306;
    this.TemporaryRedirect = 307;

    // Error
    this.BadRequest = 400;
    this.Unauthorized = 401;
    this.PaymentRequired = 402;
    this.Forbidden = 403;
    this.NotFound = 404;
    this.MethodNotAllowed = 405;
    this.NotAcceptable = 406;

    this.ProxyAuthenticationRequired = 407;
    this.RequestTimeout = 408;
    this.Conflict = 409;
    this.Gone = 410;
    this.LengthRequired = 411;
    this.PreconditionFailed = 412;
    this.RequestEntityTooLarge = 413;
    this.RequestUriTooLong = 414;
    this.UnsupportedMediaType = 415;
    this.RequestedRangeNotSatisfiable = 416;
    this.ExpectationFailed = 417;

    this.InternalServerError = 500;
    this.NotImplemented = 501;
    this.BadGateway = 502;
    this.ServiceUnavailable = 503;
    this.GatewayTimeout = 504;
    this.HttpVersionNotSupported = 505;
}
