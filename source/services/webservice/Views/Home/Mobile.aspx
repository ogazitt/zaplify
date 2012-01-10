<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>

<asp:Content ID="mobileTitle" ContentPlaceHolderID="TitleContent" runat="server">
    Mobile Applications
</asp:Content>

<asp:Content ID="cssContent" ContentPlaceHolderID="ScriptContent" runat="server">
    <link href="/Content/About.css" rel="stylesheet" type="text/css" />
</asp:Content>

<asp:Content ID="mobileContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="mobilebody">
    <center>
        <table width="100%">
            <tr>
                <td class="firsttd"><center><img src="/Images/iphone-logo.jpg" width="150" alt="iPhone" /></center></td>
                <td><center><img src="/Images/ipad-logo.png"  width="150" alt="iPad" /></center></td>
                <td><center><img src="/Images/android-logo.png"  width="150" alt="Android" /></center></td>
                <td><center><img src="/Images/winphone-logo.jpg"  width="150" alt="Windows Phone" /></center></td>
            </tr>
            <tr>
                <td class="firsttd"><a href="Mobile"><center><img src="/Images/appstore-logo.jpg"  width="150" alt="iPhone" /></center></a></td>
                <td><a href="Mobile"><center><img src="/Images/coming-soon2.gif"  width="75" alt="iPad" /></center></a></td>
                <td><a href="Mobile"><center><img src="/Images/coming-soon2.gif"  width="75" alt="Android" /></center></a></td>
                <td><a href="Mobile"><center><img src="/Images/winphone-marketplace-logo.png"  width="150" alt="Windows Phone" /></center></a></td>
            </tr>
        </table></center>
        <div class="half-width-centered">
            <center>
                <div>
                    <h2>Get it done on the run with our mobile applications.</h2>
                    <p>
                        Our mobile applications work perfectly even when they're not connected.  Take your lists and tasks wherever you go, 
                        and when you're back to the connected world, your data seamlessly syncs with the cloud.
                    </p>
                </div>
            </center>
        </div>
    </div>
</asp:Content>
