
const urlParams = new URLSearchParams(window.location.search);
const typeid = urlParams.get('typeid')
const proid = urlParams.get('PROID')


function openSurvey(participant) {
    gameInstance.SetFullscreen(0);
    setTimeout(openSurveyDelayed(participant), 1500);
}

function openSurveyDelayed(participant) {
    let surveyURL = "https://SURVEYURL?gameid=" + participant;

    if (typeid === "MTURK") {
        surveyURL = surveyURL + "&typeid=MTURK";
    } else if (typeid === "PROLIFIC") {
        surveyURL = surveyURL + "&typeid=PROLIFIC&PROID=" + proid;
    } else if (typeid === "SURVEYCIRCLE") {
        surveyURL = surveyURL + "&typeid=SURVEYCIRCLE";
    } else {
        surveyURL = surveyURL + "&typeid=WEB";
    }
    $('#surveylinktext').html(surveyURL);
    $('.surveylink').attr('href', surveyURL);

    $('#surveyid').html(participant)


    let surveyModal = UIkit.modal('#surveymodal', {
        escClose: false,
        bgClose: false
    });

    surveyModal.show();

    $("#surveymodal").focus();

}

function triggerFpsWarning(participant) {

    let fpsmodal = UIkit.modal('#fpsmodal', {
        escClose: false,
        bgClose: false
    });

    fpsmodal.show();

    $("#fpsmodal").focus();
}


if (typeid === "MTURK") {
    $('#surveyidtext').html('Copy the game id below - you will need it in the survey. <em>(do not use this number to confirm your HIT)</em><br><br>Your game id:');
    console.log("TypeId is MTURK");
}

