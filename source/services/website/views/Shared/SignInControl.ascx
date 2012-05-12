<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<% if (Request.IsAuthenticated) { %>
<ul class="nav nav-pills pull-right">
    <li class="divider-vertical"></li>    
    <li class="dropdown active">
        <a class="dropdown-toggle" data-toggle="dropdown">
            <i class="icon-user icon-white"></i> <strong><%: Page.User.Identity.Name %></strong> <b class="caret"></b>
        </a>
        <ul class="dropdown-menu">
            <li class="option-help hide"><a><i class="icon-question-sign"></i> Help</a></li>
            <li class="option-settings hide"><a><i class="icon-cogs"></i> User Settings</a></li>
            <li class="option-refresh hide"><a><i class="icon-refresh"></i> Refresh</a></li>
            <li class="divider"></li>            
            <li><a href="<%: Url.Content("~/account/signout") %>" onclick="Service.SignOut()"><i class="icon-off"></i> Sign Out</a></li>
        </ul>
    </li>
</ul>
<% } else if (Request.Path.EndsWith("SignIn", StringComparison.OrdinalIgnoreCase)) { %>
    <ul class="nav pull-right">
        <li class="divider-vertical"></li>    
        <li><a href="<%: Url.Content("~/account/register") %>">Register New User</a></li>
    </ul>
<% } else { %> 
    <ul class="nav pull-right">
        <li class="divider-vertical"></li>    
        <li><a href="<%: Url.Content("~/account/signin") %>">Sign In</a></li>
    </ul>
<% } %>

