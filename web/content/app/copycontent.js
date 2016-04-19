$(document).ready(function () {

  jQuery.fn.selectText = function(){
    var doc = document;
    var element = this[0];
    var range, selection;

    if (doc.body.createTextRange) {
      try
      {
        range = document.body.createTextRange();
        range.moveToElementText(element);
        range.select();
      } catch(e) { }
    } else if (window.getSelection) {
        selection = window.getSelection();
        range = document.createRange();
        range.selectNodeContents(element);
        selection.removeAllRanges();
        selection.addRange(range);
    }
  };

  $('#linkModal').on('show.bs.modal', function (e) {
    var dialog = $(this);
    var button = $(e.relatedTarget); // Button that triggered the modal
    var action = button.data('action');
    var snippetId = button.data('snippetid');

    if (action == 'show-link') {
      dialog.find('.modal-dialog').removeClass('modal-lg')
      dialog.find('.modal-title').text('Link to Snippet');
      dialog.find('.modal-body-inner').html('<pre id="selectMe">' + $(location).attr('href') + '</pre>');
      $("#selectMe").selectText();
    } else if (action == 'show-source') {
      dialog.find('.modal-dialog').addClass('modal-lg');
      dialog.find('.modal-title').text('Snippet Source');
      var snippetSourceUrl = document.location.href.replace(document.location.pathname, '/raw/') + snippetId;

      $.get(snippetSourceUrl, function (data) {
        dialog.find('.modal-body-inner')
          .html('<div id="selectMe"><pre><code>' + data.replace('<','&lt;').replace('>','&gt;') + '</code></pre></div>')
          .css('max-height', $(window).height() * 0.7)
          .css('overflow-y', 'auto');
        $("#selectMe").selectText();
      });
    }

    var dismissDialogHandler = function () {
      dialog.modal('hide');
    };

    $(document).keyup(dismissDialogHandler);
    dialog.on('hidden.bs.modal', function() {
      $(document).off("keyup", dismissDialogHandler);
    });
  });

});
