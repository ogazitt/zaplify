<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<UserDataModel>" %>
<%@ Import Namespace="BuiltSteady.Zaplify.Website.Models" %>

<asp:Content ContentPlaceHolderID="MasterHead" runat="server">
    <title>Dashboard</title>
    <link href="<%: Url.Content("~/content/dashboard/dashboard.css") %>" rel="stylesheet" type="text/css" />
    <script src="<%: Url.Content("~/scripts/shared/datamodel.js") %>" type="text/javascript"></script>
    <script src="<%: Url.Content("~/scripts/dashboard/controls.js") %>" type="text/javascript"></script>
    <script src="<%: Url.Content("~/scripts/dashboard/folderlist.js") %>" type="text/javascript"></script>
    <script src="<%: Url.Content("~/scripts/dashboard/foldermanager.js") %>" type="text/javascript"></script>
    <script src="<%: Url.Content("~/scripts/dashboard/suggestionlist.js") %>" type="text/javascript"></script>

    <script type="text/javascript">
        // document ready handler
        $(function () {
            DataModel.Init('<%= ConstantsModel.JsonConstants %>', '<%= Model.JsonUserData %>');
            Dashboard.Init(DataModel);
        });

    </script>
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="dashboard-region">
        <div class="dashboard-folders dashboard-list">
            &nbsp;
        </div>        
        
        <div class="dashboard-manager">         
        </div>
      
        <div class="dashboard-suggestions dashboard-list">
            &nbsp;
        </div>
    </div>


</asp:Content>
