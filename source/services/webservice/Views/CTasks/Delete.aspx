<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<TaskStoreServerEntities.Task>" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
    Delete
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">

<h2>Delete</h2>

<h3>Are you sure you want to delete this?</h3>
<fieldset>
    <legend>Task</legend>

    <div class="display-label">Name</div>
    <div class="display-field"><%: Model.Name %></div>

    <div class="display-label">Complete</div>
    <div class="display-field"><%: Model.Complete %></div>

    <div class="display-label">Description</div>
    <div class="display-field"><%: Model.Description %></div>

    <div class="display-label">DueDate</div>
    <div class="display-field"><%: Model.DueDate %></div>

    <div class="display-label">Location</div>
    <div class="display-field"><%: Model.Location %></div>

    <div class="display-label">Phone</div>
    <div class="display-field"><%: Model.Phone %></div>

    <div class="display-label">Website</div>
    <div class="display-field"><%: Model.Website %></div>

    <div class="display-label">Email</div>
    <div class="display-field"><%: Model.Email %></div>

    <div class="display-label">LinkedTaskListID</div>
    <div class="display-field"><%: Model.LinkedTaskListID %></div>

    <div class="display-label">TaskTags</div>
    <div class="display-field"><%: (Model.TaskTags == null ? "None" : Model.TaskTags.Count.ToString()) %></div>

    <div class="display-label">Created</div>
    <div class="display-field"><%: String.Format("{0:g}", Model.Created) %></div>

    <div class="display-label">LastModified</div>
    <div class="display-field"><%: String.Format("{0:g}", Model.LastModified) %></div>
</fieldset>
<% using (Html.BeginForm()) { %>
    <p>
        <input type="submit" value="Delete" /> |
        <%: Html.ActionLink("Back to List", "Index") %>
    </p>
<% } %>

</asp:Content>


