<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">Lists</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ScriptContent" runat="server">
    <script src="/Scripts/TaskStore/ajaxhelper.js" type="text/javascript"></script>
    <script src="/Scripts/TaskStore/constants.js" type="text/javascript"></script>
    <script src="/Scripts/TaskStore/dialogs.js" type="text/javascript"></script>
    <script src="/Scripts/TaskStore/html.js" type="text/javascript"></script>
    <script src="/Scripts/TaskStore/init.js" type="text/javascript"></script>
    <script src="/Scripts/TaskStore/language.js" type="text/javascript"></script>
    <script src="/Scripts/TaskStore/settings.js" type="text/javascript"></script>
    <script src="/Scripts/TaskStore/task.js" type="text/javascript"></script>
    <script src="/Scripts/TaskStore/tasklist.js" type="text/javascript"></script>
    <script src="/Scripts/TaskStore/taskstore.js" type="text/javascript"></script>
    <script src="/Scripts/TaskStore/user.js" type="text/javascript"></script>
</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="HeaderContent" runat="server">
    <div id="logindisplay">
        <% Html.RenderPartial("LogOnUserControl"); %>
    </div> 
</asp:Content>

<asp:Content ID="Content4" ContentPlaceHolderID="MainContent" runat="server">
    <div class="add">
        <div class="addwrapper">
            <input type="text" class="input-add" placeholder="Add a new task here" />
        </div>
    </div>
    <center><img id="loadingimg" src="/Images/icons/ajax-loader.gif" alt="Loading..." /></center>
    <div id="tasklists"></div>
</asp:Content>

