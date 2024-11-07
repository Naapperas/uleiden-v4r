mergeInto(LibraryManager.library, {

  CallOpenSurvey: function (str) {
    var output = Pointer_stringify(str);
    console.log(output);
    openSurvey(output);
  },

  CallFpsWarning: function () {
    console.log("FPS Warning Triggered");
    triggerFpsWarning();
  },

});