$(document).ready(function () {
  var timer;
  var previous = {code: "", packages: ""};
  var errors = false;

  $('#submit').on('click', function () {
    if (errors && !confirm('Your snippet contains compilation errors. Are you sure you want to submit it?')) return false;
  });

  $('#code, #nuget').on('change keyup paste', function (e) {
    clearTimeout(timer);
    timer = setTimeout(function (event) {
      var current = {
          code: $('#code').val(),
          packages: $('#nuget').val()
      }

      if ((current.code == previous.code && current.packages == previous.packages) ||
          current.code == "") {
          return;
      }

      previous = current;
      var content = $("#insert-form").serialize();
      $.ajax({
        url: "/pages/insert/check", data:content, type: "POST"
      }).done(function (res) {
        var container = $("#errors");
        container.empty();
        errors = res.filter(function(err) { return err.error; }).length > 0;
        res.forEach(function (err) {
          var glyph = "<span class='glyphicon glyphicon-" + (err.error?"exclamation":"warning") + "-sign' aria-hidden='true'></span>";
          container.append($("<div role='alert' class='alert alert-" + (err.error?"danger":"warning") + "'>" + glyph + "<span class='loc'>" + err.location[0] + ":" + err.location[1] + "</span><span class='msg'>" + err.message + "</span></div>"));
        });
      });
    }, 1000);
  });
});
