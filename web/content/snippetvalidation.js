$(document).ready(function () {
  
  function isRequired(element){
    var hid = $('#hidden');
    // On insert page, #hidden is checkbox
    if (hid.attr("type") == "checkbox") return !(hid.is(":checked"));
    // On update page, #hidden is 'hidden' with bool value
    else return !($("#hidden").val() == "true");
  }

  $('#insert-form').validate({
    rules: { 
      title: "required",
      description:{
          required:isRequired
        },
      author:{
          required:isRequired
        },
      tags:{
          required:isRequired
        },
      code:"required"
    },
    messages: {
      title: "Please enter the title",
      description: "Please enter the description",
      author: "Please enter the author",
      tags: "Please enter at least one tag",
      code: "Please enter the code of the snippet"
      }
  });
});
