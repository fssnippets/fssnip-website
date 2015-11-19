$(document).ready(function () {
    $('.likeLink').click(function (e) {
        addToLikeCount($(this).data('snippetid'), $(this));
    });
});

function addToLikeCount(id, elem) {
    $.ajax({
        url: "/like/"+id, type: "POST"
    }).done(function (res) {
        //remove likes link, update counter
        $('.likeCount', elem.parent()).text(res);
        elem.remove();
    })
    .fail(function (res) {
        elem.text('Could not like this, please try again');
    });
}
