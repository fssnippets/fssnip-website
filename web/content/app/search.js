$(document).ready(function () {
    $('#searchForm').submit(function (e) {
		    var searchBox = $('#searchbox')[0];
        window.location.href = '/search/' + searchBox.value;
		    return false;
    });
});
