$(function () {
    $("#sidebar-menu").dxTreeView({
        items: sidebar,
        key: "id",
        displayExpr: "text",
        itemsExpr: "items",
        selectionMode: "single",
        selectByClick: true,
        expandEvent: "click",

        onItemClick: function (e) {
            var url = e.itemData.url;

            if (url) {
                window.location.href = window.appBaseUrl + url;
            }
        }
    });
});