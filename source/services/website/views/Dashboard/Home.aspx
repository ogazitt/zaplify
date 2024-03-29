﻿<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Bootstrap.Master" Inherits="System.Web.Mvc.ViewPage<UserDataModel>" %>
<%@ Import Namespace="System.Web" %>
<%@ Import Namespace="BuiltSteady.Zaplify.ServiceHost" %>
<%@ Import Namespace="BuiltSteady.Zaplify.Website.Models" %>

<asp:Content ContentPlaceHolderID="MasterHead" runat="server">
    <title>Dashboard</title>
    <link href="<%: Url.Content("~/content/themes/bootstrap/jquery-ui-1.8.18.css") %>" rel="stylesheet" type="text/css" />
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="dashboard-region container-fluid">
        <div class="row-fluid">
            <div class="dashboard-left dashboard-list span3 well">&nbsp </div>        
            <div class="dashboard-center span6">&nbsp;</div>
            <div class="dashboard-right dashboard-list span3 well">&nbsp;</div>
        </div>
    </div>

    <div id="modalMessage" class="modal hide fade">
        <div class="modal-header">
            <button type="button" class="close" data-dismiss="modal">×</button>
            <h3></h3>        
        </div>
        <div class="modal-body">
            <p></p>        
        </div>        
        <div class="modal-footer">
            <a href="#" class="btn btn-primary" data-dismiss="modal">OK</a>      
        </div>
    </div>
    <div id="modalPrompt" class="modal hide fade">
        <div class="modal-header">
            <h3></h3>        
        </div>
        <div class="modal-body">
            <p></p>        
        </div>        
        <div class="modal-footer">
            <a href="#" class="btn btn-primary">OK</a>      
            <a href="#" class="btn btn-cancel">Cancel</a>  
        </div>
    </div>

</asp:Content>

<asp:Content ContentPlaceHolderID="ScriptBlock" runat="server">
<%  if (HostEnvironment.IsAzure && !HostEnvironment.IsAzureDevFabric) { %>
    <!-- use merged and minified scripts when deployed to Azure -->
    <script type="text/javascript" src="<%: Url.Content("~/scripts/dashboard/dashboard.generated.min.js") %>"></script>
<%  } else { %>
    <script type="text/javascript" src="<%: Url.Content("~/scripts/dashboard/controls.js") %>"></script>
    <script type="text/javascript" src="<%: Url.Content("~/scripts/dashboard/datamodel.js") %>"></script>
    <script type="text/javascript" src="<%: Url.Content("~/scripts/dashboard/dashboard.js") %>"></script>
    <script type="text/javascript" src="<%: Url.Content("~/scripts/dashboard/folderlist.js") %>"></script>
    <script type="text/javascript" src="<%: Url.Content("~/scripts/dashboard/foldermanager.js") %>"></script>
    <script type="text/javascript" src="<%: Url.Content("~/scripts/dashboard/listeditor.js") %>"></script>
    <script type="text/javascript" src="<%: Url.Content("~/scripts/dashboard/itemeditor.js") %>"></script>
    <script type="text/javascript" src="<%: Url.Content("~/scripts/dashboard/suggestionlist.js") %>"></script>
    <script type="text/javascript" src="<%: Url.Content("~/scripts/dashboard/suggestionmanager.js") %>"></script>
<%  } %>
    <script type="text/javascript" src="<%: Url.Content("~/scripts/jquery-ui-timepicker.js") %>"></script>
    <script type="text/javascript" src="https://maps.googleapis.com/maps/api/js?sensor=false&libraries=places"></script>

<%
    string jsonConstants = Ajax.JavaScriptStringEncode(ConstantsModel.JsonConstants);
    string jsonUserData = Ajax.JavaScriptStringEncode(Model.JsonUserData);
    string renewFBToken = (Model.RenewFBToken) ? "true" : "false";
    string consentStatus = (Model.ConsentStatus == null) ? "" : Model.ConsentStatus;
%>    
    <script type="text/javascript">
        // document ready handler
        $(function () {
            DataModel.Init('<%= jsonConstants %>', '<%= jsonUserData %>');
            Dashboard.Init(DataModel, <%= renewFBToken %>, '<%= consentStatus %>');
        });
    </script>
</asp:Content>
