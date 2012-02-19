<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<ChangePasswordModel>" %>
<%@ Import Namespace="BuiltSteady.Zaplify.Website.Models" %>

<asp:Content ID="changePasswordTitle" ContentPlaceHolderID="MasterHead" runat="server">
    <title>Change Password</title>
    <script src="<%: Url.Content("~/Scripts/jquery.validate.min.js") %>" type="text/javascript"></script>
    <script src="<%: Url.Content("~/Scripts/jquery.validate.unobtrusive.min.js") %>" type="text/javascript"></script>
</asp:Content>

<asp:Content ID="changePasswordContent" ContentPlaceHolderID="MainContent" runat="server">
<div class="dialog-panel-top">&nbsp;</div>
<div class="dialog-panel">
    <h2>Change Password</h2>

    <% using (Html.BeginForm()) { %>
        <div>
            <fieldset>
                <legend>User Information</legend>
                
                <div class="dialog-label">
                    <%: Html.LabelFor(m => m.OldPassword) %>
                </div>
                <div class="dialog-field">
                    <%: Html.PasswordFor(m => m.OldPassword) %>
                    <%: Html.ValidationMessageFor(m => m.OldPassword) %>
                </div>
                
                <div class="dialog-label">
                    <%: Html.LabelFor(m => m.NewPassword) %>
                </div>
                <div class="dialog-field">
                    <%: Html.PasswordFor(m => m.NewPassword) %>
                    <%: Html.ValidationMessageFor(m => m.NewPassword) %>
                </div>
                
                <div class="dialog-label">
                    <%: Html.LabelFor(m => m.ConfirmPassword) %>
                </div>
                <div class="dialog-field">
                    <%: Html.PasswordFor(m => m.ConfirmPassword) %>
                    <%: Html.ValidationMessageFor(m => m.ConfirmPassword) %>
                </div>
                
                <input class="dialog-button" type="submit" value="Change Password" />
            </fieldset>
        </div>
        <%: Html.ValidationSummary(true, "Unable to change password. Resolve issues and try again.") %>
    <% } %>
</div>
<div class="dialog-panel-bottom">&nbsp;</div>
</asp:Content>
