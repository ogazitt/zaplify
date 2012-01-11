<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<BuiltSteady.Zaplify.ServerEntities.Task>" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
    Details
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">

<h2>Details</h2>

<fieldset>
    <legend>Task</legend>

    <div class="display-label">Name</div>
    <div class="display-field"><%: Model.Name %></div>

    <div class="display-label">Complete</div>
    <div class="display-field"><%: Model.Complete %></div>

    <div class="display-label">Description</div>
    <div class="display-field"><%: Model.Description %></div>

    <div class="display-label">Due</div>
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
<p>

    <%: Html.ActionLink("Edit", "Edit", new { id=Model.ID }) %> |
    <%: Html.ActionLink("Back to List", "Index") %>
</p>

</asp:Content>


