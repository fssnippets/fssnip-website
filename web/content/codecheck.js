/************************ Background checking & tag recommendation ********************************/

$(document).ready(function () {
  var timer;
  var previous = {code: "", packages: ""};
  var errors = false;
  var tagRecommendationOn = true;
  
  $("select").on("change", function() { 
    tagRecommendationOn = false; 
  });
  
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
        // Select the recommended tags for the snippet
        if (tagRecommendationOn && res.tags.length > 0) {
          $("select").val(res.tags);
          $("select").trigger("chosen:updated");
        }
        
        // Display the reported errors and warnings
        var container = $("#errors");
        container.empty();
        errors = res.errors.filter(function(err) { return err.error; }).length > 0;
        if (res.errors.length == 0) {
          var glyph = "<span class='glyphicon glyphicon-ok-sign' aria-hidden='true'></span>";
          container.append($("<div role='alert' class='alert alert-success'>" + glyph + 
            "<span class='msg'>The snippet parses and type checks with no errors.</span></div>"));
        }
        else {
          res.errors.forEach(function (err) {
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

/************************ Listing all tags and chosen initialization ******************************/

var chosenInitialized = false;

function switchHidden() {
  if ($('#hidden').is(':checked')) {
    $('.public-details').css('display', 'none');
  } else {
    $('.public-details').css('display', '');
    if (!chosenInitialized) {
      chosenInitialized = true;
      $("select").chosen({
        create_option: true,
        persistent_create_option: true,
        skip_no_results: true 
      });               
      $('select').append('<option value="foo">Bar</option>');
      $('select').trigger("chosen:updated");
    }
  }
}

$(document).ready(function() {
  $.ajax({ url: "/pages/insert/taglist" }).done(function (res) {
    res.forEach(function(tag) {
      $('select').append('<option value="' + tag + '">' + tag + '</option>');
    });
  });
});
