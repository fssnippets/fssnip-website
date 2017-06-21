function search() {
    var searchBox = $('#searchbox')[0];
    window.location.href = '/search/' + searchBox.value;
    return false;
}

$(document).ready(function () {
    $('#searchForm').submit(search);
    $('#searchbutton').click(search);
});
