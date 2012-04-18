<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<div class="signin-control">
<% if (Request.IsAuthenticated) { %>
        <span><%: Page.User.Identity.Name %></span>
        <span>&nbsp;|&nbsp;</span>
        <%: Html.ActionLink("Sign Out", "SignOut", "Account", null, new { onclick = "Service.SignOut();" })%> 
<% } else if (Request.Path.EndsWith("SignIn", StringComparison.OrdinalIgnoreCase)) { %> 
        <%: Html.ActionLink("Register New User", "Register", "Account") %>
<% } else { %> 
        <%: Html.ActionLink("Sign In", "SignIn", "Account") %>
<% } %>
</div>