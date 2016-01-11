$(document).ready(function () {
  var timer;
  var previous;
  var errors = false;

  $('#submit').on('click', function () {
    if (errors && !confirm('Your snippet contains compilation errors. Are you sure you want to submit it?')) return false;
  });

  $('#code').on('change keyup paste', function (e) {
    clearTimeout(timer);
    timer = setTimeout(function (event) {
      var code = $('#code').val();
      if (code == previous) return;
      previous = code;
      $.ajax({
        url: "/pages/insert/check", data:code,
        contentType: "text/plain", type: "POST", dataType: "JSON"
      }).done(function (res) {
        var container = $("#errors");
        container.empty();
        errors = res.filter(function(err) { return err.error; }).length > 0;
        if (res.length == 0) {
          var glyph = "<span class='glyphicon glyphicon-ok-sign' aria-hidden='true'></span>";
          container.append($("<div role='alert' class='alert alert-success'>" + glyph + 
            "<span class='msg'>The snippet parses and type checks with no errors.</span></div>"));
        }
        else {
          res.forEach(function (err) {
            var glyph = "<span class='glyphicon glyphicon-" + (err.error?"exclamation":"warning") + "-sign' aria-hidden='true'></span>";
            container.append($("<div role='alert' class='alert alert-" + (err.error?"danger":"warning") + "'>" + 
              glyph + "<span class='loc'>" + err.location[0] + ":" + err.location[1] + "</span><span class='msg'>" + 
              err.message + "</span></div>"));
          });
        }
      });
    }, 1000);
  });
});
