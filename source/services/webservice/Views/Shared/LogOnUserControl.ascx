<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%
    if (Request.IsAuthenticated) {
%>
        <a id="loggedin"><%: Page.User.Identity.Name %></a>
        <p class="sep"></p>
        <ul class="ui-state-default" id="loginmenu">        
            <li id="managesettings" class="loginmenuitem"><a>Manage Settings</a></li>        
            <li id="changepasswd" class="loginmenuitem"><a>Change Password</a></li>
            <li id="signout" class="loginmenuitem"><a>Sign Out</a></li>
        </ul>
<%
    }
    else {
%> 
<!--
        <div><%: Html.ActionLink("Log In", "LogOn", "Account") %></div>
-->
<%
    }
%>
