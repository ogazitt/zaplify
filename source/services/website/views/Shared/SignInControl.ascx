<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<div class="signin-control">
<% if (Request.IsAuthenticated) { %>
        <span><%: Page.User.Identity.Name %></span>
        <span>&nbsp;|&nbsp;</span>
        <span><%: Html.ActionLink("Sign Out", "SignOut", "Account") %> </span>
<% } else if (Request.Path.EndsWith("SignIn", StringComparison.OrdinalIgnoreCase)) { %> 
        <span><%: Html.ActionLink("Register New User", "Register", "Account") %></span>
<% } else { %> 
        <span><%: Html.ActionLink("Sign In", "SignIn", "Account") %></span>
<% } %>
</div>