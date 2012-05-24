﻿<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<UserDataModel>" %>
<%@ Import Namespace="System.Web" %>
<%@ Import Namespace="BuiltSteady.Zaplify.Website.Models" %>

<asp:Content ContentPlaceHolderID="MasterHead" runat="server">
    <title>Dashboard</title>
    <link href="<%: Url.Content("~/content/dashboard/dashboard.css") %>" rel="stylesheet" type="text/css" />
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="dashboard-region">
        <div class="dashboard-folders dashboard-list ui-widget-content">
            &nbsp;
        </div>        
        
        <div class="dashboard-manager"></div>
        <div class="working"></div>
      
        <div class="dashboard-suggestions dashboard-list ui-widget-content">
            &nbsp;
        </div>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="ScriptBlock" runat="server">
    <script type="text/javascript" src="<%: Url.Content("~/scripts/jquery-ui-timepicker.js") %>"></script>
    <script type="text/javascript" src="<%: Url.Content("~/scripts/shared/datamodel.js") %>"></script>
    <script type="text/javascript" src="<%: Url.Content("~/scripts/dashboard/controls.js") %>"></script>
    <script type="text/javascript" src="<%: Url.Content("~/scripts/dashboard/folderlist.js") %>"></script>
    <script type="text/javascript" src="<%: Url.Content("~/scripts/dashboard/foldermanager.js") %>"></script>
    <script type="text/javascript" src="<%: Url.Content("~/scripts/dashboard/itemviewer.js") %>"></script>
    <script type="text/javascript" src="<%: Url.Content("~/scripts/dashboard/suggestionlist.js") %>"></script>
    <script type="text/javascript" src="<%: Url.Content("~/scripts/dashboard/suggestionmanager.js") %>"></script>
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