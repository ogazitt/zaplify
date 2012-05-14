<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Bootstrap.Master" Inherits="System.Web.Mvc.ViewPage<UserDataModel>" %>
<%@ Import Namespace="System.Web" %>
<%@ Import Namespace="BuiltSteady.Zaplify.Website.Models" %>

<asp:Content ContentPlaceHolderID="MasterHead" runat="server">
    <title>Zapboard</title>
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
</asp:Content>

<asp:Content ContentPlaceHolderID="ScriptBlock" runat="server">
    <script type="text/javascript" src="<%: Url.Content("~/scripts/jquery-ui-timepicker.js") %>"></script>
    <!-- use merged and minified script when NOT debugging -->
    <script type="text/javascript" src="<%: Url.Content("~/scripts/zapboard/jquery-ui-1.8.16.bootstrap.min.js") %>"></script>
    <script type="text/javascript" src="<%: Url.Content("~/scripts/zapboard/datamodel.js") %>"></script>
    <script type="text/javascript" src="<%: Url.Content("~/scripts/zapboard/dashboard.js") %>"></script>
    <script type="text/javascript" src="<%: Url.Content("~/scripts/zapboard/folderlist.js") %>"></script>
    <script type="text/javascript" src="<%: Url.Content("~/scripts/zapboard/foldermanager.js") %>"></script>
    <script type="text/javascript" src="<%: Url.Content("~/scripts/zapboard/itemviewer.js") %>"></script>
    <script type="text/javascript" src="<%: Url.Content("~/scripts/zapboard/suggestionlist.js") %>"></script>
    <script type="text/javascript" src="<%: Url.Content("~/scripts/zapboard/suggestionmanager.js") %>"></script>
    <!--
    <script type="text/javascript" src="<%: Url.Content("~/scripts/zapboard/zapboard.generated.min.js") %>"></script>
    -->
    <script type="text/javascript" src="https://maps.googleapis.com/maps/api/js?sensor=false"></script>
    
<%
    string jsonConstants = Ajax.JavaScriptStringEncode(ConstantsModel.JsonConstants);
    string jsonUserData = Ajax.JavaScriptStringEncode(Model.JsonUserData);
    string renewFBToken = (Model.RenewFBToken) ? "true" : "false";
%>    
    <script type="text/javascript">
        // document ready handler
        $(function () {
            DataModel.Init('<%= jsonConstants %>', '<%= jsonUserData %>');
            Dashboard.Init(DataModel, <%= renewFBToken %>);
        });
    </script>
</asp:Content>
