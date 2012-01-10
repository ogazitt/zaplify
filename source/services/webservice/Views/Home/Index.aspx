<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
    TaskStore
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
<div id="taskstore-main">
    <center>
        <table id="taskstore-main-table" width="100%">
            <tr>
                <td><div id="taskstore-info">
                    <img src="/Images/taskstore-graphic.png" width="350" alt="TaskStore - get it done." />
                    <center>Get it done.</center>
                </div></td>
                <td><div id="logincontrol">
                    <% Html.RenderPartial("LoginControl"); %>
                </div></td>
            </tr>
        </table>
    </center>
</div>
</asp:Content>
