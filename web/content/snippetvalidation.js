$(document).ready(function () {
  
  function isRequired(element){
    var hid=$('#hidden');
    var hiddenExists=hid.length===1; //update form doesn't have the hidden field, but validation has to occur regardless of that.
    return !hiddenExists || !hid.is(":checked");
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
