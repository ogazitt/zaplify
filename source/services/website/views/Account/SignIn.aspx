<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<SignInModel>" %>
<%@ Import Namespace="BuiltSteady.Zaplify.Website.Models" %>

<asp:Content ContentPlaceHolderID="MasterHead" runat="server">
    <title>Sign-in</title>
    <script src="<%: Url.Content("~/Scripts/jquery.validate.min.js") %>" type="text/javascript"></script>
    <script src="<%: Url.Content("~/Scripts/jquery.validate.unobtrusive.min.js") %>" type="text/javascript"></script>
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
<div class="dialog-panel-top">&nbsp;</div>
<div class="dialog-panel">
    <h2>Sign In</h2>

    <% using (Html.BeginForm()) { %>
        <div>
            <fieldset>
                <legend>User Information</legend>
                
                <div class="dialog-label">
                    <%: Html.LabelFor(m => m.Email) %>
                </div>
                <div class="dialog-field">
                    <%: Html.TextBoxFor(m => m.Email) %>
                    <%: Html.ValidationMessageFor(m => m.Email) %>
                </div>
                
                <div class="dialog-label">
                    <%: Html.LabelFor(m => m.Password) %>
                </div>
                <div class="dialog-field">
                    <%: Html.PasswordFor(m => m.Password) %>
                    <%: Html.ValidationMessageFor(m => m.Password) %>
                </div>
                
                <div class="dialog-field">
                    <%: Html.CheckBoxFor(m => m.RememberMe) %>
                    <%: Html.LabelFor(m => m.RememberMe) %>
                </div>
                
                <p>
                    <input class="dialog-button" type="submit" value="Sign-in" />
                </p>
            </fieldset>
        </div>

        <%: Html.ValidationSummary(true, "Unable to sign in. Please update your user information and try again.") %>
    <% } %>

</div>
<div class="dialog-panel-bottom">&nbsp;</div>
</asp:Content>
