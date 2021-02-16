<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="GetPsdLayers.Default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Get PSD Layers</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <asp:FileUpload runat="server" ToolTip="give me a layered psd" ID="fupGiveMeAPsd" />
        <asp:Button ID="btnGivePsd" runat="server" Text="Submit"/>
        <p>Your layers will appear here as PNGs</p>
        <asp:Literal ID="litPsdLayerImages" runat="server" />
    </div>
    </form>
</body>
</html>
